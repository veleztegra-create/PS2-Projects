using System.Text;
using PS2Manager.Core.Diagnostics;
using PS2Manager.Core.Models;

namespace PS2Manager.IO;

/// <summary>
/// Parser de ul.cfg. Cada registro ocupa exactamente 64 bytes.
/// Conserva los registros completos aunque haya bytes sobrantes al final.
/// </summary>
public static class UlCfgParser
{
    public const int RecordSize = 64;
    public const int GameNameSize = 32;
    public const int ImageIdSize = 15;
    public const int PartCountOffset = 0x2F;
    public const int MediaOffset = 0x30;

    public static UlCfgParseResult Parse(ReadOnlySpan<byte> bytes)
    {
        var diagnostics = new List<Diagnostic>();
        var recordDiagnostics = new Dictionary<int, IReadOnlyList<Diagnostic>>();

        if (bytes.Length == 0)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticCodes.Opl001,
                Severity.Warning,
                "ul.cfg está vacío (0 bytes)."));
            return new UlCfgParseResult(Array.Empty<UlCfgRecord>(), diagnostics)
            {
                RecordDiagnostics = recordDiagnostics
            };
        }

        int remainder = bytes.Length % RecordSize;
        if (remainder != 0)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticCodes.Opl001,
                Severity.Error,
                $"ul.cfg length {bytes.Length} no divisible por {RecordSize} (resto {remainder}). " +
                $"Se parsean los {bytes.Length - remainder} bytes completos y se ignoran los {remainder} sobrantes."));
        }

        int count = bytes.Length / RecordSize;
        var records = new List<UlCfgRecord>(count);

        for (int i = 0; i < count; i++)
        {
            var localDiagnostics = new List<Diagnostic>();
            var slice = bytes.Slice(i * RecordSize, RecordSize);
            records.Add(ParseRecord(slice, i, localDiagnostics));
            if (localDiagnostics.Count > 0)
            {
                recordDiagnostics[i] = localDiagnostics.ToArray();
                diagnostics.AddRange(localDiagnostics);
            }
        }

        return new UlCfgParseResult(records, diagnostics)
        {
            RecordDiagnostics = recordDiagnostics
        };
    }

    private static UlCfgRecord ParseRecord(
        ReadOnlySpan<byte> rec, int index, List<Diagnostic> diags)
    {
        string name = ReadFixedString(rec.Slice(0x00, GameNameSize));
        string imageId = ReadFixedString(rec.Slice(0x20, ImageIdSize));
        byte partCount = rec[PartCountOffset];
        byte media = rec[MediaOffset];

        if (string.IsNullOrWhiteSpace(name))
        {
            diags.Add(new Diagnostic(
                DiagnosticCodes.Opl007,
                Severity.Warning,
                $"Registro #{index}: GameName vacío."));
        }

        if (string.IsNullOrWhiteSpace(imageId))
        {
            diags.Add(new Diagnostic(
                DiagnosticCodes.Opl007,
                Severity.Warning,
                $"Registro #{index} ('{name}'): ImageIdentifier vacío."));
        }

        var mediaType = UlMediaTypeExtensions.FromByte(media);
        if (mediaType == UlMediaType.Unknown)
        {
            diags.Add(new Diagnostic(
                DiagnosticCodes.Opl002,
                Severity.Unknown,
                $"Registro #{index} ('{name}'): media 0x{media:X2} desconocido."));
        }

        return new UlCfgRecord(index, name, imageId, partCount, mediaType, media);
    }

    private static string ReadFixedString(ReadOnlySpan<byte> span)
    {
        int end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        // No recortar espacios: pueden formar parte de los bytes usados para el CRC.
        return Encoding.Latin1.GetString(span.Slice(0, end));
    }
}
