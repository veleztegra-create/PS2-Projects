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
4. Al terminar de construir la tabla, el código original reutiliza el valor que queda en `crc` como estado inicial. No lo reinicia explícitamente.
5. El índice de tabla es `byte ^ ((crc >> 24) & 0xFF)`; no se aplica una inversión adicional al índice.
6. El bucle es `do/while`, por lo que procesa también el byte NUL que termina el nombre.
7. No hay XOR final en la función fuente.

Como los desplazamientos de enteros con signo que se desbordan no son portables según el estándar C, C# reproduce explícitamente el comportamiento habitual de enteros de 32 bits en las plataformas para las que se escribió esta herramienta. Los nombres de juego con bytes no ASCII requieren comprobación adicional contra una biblioteca real.

## Vectores de regresión

Los tests incluyen vectores de referencia calculados a partir de una traducción independiente de la rutina fuente. Sirven para detectar cambios accidentales en la implementación, pero **no son una sustitución de la comparación con un `ul.cfg` real generado por OPL**. Esa comprobación externa sigue pendiente.

## Qué falta para cerrar la verificación práctica

Comparar al menos un nombre de juego y su CRC contra un archivo `ul.*` generado por OPL/USBExtreme real. Hasta entonces, el código ya refleja la rutina fuente inspeccionada, pero no debe afirmarse que se haya validado con una unidad física.
