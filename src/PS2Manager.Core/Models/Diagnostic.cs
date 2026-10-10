namespace PS2Manager.Core.Models;

/// <summary>
/// Diagnóstico estable producido al analizar una biblioteca UL.
/// El código identifica la regla; la severidad y el mensaje describen el hallazgo.
/// </summary>
public sealed record Diagnostic(
    string Code,
    Severity Severity,
    string Message);
