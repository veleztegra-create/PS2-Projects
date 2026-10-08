using PS2Manager.Core.Models;

namespace PS2Manager.IO;

public static class UlCfgParser
{
    public const int RecordSize = 64;

    public static UlCfgParseResult Parse(ReadOnlySpan<byte> bytes)
    {
        var diagnostics = new List<Diagnostic>();

        if (bytes.Length == 0)
        {
            diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl001,
                Severity.Warning, "ul.cfg está vacío."));
            return new UlCfgParseResult(Array.Empty<UlCfgRecord>(), diagnostics);
        }

        if (bytes.Length % RecordSize != 0)
        {
            int remainder = bytes.Length % RecordSize;
            diagnostics.Add(new Diagnostic(DiagnosticCodes.Opl001,
                Severity.Error,
                $"ul.cfg length {bytes.Length} no divisible por 64 (resto {remainder})."));
            // No intentamos parsear parcialmente: preferimos no fabricar datos.
            return new UlCfgParseResult(Array.Empty<UlCfgRecord>(), diagnostics);
        }

        int count = bytes.Length / RecordSize;
        var records = new List<UlCfgRecord>(count);

        for (int i = 0; i < count; i++)
        {
            var slice = bytes.Slice(i * RecordSize, RecordSize);
            records.Add(ParseRecord(slice, i, diagnostics));
        }

        return new UlCfgParseResult(records, diagnostics);
    }

    private static UlCfgRecord ParseRecord(ReadOnlySpan<byte> rec, int index, List<Diagnostic> diags)
    {
        string name = ReadFixedString(rec.Slice(0x00, 32));
        string imageId = ReadFixedString(rec.Slice(0x20, 15));
        byte partCount = rec[0x2F];
        byte media = rec[0x30];
        // 0x31..0x3F reservado; no lo exponemos.

        var mediaType = UlMediaTypeExtensions.FromByte(media);
        if (mediaType == UlMediaType.Unknown)
        {
            diags.Add(new Diagnostic(DiagnosticCodes.Opl002,
                Severity.Unknown,
                $"Registro #{index} ('{name}'): media 0x{media:X2} desconocido."));
        }

        return new UlCfgRecord(
            Index: index,
            GameName: name,
            ImageIdentifier: imageId,
            DeclaredPartCount: partCount,
            Media: mediaType,
            RawMediaByte: media);
    }

    private static string ReadFixedString(ReadOnlySpan<byte> span)
    {
        int end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        return System.Text.Encoding.Latin1.GetString(span.Slice(0, end)).TrimEnd();
    }
}

public sealed record UlCfgParseResult(
    IReadOnlyList<UlCfgRecord> Records,
    IReadOnlyList<Diagnostic> Diagnostics);
