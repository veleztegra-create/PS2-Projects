namespace PS2Manager.Core.Models;

/// <summary>
/// Valores de media byte documentados en docs/ul-format.md.
/// 0x12 = CD, 0x14 = DVD. Cualquier otro valor es Unknown — nunca se reinterpreta.
/// </summary>
public enum UlMediaType : byte
{
    Unknown = 0x00,
    Cd = 0x12,
    Dvd = 0x14,
}

public static class UlMediaTypeExtensions
{
    public static UlMediaType FromByte(byte value) => value switch
    {
        0x12 => UlMediaType.Cd,
        0x14 => UlMediaType.Dvd,
        _ => UlMediaType.Unknown,
    };
}
