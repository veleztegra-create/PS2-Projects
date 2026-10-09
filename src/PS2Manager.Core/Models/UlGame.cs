namespace PS2Manager.Core.Models;

public sealed record UlGame(
    string GameName,
    string GameId,
    UlMediaType Media,
    int DeclaredPartCount,
    IReadOnlyList<UlGamePart> Parts,
    Severity Severity,
    IReadOnlyList<Diagnostic> Diagnostics);
