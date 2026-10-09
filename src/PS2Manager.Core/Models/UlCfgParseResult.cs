namespace PS2Manager.Core.Models;

public sealed record UlCfgParseResult(
    IReadOnlyList<UlCfgRecord> Records,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public static UlCfgParseResult Empty { get; } = new(
        Array.Empty<UlCfgRecord>(),
        Array.Empty<Diagnostic>());
}
