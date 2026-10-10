using PS2Manager.Core.Analysis;
using PS2Manager.Core.Diagnostics;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class UlStorageAnalyzerTests
{
    [Fact]
    public void OrphanRecord_ReportsWarningOpl008()
    {
        // 1. Creamos un ul.cfg que contiene un registro válido, pero NO creamos ningún archivo ul.* en el sistema
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Missing Game", "ul.SLUS_000.00", 1))
            .Build();

        // 2. Parseamos el ul.cfg para obtener los registros
        var ulCfgBytes = fs.ReadAllBytes("ul.cfg");
        var parseResult = UlCfgParser.Parse(ulCfgBytes);

        // 3. Obtenemos la lista de archivos reales en el disco (en este caso, solo ul.cfg, sin fragmentos de juego)
        var fileNames = fs.GetFileNames(); // O la lista de archivos de la ruta analizada

        // 4. Ejecutamos el analizador de integridad cruzada
        var analysisResult = UlStorageAnalyzer.Analyze(parseResult.Records, fileNames);

        // 5. Verificamos que se haya emitido el diagnóstico Opl008 por registro huérfano
        var diagnostic = Assert.Single(analysisResult.Diagnostics);
        Assert.Equal(DiagnosticCodes.Opl008, diagnostic.Code);
        Assert.Equal(Severity.Warning, diagnostic.Severity);
        Assert.Contains("Registro huérfano detectado", diagnostic.Message);
    }

    [Fact]
    public void ValidRecordWithExistingParts_NoOrphanDiagnostics()
    {
        // 1. Creamos el ul.cfg y además simulamos la existencia del archivo de fragmentos correspondiente en el FS
        var fs = new FixtureBuilder()
            .WithUlCfg(UlCfgRecordSpec.Dvd("Complete Game", "ul.SLUS_000.00", 1))
            .WithArbitraryFile("ul.A1B2C3D4.SLUS_000.00.00") // Simula el archivo físico de la parte 00
            .Build();

        var parseResult = UlCfgParser.Parse(fs.ReadAllBytes("ul.cfg"));
        var fileNames = fs.GetFileNames();

        // 2. Ejecutamos el analizador
        var analysisResult = UlStorageAnalyzer.Analyze(parseResult.Records, fileNames);

        // 3. Comprobamos que no haya diagnósticos de registros huérfanos
        Assert.Empty(analysisResult.Diagnostics);
        var record = Assert.Single(analysisResult.Records);
        Assert.Equal("Complete Game", record.GameName);
    }
}
