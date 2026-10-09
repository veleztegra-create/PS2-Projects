using System.Text;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

/// <summary>
/// Constructor de fixtures sintéticos. Genera ul.cfg (64 bytes por registro)
/// y archivos ul.* con los nombres que el analyzer debe reconocer.
///
/// El CRC no se hardcodea: se calcula con OplCrc32 para que los tests sean
/// auto-consistentes con la implementación. Para anclar el CRC contra OPL real
/// hace falta un vector externo — ver docs/crc-uncertainty.md.
/// </summary>
internal sealed class FixtureBuilder
{
    private readonly InMemoryFileSystem _fs = new();

    public FixtureBuilder WithUlCfg(params UlCfgRecordSpec[] records)
    {
        var bytes = new byte[records.Length * UlCfgParser.RecordSize];
        for (int i = 0; i < records.Length; i++)
        {
            var r = records[i];
            int offset = i * UlCfgParser.RecordSize;

            WriteFixed(bytes, offset + 0x00, 32, r.GameName);
            WriteFixed(bytes, offset + 0x20, 15, r.ImageIdentifier);
            bytes[offset + 0x2F] = r.PartCount;
            bytes[offset + 0x30] = r.MediaByte;
            // 0x31..0x3F queda en 0x00.
        }
        _fs.AddFile("ul.cfg", bytes);
        return this;
    }

    public FixtureBuilder WithRawUlCfg(byte[] raw)
    {
        _fs.AddFile("ul.cfg", raw);
        return this;
    }

    /// <summary>Crea una parte UL con CRC correcto para el nombre dado.</summary>
    public FixtureBuilder WithUlPartForName(
        string gameName, string gameId, int partNumber, int size = 1024)
    {
        string crc = OplCrc32.Format(OplCrc32.ComputeGameName(gameName));
        string name = $"ul.{crc}.{gameId}.{partNumber:D2}";
        _fs.AddFile(name, new byte[size]);
        return this;
    }

    /// <summary>Crea una parte UL con CRC explícito (para probar CRC mismatch).</summary>
    public FixtureBuilder WithUlPartExplicitCrc(
        string crcHex, string gameId, int partNumber, int size = 1024)
    {
        string name = $"ul.{crcHex}.{gameId}.{partNumber:D2}";
        _fs.AddFile(name, new byte[size]);
        return this;
    }

    public FixtureBuilder WithArbitraryFile(string fileName, int size = 16)
    {
        _fs.AddFile(fileName, new byte[size]);
        return this;
    }

    public InMemoryFileSystem Build() => _fs;

    private static void WriteFixed(byte[] buf, int offset, int size, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        int copy = Math.Min(bytes.Length, size - 1); // dejar al menos 1 byte nulo
        Array.Copy(bytes, 0, buf, offset, copy);
    }
}

internal sealed record UlCfgRecordSpec(
    string GameName,
    string ImageIdentifier,
    byte PartCount,
    byte MediaByte)
{
    public static UlCfgRecordSpec Cd(string name, string id, byte parts) =>
        new(name, id, parts, 0x12);

    public static UlCfgRecordSpec Dvd(string name, string id, byte parts) =>
        new(name, id, parts, 0x14);
}
