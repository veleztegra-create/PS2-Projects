namespace PS2Manager.Core.Models;

/// <summary>
/// Resultado inmutable de un análisis UL: juegos interpretados, registros crudos
/// de ul.cfg y diagnósticos globales.
/// </summary>
public sealed record UlAnalysisResult(
    IReadOnlyList<UlGame> Games,
    IReadOnlyList<UlCfgRecord> Records,
    IReadOnlyList<Diagnostic> Diagnostics);
