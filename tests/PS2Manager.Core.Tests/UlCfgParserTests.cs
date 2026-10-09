using System.Text;
using PS2Manager.Core.Diagnostics;
using PS2Manager.Core.Models;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class UlCfgParserTests
{
    [Fact]
    public void EmptyFile_ReportsWarning()
    {
        var result = UlCfgParser.Parse(ReadOnlySpan<byte>.Empty);
        Assert.Empty(result.Records);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl001 && d.Severity == Severity.Warning);
    }

    [Fact]
    public void Length65_ParsesCompleteRecordAndReportsTrailingByte()
    {
        var bytes = new byte[65];
        var result = UlCfgParser.Parse(bytes);

        Assert.Single(result.Records);
        var diag = Assert.Single(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl001);
        Assert.Equal(Severity.Error, diag.Severity);
        Assert.Contains("resto 1", diag.Message);
        Assert.Contains("ignoran los 1 sobrantes", diag.Message);
    }

    [Fact]
    public void ValidRecord_ParsesAllFields()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Fixture UL Game", "ul.SLUS_000.00", 2))
            .Build();

        var result = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));

        Assert.Empty(result.Diagnostics);
        var rec = Assert.Single(result.Records);
        Assert.Equal("Fixture UL Game", rec.GameName);
        Assert.Equal("ul.SLUS_000.00", rec.ImageIdentifier);
        Assert.Equal((byte)2, rec.DeclaredPartCount);
        Assert.Equal(UlMediaType.Dvd, rec.Media);
    }

    [Fact]
    public void UnknownMedia_ReportsUnknown_NotReinterpreted()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(new UlCfgRecordSpec("X", "ul.SLUS_000.00", 1, 0x99))
            .Build();

        var result = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));

        var rec = Assert.Single(result.Records);
        Assert.Equal(UlMediaType.Unknown, rec.Media);
        Assert.Equal((byte)0x99, rec.RawMediaByte);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl002 && d.Severity == Severity.Unknown);
    }

    [Fact]
    public void EmptyGameName_ReportsOpl007_ButStillParses()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(new UlCfgRecordSpec("", "ul.SLUS_000.00", 1, 0x14))
            .Build();

        var result = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));

        Assert.Single(result.Records);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.Opl007 && d.Severity == Severity.Warning);
    }

    [Fact]
    public void EmptyImageIdentifier_ReportsOpl007()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(new UlCfgRecordSpec("X", "", 1, 0x14))
            .Build();

        var result = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));

        Assert.Single(result.Records);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.Opl007);
    }

    [Fact]
    public void MultipleRecords_AllParsed()
    {
        var fs = new FixtureBuilder()
            .WithUlCfg(
                UlCfgRecordSpec.Dvd("A", "ul.SLUS_000.00", 1),
                UlCfgRecordSpec.Cd("B", "ul.SLUS_000.01", 2))
            .Build();

        var result = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));

        Assert.Equal(2, result.Records.Count);
        Assert.Equal(UlMediaType.Dvd, result.Records[0].Media);
        Assert.Equal(UlMediaType.Cd, result.Records[1].Media);
    }
}
