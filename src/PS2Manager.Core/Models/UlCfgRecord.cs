namespace PS2Manager.Core.Models;

/// <summary>
/// Un registro de 64 bytes de ul.cfg, ya parseado.
/// No se inventan valores: si un campo viene vacío o desconocido, se refleja tal cual.
/// </summary>
public sealed record UlCfgRecord(
    int Index,
    string GameName,
    string ImageIdentifier,
    byte DeclaredPartCount,
    UlMediaType Media,
    byte RawMediaByte);
