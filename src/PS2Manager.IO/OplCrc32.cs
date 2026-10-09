using System.Text;

namespace PS2Manager.IO;

/// <summary>
/// CRC32 compatible con Open PS2 Loader (pc/iso2opl).
///
/// ADVERTENCIA — INCERTIDUMBRE NO RESUELTA:
///   No se pudo verificar contra el código fuente actual de OPL en esta iteración.
///   Las decisiones abiertas (estado inicial, XOR final, inclusión del nulo, storage
///   invertido de la tabla) están documentadas en docs/crc-uncertainty.md.
///
/// Lo que SÍ está fijo según docs/ul-format.md:
///   - polynomial 0x04C11DB7
///   - procesamiento MSB-first (no reflejado)
///   - string terminado en nulo
///
/// Este archivo NO debe reemplazarse por System.IO.Hashing.Crc32 ni por CRC32 reflejado.
/// </summary>
public static class OplCrc32
{
    private const uint Polynomial = 0x04C11DB7u;
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        // Tabla estándar MSB-first.
        // Si el revisor confirma que OPL usa "reversed table index 255 - i",
        // la única línea a cambiar es la asignación de t[i] por t[255 - i].
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i << 24;
            for (int j = 0; j < 8; j++)
            {
                crc = (crc & 0x80000000u) != 0
                    ? (crc << 1) ^ Polynomial
                    : crc << 1;
            }
            t[i] = crc;
        }
        return t;
    }

    /// <summary>
    /// CRC crudo sobre bytes. Parámetro initial expuesto porque el valor inicial
    /// de OPL no pudo confirmarse (ver docs/crc-uncertainty.md).
    /// </summary>
    public static uint Compute(ReadOnlySpan<byte> data, uint initial = 0)
    {
        uint crc = initial;
        foreach (byte b in data)
        {
            crc = (crc << 8) ^ Table[((crc >> 24) ^ b) & 0xFF];
        }
        return crc;
    }

    /// <summary>
    /// CRC del nombre del juego, tal como lo usa OPL para construir
    /// ul.&lt;CRC&gt;.&lt;GAME_ID&gt;.&lt;NN&gt;.
    ///
    /// includeNullTerminator se expone explícitamente y NO se asume:
    ///   - false (default): procesa solo los caracteres del nombre
    ///   - true: incluye el byte 0x00 final en el cálculo
    ///
    /// Ver docs/crc-uncertainty.md para la justificación de cada opción.
    /// </summary>
    public static uint ComputeGameName(string gameName, bool includeNullTerminator = false)
    {
        if (gameName is null) throw new ArgumentNullException(nameof(gameName));

        // Latin-1: preserva byte-a-byte, sin transformaciones UTF-8.
        var bytes = Encoding.Latin1.GetBytes(gameName);

        if (!includeNullTerminator)
        {
            return Compute(bytes);
        }

        var withNull = new byte[bytes.Length + 1];
        bytes.CopyTo(withNull, 0);
        // withNull[bytes.Length] ya es 0x00.
        return Compute(withNull);
    }

    public static string Format(uint crc) => crc.ToString("X8");
}
