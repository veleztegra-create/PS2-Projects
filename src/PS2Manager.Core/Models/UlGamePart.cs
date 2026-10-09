namespace PS2Manager.Core.Models;

public sealed record UlGamePart(
    int PartNumber,
    string CrcHex,
    string FileName);
