namespace PS2Manager.IO;

/// <summary>
/// CRC32 compatible con Open PS2 Loader (pc/iso2opl).
/// NO sustituir por System.IO.Hashing.Crc32 ni por CRC32 estándar reflejado.
/// </summary>
public static class OplCrc32
{
    private const uint Polynomial = 0x04C11DB7u;
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i << 24;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x80000000u) != 0)
                    crc = (crc << 1) ^ Polynomial;
                else
                    crc <<= 1;
            }
            // OPL almacena en índice invertido.
            t[255 - i] = crc;
        }
        return t;
    }

    /// <summary>Calcula CRC32 estilo OPL sobre bytes. Estado inicial = 0.</summary>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0;
        foreach (byte b in data)
            crc = (crc << 8) ^ Table[((crc >> 24) ^ b) & 0xFF];
        return crc;
    }

    /// <summary>CRC sobre el nombre del juego en ASCII/Latin-1 + terminador nulo.</summary>
    public static uint ComputeGameName(string gameName, bool includeNullTerminator = true)
    {
        // OPL procesa la cadena hasta el nulo. El byte nulo participa o no,
        // según la variante del tool. Por defecto lo incluimos (ver docs/ul-format.md).
        var bytes = System.Text.Encoding.Latin1.GetBytes(gameName);
        Span<byte> buffer = includeNullTerminator
            ? new byte[bytes.Length + 1]
            : new byte[bytes.Length];
        bytes.CopyTo(buffer);
        return Compute(buffer);
    }

    public static string Format(uint crc) => crc.ToString("X8");
}
