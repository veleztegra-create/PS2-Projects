using PS2Manager.Core.Abstractions;

namespace PS2Manager.Core.Tests;

/// <summary>
/// Filesystem determinista en memoria para probar el analizador sin tocar disco.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files =
        new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string fileName, byte[] contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(contents);
        _files[fileName] = contents.ToArray();
    }

    public IEnumerable<string> EnumerateFileNames() => _files.Keys.ToArray();

    public bool FileExists(string fileName) => _files.ContainsKey(fileName);

    public byte[] ReadAllBytes(string fileName)
    {
        if (!_files.TryGetValue(fileName, out var contents))
            throw new FileNotFoundException("Archivo no encontrado en el filesystem de prueba.", fileName);

        return contents.ToArray();
    }
}
