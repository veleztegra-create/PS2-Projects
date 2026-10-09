namespace PS2Manager.Core.Abstractions;

/// <summary>
/// Abstracción mínima de filesystem. La implementación física vive en PS2Manager.IO.
/// El analyzer solo conoce esta interfaz, lo cual permite tests deterministas con
/// un FS en memoria y garantiza que el parser nunca escriba en disco.
/// </summary>
public interface IFileSystem
{
    /// <summary>Nombres de archivo (sin directorio) presentes en la raíz analizada.</summary>
    IEnumerable<string> EnumerateFileNames();

    bool FileExists(string fileName);

    byte[] ReadAllBytes(string fileName);
}
