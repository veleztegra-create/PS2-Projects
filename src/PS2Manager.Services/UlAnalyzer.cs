public sealed class UlAnalyzer
{
    private readonly IFileSystem _fs;

    public UlAnalyzer(IFileSystem fs) { _fs = fs; }

    public UlAnalysisResult Analyze(string rootPath, UlAnalyzerOptions options)
    {
        var diagnostics = new List<Diagnostic>();
        var games = new List<UlGame>();

        // 1. Leer ul.cfg si existe
        var cfgPath = _fs.Combine(rootPath, "ul.cfg");
        UlCfgParseResult cfg;
        if (_fs.FileExists(cfgPath))
        {
            var bytes = _fs.ReadAllBytes(cfgPath);
            cfg = UlCfgParser.Parse(bytes);
            diagnostics.AddRange(cfg.Diagnostics);
        }
        else
        {
            cfg = new UlCfgParseResult(Array.Empty<UlCfgRecord>(), Array.Empty<Diagnostic>());
        }

        // 2. Enumerar archivos ul.*
        var ulFiles = _fs.EnumerateFiles(rootPath)
            .Select(p => _fs.GetFileName(p))
            .Where(n => n.StartsWith("ul.", StringComparison.OrdinalIgnoreCase)
                        && !n.Equals("ul.cfg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 3. Asociar por (CRC, GameId)
        var partsByKey = new Dictionary<(string Crc, string Id), List<UlFilenameParts>>();
        var unparsed = new List<string>();
        foreach (var name in ulFiles)
        {
            if (UlFilename.TryParse(name, out var fp))
            {
                var key = (fp.CrcHex, fp.GameId);
                if (!partsByKey.TryGetValue(key, out var list))
                    partsByKey[key] = list = new List<UlFilenameParts>();
                list.Add(fp);
            }
            else unparsed.Add(name);
        }

        // 4. Por cada registro, resolver sus partes
        var usedKeys = new HashSet<(string, string)>();
        foreach (var rec in cfg.Records)
        {
            var expectedCrc = OplCrc32.Format(OplCrc32.ComputeGameName(rec.GameName));
            var expectedId = NormalizeImageId(rec.ImageIdentifier); // quita "ul." prefix

            var key = (expectedCrc, expectedId);
            usedKeys.Add(key);

            if (!partsByKey.TryGetValue(key, out var foundParts))
            {
                // Registro sin partes: OPL003 sobre cada parte esperada.
                for (int i = 0; i < rec.DeclaredPartCount; i++)
                    diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl003, Severity.Missing,
                        $"Registro '{rec.GameName}': falta parte {i:D2}."));
                games.Add(new UlGame(rec.GameName, expectedId, rec.Media,
                    rec.DeclaredPartCount, Array.Empty<UlGamePart>(), Severity.Missing));
                continue;
            }

            var actualNumbers = foundParts.Select(p => p.PartNumber).OrderBy(n => n).ToList();
            var expectedNumbers = Enumerable.Range(0, rec.DeclaredPartCount).ToHashSet();

            foreach (var n in expectedNumbers.Except(actualNumbers))
                diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl003, Severity.Missing,
                    $"'{rec.GameName}': falta parte {n:D2}."));

            foreach (var n in actualNumbers.Except(expectedNumbers))
                diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl006, Severity.Warning,
                    $"'{rec.GameName}': parte sobrante {n:D2} (declaradas {rec.DeclaredPartCount})."));

            // CRC mismatch: si el nombre del registro produce un CRC distinto al del filename.
            foreach (var p in foundParts)
            {
                if (!string.Equals(p.CrcHex, expectedCrc, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl005, Severity.Error,
                        $"'{rec.GameName}': CRC filename {p.CrcHex} != esperado {expectedCrc}."));
                }
            }

            var sev = diagnostics.Any(d => d.Severity == Severity.Error) ? Severity.Error : Severity.Healthy;
            games.Add(new UlGame(rec.GameName, expectedId, rec.Media,
                rec.DeclaredPartCount,
                foundParts.Select(p => new UlGamePart(p.PartNumber, p.CrcHex)).ToList(),
                sev));
        }

        // 5. Huérfanos: partes cuyo key no coincide con ningún registro
        foreach (var kv in partsByKey)
        {
            if (!usedKeys.Contains(kv.Key))
            {
                foreach (var p in kv.Value)
                    diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl004, Severity.Orphan,
                        $"Parte huérfana {p.CrcHex}.{p.GameId}.{p.PartNumber:D2} sin registro en ul.cfg."));
            }
        }

        // 6. Archivos "ul.*" que no parsean
        foreach (var bad in unparsed)
            diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl007, Severity.Warning,
                $"Nombre UL no reconocido: {bad}"));

        return new UlAnalysisResult(games, diagnostics);
    }

    private static string NormalizeImageId(string imageId)
    {
        // El campo suele venir como "ul.<GAME_ID>" (15 bytes). Quitamos prefijo.
        const string prefix = "ul.";
        return imageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? imageId.Substring(prefix.Length)
            : imageId;
    }
}
