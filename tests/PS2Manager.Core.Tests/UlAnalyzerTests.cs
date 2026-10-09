using PS2Manager.Core.Diagnostics;
using PS2Manager.Core.Models;
using PS2Manager.Services;

namespace PS2Manager.Core.Tests;

public class UlAnalyzerTests
{
    private static UlAnalysisResult Run(FixtureBuilder b) =>
        new UlAnalyzer(b.Build()).Analyze();

    // ---------- Fixture 03 — UL válido ----------
    [Fact]
    public void Fixture03_ValidUlLibrary_IsHealthy()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Fixture UL Game", "ul.SLUS_000.00", 2))
            .WithUlPartForName("Fixture UL Game", "SLUS_000.00", 0)
            .WithUlPartForName("Fixture UL Game", "SLUS_000.00", 1);

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Equal(Severity.Healthy, game.Severity);
        Assert.Equal(2, game.Parts.Count);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == Severity.Error);
    }

    [Fact]
    public void Fixture03_CdMedia_IsHealthy()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Cd("Fixture CD", "ul.SLUS_000.00", 1))
            .WithUlPartForName("Fixture CD", "SLUS_000.00", 0);

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Equal(UlMediaType.Cd, game.Media);
        Assert.Equal(Severity.Healthy, game.Severity);
    }

    // ---------- Fixture 05 — parte faltante ----------
    [Fact]
    public void Fixture05_MissingPart_NotHealthy_ContainsOpl003()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Missing Part Game", "ul.SLUS_000.00", 3))
            .WithUlPartForName("Missing Part Game", "SLUS_000.00", 0)
            .WithUlPartForName("Missing Part Game", "SLUS_000.00", 1);

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.NotEqual(Severity.Healthy, game.Severity);
        Assert.Equal(Severity.Missing, game.Severity);
        Assert.Contains(game.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl003 && d.Message.Contains("02"));
    }

    // ---------- Fixture 06 — huérfano ----------
    [Fact]
    public void Fixture06_OrphanPart_ReportedAsOrphan_NotAttachedToOtherGame()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Valid Game", "ul.SLUS_000.00", 1))
            .WithUlPartForName("Valid Game", "SLUS_000.00", 0)
            .WithUlPartExplicitCrc("FFEE0011", "SLES_999.99", 0); // no hay registro

        var result = Run(fs);

        var orphan = Assert.Single(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl004);
        Assert.Equal(Severity.Orphan, orphan.Severity);
        Assert.Contains("SLES_999.99", orphan.Message);

        // El juego válido sigue sano y no absorbió la parte huérfana.
        var game = Assert.Single(result.Games);
        Assert.Equal(Severity.Healthy, game.Severity);
        Assert.Single(game.Parts);
    }

    // ---------- Fixture 07 — CRC incorrecto ----------
    [Fact]
    public void Fixture07_BadCrc_ReportedAsOpl005_NotAsMissing_NotAsOrphan()
    {
        // El filename tiene un Game ID que coincide con el registro, pero el
        // CRC no corresponde al GameName.
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Bad CRC Game", "ul.SLUS_000.00", 1))
            .WithUlPartExplicitCrc("DEADBEEF", "SLUS_000.00", 0);

        var result = Run(fs);

        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl005);

        // NO debe aparecer como faltante ni huérfano.
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.Opl003);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.Opl004);

        var game = Assert.Single(result.Games);
        Assert.Equal(Severity.Error, game.Severity);
    }

    // ---------- Fixture 08 — longitud inválida ----------
    [Fact]
    public void Fixture08_InvalidUlCfgLength_ReportsError_NoCrash()
    {
        var fs = new FixtureBuilder()
            .WithRawUlCfg(new byte[65]);

        var result = Run(fs);

        Assert.Single(result.Games); // se conserva el registro completo de 64 bytes
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl001 && d.Severity == Severity.Error);
    }

    [Fact]
    public void DuplicateGameIdInCfg_ReportedForBothGamesAsOpl008()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(
                UlCfgRecordSpec.Dvd("First Game", "ul.SLUS_000.00", 1),
                UlCfgRecordSpec.Dvd("Second Game", "ul.SLUS_000.00", 1))
            .WithUlPartForName("First Game", "SLUS_000.00", 0);

        var result = Run(fs);

        Assert.Equal(2, result.Games.Count);
        Assert.All(result.Games, game =>
        {
            Assert.Equal(Severity.Error, game.Severity);
            Assert.Contains(game.Diagnostics, d => d.Code == DiagnosticCodes.Opl008);
        });
        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == DiagnosticCodes.Opl008));
    }

    [Fact]
    public void MetadataDiagnosticsAffectPerGameSeverity()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("", "ul.SLUS_000.00", 0));

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Equal(Severity.Warning, game.Severity);
        Assert.Contains(game.Diagnostics, d => d.Code == DiagnosticCodes.Opl007);
        // El diagnóstico del registro debe aparecer una sola vez en el resultado global.
        Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.Opl007);
    }

    // ---------- Fixture 09 — media inválida ----------
    [Fact]
    public void Fixture09_InvalidMedia_ReportsUnknown_NotCdNotDvd()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(new UlCfgRecordSpec("Weird Media", "ul.SLUS_000.00", 1, 0x99))
            .WithUlPartForName("Weird Media", "SLUS_000.00", 0);

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Equal(UlMediaType.Unknown, game.Media);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl002 && d.Severity == Severity.Unknown);
    }

    // ---------- Fixture 10 — parte sobrante ----------
    [Fact]
    public void Fixture10_ExtraPart_ReportedAsOpl006()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Extra Part", "ul.SLUS_000.00", 2))
            .WithUlPartForName("Extra Part", "SLUS_000.00", 0)
            .WithUlPartForName("Extra Part", "SLUS_000.00", 1)
            .WithUlPartForName("Extra Part", "SLUS_000.00", 2); // sobrante

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Contains(game.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl006 && d.Message.Contains("sobrante"));
        // La parte sobrante no es un Error: la severidad es Warning.
        Assert.Equal(Severity.Warning, game.Severity);
    }

    // ---------- Severidad aislada entre juegos ----------
    [Fact]
    public void MultipleGames_SeverityIsolated_OneHealthy_OneMissing()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(
                UlCfgRecordSpec.Dvd("Healthy Game", "ul.SLUS_000.00", 1),
                UlCfgRecordSpec.Dvd("Broken Game", "ul.SLUS_000.01", 2))
            .WithUlPartForName("Healthy Game", "SLUS_000.00", 0)
            .WithUlPartForName("Broken Game",  "SLUS_000.01", 0);
            // Falta la parte .01 del Broken Game.

        var result = Run(fs);

        var healthy = result.Games.Single(g => g.GameName == "Healthy Game");
        var broken  = result.Games.Single(g => g.GameName == "Broken Game");

        Assert.Equal(Severity.Healthy, healthy.Severity);
        Assert.Equal(Severity.Missing, broken.Severity);

        // El diagnóstico de faltante pertenece a Broken Game, no a Healthy.
        Assert.DoesNotContain(healthy.Diagnostics, d => d.Code == DiagnosticCodes.Opl003);
        Assert.Contains(broken.Diagnostics, d => d.Code == DiagnosticCodes.Opl003);
    }

    // ---------- Parte duplicada ----------
    [Fact]
    public void DuplicatePart_ReportedAsError()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Dup Game", "ul.SLUS_000.00", 1))
            .WithUlPartForName("Dup Game", "SLUS_000.00", 0)
            // Segunda copia con el MISMO nombre no se puede crear en FS real,
            // así que forzamos dos archivos con diferente CRC pero mismo part number.
            .WithUlPartExplicitCrc("00000000", "SLUS_000.00", 0);

        var result = Run(fs);

        var game = Assert.Single(result.Games);
        Assert.Contains(game.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl006 && d.Message.Contains("duplicada"));
        Assert.Equal(Severity.Error, game.Severity);
    }

    // ---------- ul.* no parseable ----------
    [Fact]
    public void MalformedUlFile_ReportedAsOpl007Warning()
    {
        var fs = new FixtureBuilder()
            .WithArbitraryFile("ul.NOTVALID")
            .WithArbitraryFile("README.TXT");

        var result = Run(fs);

        Assert.Empty(result.Games);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl007 && d.Severity == Severity.Warning);
    }

    // ---------- Sin ul.cfg ----------
    [Fact]
    public void NoUlCfg_NoGames_NoErrors()
    {
        var fs = new FixtureBuilder();

        var result = Run(fs);

        Assert.Empty(result.Games);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == Severity.Error);
    }
}
