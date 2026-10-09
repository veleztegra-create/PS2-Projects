using System.Text;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class OplCrc32Tests
{
    [Fact(Skip = "Requiere vector real (gameName → CRC hex) de un ul.cfg generado por OPL. " +
                 "Ver docs/crc-uncertainty.md.")]
    public void KnownVector_FromOplSource()
    {
        // Cuando se provea un vector verificado, este test queda como ancla.
        // Ejemplo de forma esperada:
        //   string gameName = "...";
        //   string expectedHex = "XXXXXXXX";
        //   Assert.Equal(expectedHex, OplCrc32.Format(OplCrc32.ComputeGameName(gameName)));
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var a = OplCrc32.Compute(Encoding.Latin1.GetBytes("Fixture UL Game"));
        var b = OplCrc32.Compute(Encoding.Latin1.GetBytes("Fixture UL Game"));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Compute_EmptyInput_ReturnsInitial()
    {
        Assert.Equal(0u, OplCrc32.Compute(ReadOnlySpan<byte>.Empty));
        Assert.Equal(0xDEADBEEFu,
            OplCrc32.Compute(ReadOnlySpan<byte>.Empty, initial: 0xDEADBEEFu));
    }

    [Fact]
    public void Compute_DifferentNames_ProduceDifferentValues()
    {
        var a = OplCrc32.ComputeGameName("Game A");
        var b = OplCrc32.ComputeGameName("Game B");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ComputeGameName_NullTerminator_ChangesResult()
    {
        // El resultado CON nulo debe diferir del resultado SIN nulo.
        // Esto no decide cuál es correcto para OPL — solo documenta que la
        // elección importa.
        var without = OplCrc32.ComputeGameName("X", includeNullTerminator: false);
        var with    = OplCrc32.ComputeGameName("X", includeNullTerminator: true);
        Assert.NotEqual(without, with);
    }

    [Fact]
    public void Format_IsEightUppercaseHex()
    {
        string s = OplCrc32.Format(0x0A1B2C3D);
        Assert.Equal("0A1B2C3D", s);
        Assert.Equal(8, s.Length);
    }

    [Fact]
    public void Compute_Latin1PreservesHighBytes()
    {
        // 'é' en Latin-1 es 0xE9. No debe convertirse a UTF-8 de 2 bytes.
        var latin1 = OplCrc32.Compute(new byte[] { 0xE9 });
        var utf8   = OplCrc32.Compute(Encoding.UTF8.GetBytes("é"));
        Assert.NotEqual(latin1, utf8);
    }
}
