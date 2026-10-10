using PS2Manager.Core.Models;
using PS2Manager.IO;

namespace PS2Manager.Core.Tests;

public class UlCfgWriterTests
{
    [Fact]
    public void RoundTrip_SerializeAndParse_PreservesAllRecordData()
    {
        // 1. Arrange: Definimos una lista de registros originales con casos variados
        var originalRecords = new List<UlCfgRecord>
        {
            new(0, "Super Mario 64 ESP", "ul.SLUS_623.90", 1, UlMediaType.Dvd, 0x14),
            new(1, "Curious George", "ul.SLUS_213.54", 2, UlMediaType.Cd, 0x12),
            new(2, "Custom Test Game", "ul.SCES_509.59", 3, UlMediaType.Unknown, 0x99)
        };

        // 2. Act: Serializamos los registros a bytes usando UlCfgWriter
        byte[] serializedBytes = UlCfgWriter.Serialize(originalRecords);

        // 3. Act: Volvemos a parsear esos bytes usando tu UlCfgParser existente
        var parseResult = UlCfgParser.Parse(serializedBytes);

        // 4. Assert: Verificamos que no se hayan generado errores de análisis
        Assert.Empty(parseResult.Diagnostics);
        Assert.Equal(originalRecords.Count, parseResult.Records.Count);

        // 5. Assert: Comprobamos campo por campo la igualdad exacta
        for (int i = 0; i < originalRecords.Count; i++)
        {
            var original = originalRecords[i];
            var parsed = parseResult.Records[i];

            Assert.Equal(original.Index, parsed.Index);
            Assert.Equal(original.GameName, parsed.GameName);
            Assert.Equal(original.ImageIdentifier, parsed.ImageIdentifier);
            Assert.Equal(original.DeclaredPartCount, parsed.DeclaredPartCount);
            Assert.Equal(original.Media, parsed.Media);
            Assert.Equal(original.RawMediaByte, parsed.RawMediaByte);
        }
    }

    [Fact]
    public void Serialize_EmptyList_ReturnsEmptyByteArray()
    {
        // Comprobación de casos límite: una lista vacía debe serializarse limpiamente sin excepciones
        byte[] bytes = UlCfgWriter.Serialize(Enumerable.Empty<UlCfgRecord>());
        
        Assert.NotNull(bytes);
        Assert.Empty(bytes);
    }
}
