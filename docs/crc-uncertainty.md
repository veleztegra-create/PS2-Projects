# CRC32 de OPL — estado de verificación

## Estado actual

Se inspeccionó la implementación `crc32(const char *string)` de estos dos archivos del código fuente de Open PS2 Loader:

- `pc/iso2opl/src/iso2opl.c`
- `pc/opl2iso/src/opl2iso.c`

Ambos contienen el mismo algoritmo. `src/PS2Manager.IO/OplCrc32.cs` ahora reproduce sus operaciones, en lugar de asumir el CRC-32 convencional.

## Detalles importantes del algoritmo de OPL

1. Polinomio: `0x04C11DB7`.
2. La tabla se construye con la condición de signo de un `int` de 32 bits, como aparece en el código C original. No es la rutina MSB-first convencional.
3. La tabla se guarda en orden inverso: `crctab[255 - table]`.
4. Al terminar de construir la tabla, el código original reutiliza el valor que queda en `crc` como estado inicial. No lo reinicia explícitamente; en la implementación examinada, ese valor es cero.
5. El índice de tabla es `byte ^ ((crc >> 24) & 0xFF)`; no se aplica una inversión adicional al índice.
6. El bucle es `do/while`, por lo que procesa también el byte NUL que termina el nombre.
7. No hay XOR final en la función fuente.

Como los desplazamientos de enteros con signo que se desbordan no son portables según el estándar C, C# reproduce explícitamente el comportamiento habitual de enteros de 32 bits en las plataformas para las que se escribió esta herramienta. Los nombres de juego con bytes no ASCII requieren comprobación adicional contra una biblioteca real.

## Vectores de regresión

Los vectores de `OplCrc32Tests` se verificaron ejecutando un pequeño harness C compilado con GCC que reproduce la rutina fuente. Los resultados de referencia son:

| Nombre | CRC |
|---|---|
| `Fixture UL Game` | `41552A45` |
| `X` | `DB5FB40D` |
| cadena vacía | `00000000` |

Estos vectores detectan cambios accidentales en el port C#, pero **no sustituyen una comparación con un `ul.cfg` o un archivo `ul.*` real generado por OPL**.

## Qué falta para cerrar la verificación práctica

Comparar al menos un nombre de juego y su CRC contra un archivo `ul.*` generado por OPL/USBExtreme real. Hasta entonces, el código refleja la rutina fuente inspeccionada y los vectores coinciden con el harness C, pero la comprobación con una biblioteca real sigue pendiente.
