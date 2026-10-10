using PS2Manager.Core.Analysis;
using PS2Manager.Core.Diagnostics;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class UlAnalyzerTests
{
    [Fact]
    public void MissingParts_ReportsOpl003()
    {
        // 1. Arrange: Creamos un ul.cfg con un juego, pero NO creamos los archivos ul.* en el disco simulado
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Incomplete Game", "ul.SLUS_000.00", 1))
            .Build();

        var parseResult = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));
        var fileNames = fs.GetFileNames();

        // 2. Act: Ejecutamos el UlAnalyzer real
        var analysisResult = UlAnalyzer.Analyze(parseResult.Records, fileNames);

        // 3. Assert: Verificamos que se emita el diagnóstico Opl003 por fragmentos faltantes
        var diagnostic = Assert.Single(analysisResult.Diagnostics);
        Assert.Equal(DiagnosticCodes.Opl003, diagnostic.Code);
        Assert.Equal(Severity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void OrphanFiles_ReportsOpl004()
    {
        // 1. Arrange: Un ul.cfg vacío pero con un archivo ul.* suelto en el disco
        var fs = new FixtureBuilder()
            .WithRawUlCfg(Array.Empty<byte>())
            .WithArbitraryFile("ul.A1B2C3D4.SLUS_000.00.00")
            .Build();

        var parseResult = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));
        var fileNames = fs.GetFileNames();

        // 2. Act: Ejecutamos el UlAnalyzer
        var analysisResult = UlAnalyzer.Analyze(parseResult.Records, fileNames);

        // 3. Assert: Verificamos que se detecte el archivo huérfano con Opl004
        var diagnostic = Assert.Single(analysisResult.Diagnostics);
        Assert.Equal(DiagnosticCodes.Opl004, diagnostic.Code);
    }
}
