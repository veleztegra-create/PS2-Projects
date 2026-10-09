using PS2Manager.Core.Abstractions;
using PS2Manager.Core.Diagnostics;
using PS2Manager.Core.Models;
using PS2Manager.IO;

namespace PS2Manager.Services;

/// <summary>
/// Analyzer read-only de bibliotecas UL (USBExtreme / OPL).
///
/// Flujo:
///   1. Leer y parsear ul.cfg (si existe).
///   2. Enumerar archivos ul.* y parsear sus nombres.
///   3. Asociar por GAME ID (no por CRC), para poder detectar CRC incorrecto
///      como un diagnóstico independiente de "parte faltante" y de "huérfano".
///   4. Calcular severidad por juego usando únicamente los diagnósticos de ese juego.
///   5. Reportar huérfanos y archivos ul.* no parseables.
///
/// Garantías:
///   - Nunca escribe ni borra archivos (no recibe ninguna operación de escritura).
///   - La severidad de un juego no se contamina por diagnósticos de otro.
///   - Un archivo con CRC incorrecto NO se reporta como faltante ni como huérfano.
/// </summary>
public sealed class UlAnalyzer
{
    private readonly IFileSystem _fs;

    public UlAnalyzer(IFileSystem fs)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
    }

    public UlAnalysisResult Analyze()
    {
        var diagnostics = new List<Diagnostic>();
        var games = new List<UlGame>();

        var allFiles = _fs.EnumerateFileNames().ToList();

        // ---------- 1. ul.cfg ----------
        UlCfgParseResult cfg;
        var cfgFile = allFiles.FirstOrDefault(f =>
            f.Equals("ul.cfg", StringComparison.OrdinalIgnoreCase));

        if (cfgFile is not null)
        {
            var bytes = _fs.ReadAllBytes(cfgFile);
            cfg = UlCfgParser.Parse(bytes);
            // Los diagnósticos de cada registro se añadirán una sola vez al juego
            // correspondiente; aquí solo añadimos los diagnósticos globales del archivo.
            var recordDiagnosticSet = cfg.RecordDiagnostics.Values
                .SelectMany(items => items)
                .ToHashSet();
            diagnostics.AddRange(cfg.Diagnostics.Where(d => !recordDiagnosticSet.Contains(d)));
        }
        else
        {
            cfg = UlCfgParseResult.Empty;
        }

        // ---------- 2. Partición de archivos ul.* ----------
        var ulFiles = allFiles
            .Where(f => f.StartsWith("ul.", StringComparison.OrdinalIgnoreCase)
                        && !f.Equals("ul.cfg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var parsedUlFiles = new List<UlFileEntry>();
        var unparsedUlFiles = new List<string>();

        foreach (var name in ulFiles)
        {
            if (UlFilename.TryParse(name, out var parts))
                parsedUlFiles.Add(new UlFileEntry(name, parts));
            else
                unparsedUlFiles.Add(name);
        }

        // Agrupar por GameId. La asociación con registros es por GameId.
        var filesByGameId = parsedUlFiles
            .GroupBy(f => f.Parts.GameId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var usedGameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateGameIds = cfg.Records
            .Select(r => NormalizeImageId(r.ImageIdentifier))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ---------- 3. Procesar registros ----------
        foreach (var rec in cfg.Records)
        {
            // Los diagnósticos se acumulan en una lista LOCAL al juego.
            // Así la severidad se calcula solo con lo que le pertenece.
            var gameDiagnostics = new List<Diagnostic>();
            if (cfg.RecordDiagnostics.TryGetValue(rec.Index, out var metadataDiagnostics))
                gameDiagnostics.AddRange(metadataDiagnostics);

            string expectedGameId = NormalizeImageId(rec.ImageIdentifier);

            if (!string.IsNullOrWhiteSpace(expectedGameId) &&
                duplicateGameIds.Contains(expectedGameId))
            {
                gameDiagnostics.Add(new Diagnostic(
                    DiagnosticCodes.Opl008,
                    Severity.Error,
                    $"Game ID duplicado en ul.cfg: '{expectedGameId}' (registro #{rec.Index}, '{rec.GameName}'). " +
                    "La asociación de partes es ambigua."));
            }
            string expectedCrc = OplCrc32.Format(OplCrc32.ComputeGameName(rec.GameName));

            List<UlFileEntry> foundFiles;

            if (string.IsNullOrWhiteSpace(expectedGameId))
            {
                gameDiagnostics.Add(new Diagnostic(
                    DiagnosticCodes.Opl007,
                    Severity.Error,
                    $"Registro #{rec.Index}: ImageIdentifier vacío; no se puede asociar " +
                    "archivos UL a este registro."));
                foundFiles = new List<UlFileEntry>();
            }
            else
            {
                usedGameIds.Add(expectedGameId);
                if (!filesByGameId.TryGetValue(expectedGameId, out foundFiles!))
                    foundFiles = new List<UlFileEntry>();
            }

            if (foundFiles.Count == 0)
            {
                // Ninguna parte con este GameId: reportar cada parte esperada como faltante.
                for (int i = 0; i < rec.DeclaredPartCount; i++)
                {
                    gameDiagnostics.Add(new Diagnostic(
                        DiagnosticCodes.Opl003,
                        Severity.Missing,
                        $"'{rec.GameName}': falta parte {i:D2} (Game ID {expectedGameId})."));
                }
            }
            else
            {
                var actualNumbers = foundFiles.Select(f => f.Parts.PartNumber).ToList();
                var expectedNumbers = Enumerable
                    .Range(0, rec.DeclaredPartCount)
                    .ToHashSet();

                // Partes esperadas que no aparecen.
                foreach (var n in expectedNumbers.Except(actualNumbers).OrderBy(n => n))
                {
                    gameDiagnostics.Add(new Diagnostic(
                        DiagnosticCodes.Opl003,
                        Severity.Missing,
                        $"'{rec.GameName}': falta parte {n:D2}."));
                }

                // Partes presentes que no deberían existir según DeclaredPartCount.
                foreach (var n in actualNumbers.Except(expectedNumbers).OrderBy(n => n))
                {
                    gameDiagnostics.Add(new Diagnostic(
                        DiagnosticCodes.Opl006,
                        Severity.Warning,
                        $"'{rec.GameName}': parte sobrante {n:D2} " +
                        $"(DeclaredPartCount={rec.DeclaredPartCount})."));
                }

                // Duplicados del mismo número de parte.
                foreach (var group in actualNumbers
                             .GroupBy(n => n)
                             .Where(g => g.Count() > 1)
                             .OrderBy(g => g.Key))
                {
                    gameDiagnostics.Add(new Diagnostic(
                        DiagnosticCodes.Opl006,
                        Severity.Error,
                        $"'{rec.GameName}': parte {group.Key:D2} duplicada " +
                        $"({group.Count()} archivos con el mismo número)."));
                }

                // Huecos en la secuencia (independiente de DeclaredPartCount).
                var sorted = actualNumbers.Distinct().OrderBy(n => n).ToList();
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    if (sorted[i + 1] != sorted[i] + 1)
                    {
                        gameDiagnostics.Add(new Diagnostic(
                            DiagnosticCodes.Opl003,
                            Severity.Missing,
                            $"'{rec.GameName}': hueco en secuencia de partes " +
                            $"entre {sorted[i]:D2} y {sorted[i + 1]:D2}."));
                    }
                }

                // CRC por archivo.
                foreach (var f in foundFiles)
                {
                    if (!string.Equals(f.Parts.CrcHex, expectedCrc, StringComparison.OrdinalIgnoreCase))
                    {
                        gameDiagnostics.Add(new Diagnostic(
                            DiagnosticCodes.Opl005,
                            Severity.Error,
                            $"'{rec.GameName}': CRC en filename {f.Parts.CrcHex} != " +
                            $"esperado {expectedCrc} (archivo {f.FileName})."));
                    }
                }
            }

            var severity = ComputeGameSeverity(gameDiagnostics);

            games.Add(new UlGame(
                GameName: rec.GameName,
                GameId: expectedGameId,
                Media: rec.Media,
                DeclaredPartCount: rec.DeclaredPartCount,
                Parts: foundFiles
                    .OrderBy(f => f.Parts.PartNumber)
                    .Select(f => new UlGamePart(
                        f.Parts.PartNumber, f.Parts.CrcHex, f.FileName))
                    .ToList(),
                Severity: severity,
                Diagnostics: gameDiagnostics));

            diagnostics.AddRange(gameDiagnostics);
        }

        // ---------- 4. Huérfanos ----------
        foreach (var kv in filesByGameId)
        {
            if (usedGameIds.Contains(kv.Key)) continue;

            foreach (var f in kv.Value)
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticCodes.Opl004,
                    Severity.Orphan,
                    $"Parte huérfana {f.FileName} sin registro en ul.cfg " +
                    $"(Game ID {kv.Key})."));
            }
        }

        // ---------- 5. ul.* no parseables ----------
        foreach (var name in unparsedUlFiles)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticCodes.Opl007,
                Severity.Warning,
                $"Nombre UL no reconocido: {name}"));
        }

        return new UlAnalysisResult(games, cfg.Records, diagnostics);
    }

    private static Severity ComputeGameSeverity(IReadOnlyList<Diagnostic> diags)
    {
        // Prioridad estricta: Error > Missing > Orphan > Warning > Unknown > Healthy.
        // Se evalúa SOLO sobre los diagnósticos de este juego.
        if (diags.Any(d => d.Severity == Severity.Error)) return Severity.Error;
        if (diags.Any(d => d.Severity == Severity.Missing)) return Severity.Missing;
        if (diags.Any(d => d.Severity == Severity.Orphan)) return Severity.Orphan;
        if (diags.Any(d => d.Severity == Severity.Warning)) return Severity.Warning;
        if (diags.Any(d => d.Severity == Severity.Unknown)) return Severity.Unknown;
        return Severity.Healthy;
    }

    private static string NormalizeImageId(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId)) return string.Empty;

        // El campo suele venir como "ul.<GAME_ID>" (15 bytes).
        const string prefix = "ul.";
        return imageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? imageId.Substring(prefix.Length)
            : imageId;
    }

    private sealed record UlFileEntry(string FileName, UlFilenameParts Parts);
}
