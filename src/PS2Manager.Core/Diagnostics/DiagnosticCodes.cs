namespace PS2Manager.Core.Diagnostics;

/// <summary>
/// Identificadores estables de diagnóstico. Los tests deben comparar contra estas
/// constantes, nunca contra texto humano.
/// </summary>
public static class DiagnosticCodes
{
    public const string Opl001 = "OPL001"; // ul.cfg malformado / longitud no divisible por 64 / vacío
    public const string Opl002 = "OPL002"; // media UL desconocida
    public const string Opl003 = "OPL003"; // parte UL faltante
    public const string Opl004 = "OPL004"; // parte UL huérfana
    public const string Opl005 = "OPL005"; // CRC en filename != CRC esperado
    public const string Opl006 = "OPL006"; // part-count mismatch / parte sobrante / parte duplicada
    public const string Opl007 = "OPL007"; // metadata inconsistente: GameName/ImageIdentifier vacío, nombre no parseable
}
