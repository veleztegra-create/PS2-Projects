using System.Text.RegularExpressions;

namespace PS2Manager.IO;

/// <summary>
/// Parser del nombre de archivo UL: ul.&lt;CRC8&gt;.&lt;GAME_ID&gt;.&lt;NN&gt;
///
/// Reglas:
///   - CRC: exactamente 8 caracteres hexadecimales.
///   - GAME_ID: al menos 1 carácter del set [A-Za-z0-9_.-]. Puede contener puntos
///     internos porque IDs como "SLUS_000.00" los tienen.
///   - NN: exactamente 2 dígitos decimales. OPL escribe %02x.
///
/// El GameId se captura de forma NO-greedy para que un nombre como
/// "ul.A1B2C3D4.SLUS_000.00.00" se interprete como GameId="SLUS_000.00", parte=0,
/// y no como GameId="SLUS_000.00.00".
/// </summary>
public static partial class UlFilename
{
    [GeneratedRegex(
        @"^ul\.([0-9A-Fa-f]{8})\.([A-Za-z0-9_\-\.]+?)\.(\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();

    public static bool TryParse(string fileName, out UlFilenameParts parts)
    {
        parts = default;
        if (string.IsNullOrEmpty(fileName)) return false;

        var m = Pattern().Match(fileName);
        if (!m.Success) return false;

        parts = new UlFilenameParts(
            CrcHex: m.Groups[1].Value.ToUpperInvariant(),
            GameId: m.Groups[2].Value,
            PartNumber: int.Parse(m.Groups[3].Value));
        return true;
    }
}

public readonly record struct UlFilenameParts(
    string CrcHex,
    string GameId,
    int PartNumber);
