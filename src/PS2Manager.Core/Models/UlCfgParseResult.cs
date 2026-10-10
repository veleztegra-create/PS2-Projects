namespace PS2Manager.Core.Models;

public sealed record UlCfgParseResult(
    IReadOnlyList<UlCfgRecord> Records,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Diagnósticos de metadata agrupados por índice de registro.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<Diagnostic>> RecordDiagnostics { get; init; }
        = new Dictionary<int, IReadOnlyList<Diagnostic>>();

    public static UlCfgParseResult Empty { get; } = new(
        Array.Empty<UlCfgRecord>(),
        Array.Empty<Diagnostic>());
}
