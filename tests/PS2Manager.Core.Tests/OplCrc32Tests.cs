using System.Text;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class OplCrc32Tests
{
    [Fact]
    public void ComputeGameName_MatchesSourceDerivedReferenceVector()
    {
        // Vectores comprobados con un pequeño harness C compilado a partir de
        // la rutina crc32() de iso2opl.c / opl2iso.c. No equivalen a validar
        // una biblioteca generada por una consola o por OPL ejecutándose.
        Assert.Equal("41552A45", OplCrc32.Format(OplCrc32.ComputeGameName("Fixture UL Game")));
        Assert.Equal("DB5FB40D", OplCrc32.Format(OplCrc32.ComputeGameName("X")));
        Assert.Equal("00000000", OplCrc32.Format(OplCrc32.ComputeGameName("")));
    }

    [Fact]
    public void ComputeGameName_MatchesRealOplUlCfgAndPartFilename()
    {
        // Validación de muestra real aportada por el usuario:
        // ul.cfg contiene "Harry Potter to Kenja no Ishi" y el conjunto de
        // archivos incluye ul.E8C54EAD.SLPM_654.65.01.
        // El CRC se deriva del nombre del juego, no del identificador SLPM.
        Assert.Equal(
            "E8C54EAD",
            OplCrc32.Format(OplCrc32.ComputeGameName("Harry Potter to Kenja no Ishi")));
    }

    [Fact]
    public void ComputeGameName_MatchesSecondReportedRealSample()
    {
        // El usuario identifica el registro como "Curious George" y reporta
        // archivos ul.24DE05BF.SLUS_213.54.xx. El cálculo coincide con el prefijo.
        Assert.Equal(
            "24DE05BF",
            OplCrc32.Format(OplCrc32.ComputeGameName("Curious George")));
    }

    [Fact]
    public void ComputeGameName_MatchesPrefixedTitleStoredInUlCfg()
    {
        // Registro real de 64 bytes compartido por el usuario:
        // el campo name[32] contiene "SLUS_623.90.Super Mario 64 ESP" + NUL.
        // OPL calcula el CRC de la cadena almacenada completa, incluido el ID
        // y el punto; no del título comercial separado.
        Assert.Equal(
            "32D7DD31",
            OplCrc32.Format(OplCrc32.ComputeGameName("SLUS_623.90.Super Mario 64 ESP")));
    }

    [Fact]
    public void ComputeGameName_MatchesReportedDisneyBoltTitleCandidate()
    {
        // La lista de colección asocia SLES_554.29 con Disney Bolt y reporta
        // el prefijo B8913F43. El título limpio produce ese CRC; sin el registro
        // binario, se trata de una coincidencia candidata, no de una prueba del
        // texto exacto almacenado en name[32].
        Assert.Equal(
            "B8913F43",
            OplCrc32.Format(OplCrc32.ComputeGameName("Disney Bolt")));
    }

    [Fact]
    public void ComputeGameName_MatchesReportedPigletTitleCandidate()
    {
        // El usuario reportó ul.270B457C.SLES_516.66.xx.
        // El título "Piglet el Gran Juego" produce ese CRC; la coincidencia
        // debe considerarse candidata hasta inspeccionar el registro binario.
        Assert.Equal(
            "270B457C",
            OplCrc32.Format(OplCrc32.ComputeGameName("Piglet el Gran Juego")));
    }

    [Fact]
    public void ComputeGameName_MatchesReportedStitchTitleCandidate()
    {
        // El usuario reportó ul.02CAA445.SCES_509.59.xx.
        // "Stitch Experiment 626" produce ese CRC; el nombre exacto almacenado
        // en el ul.cfg de esa unidad aún debe confirmarse con sus bytes.
        Assert.Equal(
            "02CAA445",
            OplCrc32.Format(OplCrc32.ComputeGameName("Stitch Experiment 626")));
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
