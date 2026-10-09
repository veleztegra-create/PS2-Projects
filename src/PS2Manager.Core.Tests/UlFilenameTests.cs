using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class UlFilenameTests
{
    [Theory]
    // Game IDs reales con guión bajo y punto.
    [InlineData("ul.A1B2C3D4.SLUS_000.00.00", true, "SLUS_000.00", 0)]
    // Game ID con guión.
    [InlineData("ul.A1B2C3D4.SLUS-000.00.00", true, "SLUS-000.00", 0)]
    // Game ID con guión bajo simple y parte 01.
    [InlineData("ul.A1B2C3D4.SCES_123.45.01", true, "SCES_123.45", 1)]
    // Parte 99.
    [InlineData("ul.A1B2C3D4.SLUS_000.00.99", true, "SLUS_000.00", 99)]
    // CRC en minúsculas.
    [InlineData("ul.a1b2c3d4.SLUS_000.00.00", true, "SLUS_000.00", 0)]
    // Game ID con múltiples puntos internos.
    [InlineData("ul.A1B2C3D4.SLUS_000.00.05.07", true, "SLUS_000.00.05", 7)]
    public void Valid(string input, bool ok, string? gameId, int part)
    {
        Assert.Equal(ok, UlFilename.TryParse(input, out var parsed));
        if (ok)
        {
            Assert.Equal(gameId, parsed.GameId);
            Assert.Equal(part, parsed.PartNumber);
        }
    }

    [Theory]
    [InlineData("ul.A1B2C3D4.SLUS_000.00.0")]    // 1 dígito
    [InlineData("ul.A1B2C3D4.SLUS_000.00.000")]  // 3 dígitos
    [InlineData("ul.A1B2C3D.SLUS_000.00.00")]    // CRC 7 hex
    [InlineData("ul.A1B2C3D4Z.SLUS_000.00.00")]  // CRC con carácter no hex
    [InlineData("ul.A1B2C3D4..00")]              // Game ID vacío
    [InlineData("ul.A1B2C3D4.SLUS 000.00.00")]   // espacio en Game ID
    [InlineData("UL.A1B2C3D4.SLUS_000.00.00")]   // "UL" mayúsculas — no matchea (case-sensitive)
    [InlineData("ul.A1B2C3D4.SLUS_000.00.00.bak")] // extensión extra
    [InlineData("notul.A1B2C3D4.SLUS_000.00.00")]
    [InlineData("")]
    public void Invalid(string input)
    {
        Assert.False(UlFilename.TryParse(input, out _));
    }

    [Fact]
    public void NonGreedy_GameIdCapture_PrefersShortestLeftmost()
    {
        Assert.True(UlFilename.TryParse("ul.A1B2C3D4.SLUS_000.00.00", out var p));
        Assert.Equal("SLUS_000.00", p.GameId);
        Assert.Equal(0, p.PartNumber);
    }
}
