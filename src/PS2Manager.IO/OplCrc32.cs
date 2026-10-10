using System.Text;

namespace PS2Manager.IO;

/// <summary>
/// Reproduce el crc32(const char*) de pc/iso2opl/src/iso2opl.c y
/// pc/opl2iso/src/opl2iso.c de Open PS2 Loader.
/// No es el CRC-32 estándar: la tabla se genera y consulta tal como en el C original.
/// </summary>
public static class OplCrc32
{
    private const uint Polynomial = 0x04C11DB7u;
    private static readonly (uint[] Table, uint InitialCrc) Data = BuildTable();

    private static (uint[] Table, uint InitialCrc) BuildTable()
    {
        var table = new uint[256];
        uint crc = 0;

        for (int index = 0; index < 256; index++)
        {
            crc = unchecked((uint)(index << 24));

            for (int bit = 8; bit > 0; bit--)
            {
                // Emula el comportamiento de int de 32 bits del código C original.
                int signedCrc = unchecked((int)crc);
                crc = signedCrc < 0
                    ? unchecked(crc << 1)
                    : unchecked((crc << 1) ^ Polynomial);
            }

            // El código OPL almacena la tabla en orden inverso.
            table[255 - index] = crc;
        }

        // En el C original, crc no se reinicia después de construir la tabla.
        // El valor que queda tras index=255 es el estado inicial del cálculo.
        return (table, crc);
    }

    /// <summary>
    /// Procesa los bytes suministrados usando el estado inicial derivado del
    /// código fuente OPL. No añade un terminador automáticamente.
    /// El parámetro initial existe solo para pruebas/diagnóstico de bajo nivel.
    /// </summary>
    public static uint Compute(ReadOnlySpan<byte> data, uint? initial = null)
    {
        uint crc = initial ?? Data.InitialCrc;

        foreach (byte value in data)
        {
            int tableIndex = value ^ (int)((crc >> 24) & 0xFF);
            crc = Data.Table[tableIndex] ^ ((crc << 8) & 0xFFFFFF00u);
        }

        return crc;
    }

    /// <summary>
    /// Calcula el CRC del nombre de juego como hace crc32(game_name) en OPL.
    /// El bucle do/while del código C procesa también el byte NUL final.
    /// </summary>
    public static uint ComputeGameName(string gameName)
    {
        ArgumentNullException.ThrowIfNull(gameName);

        byte[] nameBytes = Encoding.Latin1.GetBytes(gameName);
        var terminated = new byte[nameBytes.Length + 1];
        nameBytes.CopyTo(terminated, 0);
        // El último byte queda en 0x00, igual que el terminador C.
        return Compute(terminated);
    }

    public static string Format(uint crc) => crc.ToString("X8");
}
