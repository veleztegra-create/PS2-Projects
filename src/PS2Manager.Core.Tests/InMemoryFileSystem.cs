using PS2Manager.Core.Abstractions;

namespace PS2Manager.Core.Tests;

internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files =
        new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string fileName, byte[] content) =>
        _files[fileName] = content;

    public IEnumerable<string> EnumerateFileNames() => _files.Keys.ToList();

    public bool FileExists(string fileName) => _files.ContainsKey(fileName);

    public byte[] ReadAllBytes(string fileName) => _files[fileName];
}
