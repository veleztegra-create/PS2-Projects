using System.Text;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class OplCrc32Tests
{
    [Fact]
    public void ComputeGameName_MatchesSourceDerivedReferenceVector()
    {
        // Vector fijado a partir de una traducción independiente del crc32()
        // publicado en pc/iso2opl/src/iso2opl.c y pc/opl2iso/src/opl2iso.c.
        // No sustituye una comprobación contra un ul.cfg producido por hardware/OPL.
        Assert.Equal("84BCFF5D", OplCrc32.Format(OplCrc32.ComputeGameName("Fixture UL Game")));
        Assert.Equal("8D19B75D", OplCrc32.Format(OplCrc32.ComputeGameName("X")));
        Assert.Equal("52F492A1", OplCrc32.Format(OplCrc32.ComputeGameName("")));
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var bytes = Encoding.Latin1.GetBytes("Fixture UL Game");
        Assert.Equal(OplCrc32.Compute(bytes), OplCrc32.Compute(bytes));
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
        Assert.NotEqual(OplCrc32.ComputeGameName("Game A"), OplCrc32.ComputeGameName("Game B"));
    }

    [Fact]
    public void ComputeGameName_IncludesNullTerminator()
    {
        var nameBytes = Encoding.Latin1.GetBytes("X");
        Assert.NotEqual(
            OplCrc32.Compute(nameBytes),
            OplCrc32.ComputeGameName("X"));
    }

    [Fact]
    public void Format_IsEightUppercaseHex()
    {
        Assert.Equal("0A1B2C3D", OplCrc32.Format(0x0A1B2C3D));
    }

    [Fact]
    public void Compute_Latin1PreservesHighBytes()
    {
        var latin1 = OplCrc32.Compute(new byte[] { 0xE9 });
        var utf8 = OplCrc32.Compute(Encoding.UTF8.GetBytes("é"));
        Assert.NotEqual(latin1, utf8);
    }
}
