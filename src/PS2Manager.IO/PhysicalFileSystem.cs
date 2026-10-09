using PS2Manager.Core.Abstractions;

namespace PS2Manager.IO;

/// <summary>
/// Implementación física de IFileSystem, estrictamente read-only.
/// No expone ninguna operación de escritura ni de borrado.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    private readonly string _root;

    public PhysicalFileSystem(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root path requerido.", nameof(root));
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Directorio no existe: {root}");

        _root = root;
    }

    public IEnumerable<string> EnumerateFileNames()
    {
        foreach (var path in Directory.EnumerateFiles(_root))
        {
            yield return Path.GetFileName(path);
        }
    }

    public bool FileExists(string fileName) =>
        File.Exists(Path.Combine(_root, fileName));

    public byte[] ReadAllBytes(string fileName) =>
        File.ReadAllBytes(Path.Combine(_root, fileName));
}
