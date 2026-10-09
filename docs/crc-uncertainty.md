# CRC32 de OPL — Incertidumbre abierta

## Estado

La implementación en `src/PS2Manager.IO/OplCrc32.cs` reproduce el algoritmo
**descrito** en `docs/ul-format.md`:

- polynomial `0x04C11DB7`
- procesamiento MSB-first (no reflejado)
- string terminado en nulo

Pero **no se ha verificado byte-a-byte contra el código fuente actual de OPL**
(`pc/iso2opl/src/iso2opl.c`, `include/supportbase.h`). Los intentos de acceso
en esta iteración fallaron.

## Preguntas que deben resolverse antes de considerar el parser verificado

1. **Estado inicial de `crc`** en la llamada a `USBA_crc32` / `crc32`.
   Opciones plausibles: `0` (implementación típica en muchos ports) o
   `0xFFFFFFFF` (convención CRC32 estándar). El código actual asume `0`.
2. **Inclusión del byte nulo**. El doc dice "processes the null-terminated
   game-name string", que puede interpretarse como:
   - procesar los caracteres hasta, pero sin incluir, el nulo (`while (*s)`), o
   - procesar la cadena incluyendo el nulo final.
   El código actual NO incluye el nulo por defecto; `ComputeGameName` expone
   el flag `includeNullTerminator` para invertirlo sin reescribir el algoritmo.
3. **Normalización previa del nombre**. No se sabe si OPL pasa el `GameName`
   tal cual (con espacios finales, mayúsculas/minúsculas originales) o lo
   normaliza. El código actual pasa el string sin transformar.
4. **XOR final**. No se sabe si OPL aplica `crc ^= 0xFFFFFFFF` antes de formatear.
   El código actual NO aplica XOR final.
5. **"Reversed table index `255 - table`"**. El doc describe un storage invertido
   de la tabla. Si el acceso también usa `255 - idx`, el resultado es idéntico
   al de una tabla estándar. Si no, el resultado difiere. El código actual usa
   tabla y acceso estándar, que es matemáticamente equivalente a la interpretación
   simétrica.

## Qué se necesita para cerrar esta incertidumbre

Un único vector real verificado:
