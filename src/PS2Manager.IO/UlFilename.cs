using System.Text.RegularExpressions;

namespace PS2Manager.IO;

/// <summary>Parser de nombres ul.&lt;CRC&gt;.&lt;GAME_ID&gt;.&lt;NN&gt;.</summary>
public static partial class UlFilename
{
    [GeneratedRegex(@"^ul\.([0-9A-Fa-f]{8})\.([A-Za-z0-9_\-\.]+)\.(\d{2})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool TryParse(string fileName, out UlFilenameParts parts)
    {
        parts = default;
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
    string CrcHex, string GameId, int PartNumber);
