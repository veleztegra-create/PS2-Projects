# CRC32 de OPL — estado de verificación

## Estado actual

Se inspeccionó la implementación `crc32(const char *string)` de estos dos archivos del código fuente de Open PS2 Loader:

- `pc/iso2opl/src/iso2opl.c`
- `pc/opl2iso/src/opl2iso.c`

Ambos contienen el mismo algoritmo. `src/PS2Manager.IO/OplCrc32.cs` reproduce sus operaciones, en lugar de asumir el CRC-32 convencional.

Referencia del código fuente: [iso2opl.c de Open PS2 Loader](https://github.com/ps2homebrew/Open-PS2-Loader/blob/master/pc/iso2opl/src/iso2opl.c) y [opl2iso.c](https://github.com/ps2homebrew/Open-PS2-Loader/blob/master/pc/opl2iso/src/opl2iso.c).

## Detalles importantes del algoritmo de OPL

1. Polinomio: `0x04C11DB7`.
2. La tabla se construye con la condición de signo de un `int` de 32 bits, como aparece en el código C original.
3. La tabla se guarda en orden inverso: `crctab[255 - table]`.
4. Al terminar de construir la tabla, el código original reutiliza el valor que queda en `crc` como estado inicial; en la implementación examinada, ese valor es cero.
5. El índice de tabla es `byte ^ ((crc >> 24) & 0xFF)`.
6. El bucle es `do/while`, por lo que procesa también el byte NUL que termina el nombre.
7. No hay XOR final en la función fuente.

Como los desplazamientos de enteros con signo que se desbordan no son portables según el estándar C, C# reproduce explícitamente el comportamiento habitual de enteros de 32 bits en las plataformas para las que se escribió esta herramienta. Los nombres con bytes no ASCII requieren comprobación adicional.

## Vectores derivados del código fuente

Los vectores siguientes se verificaron con un pequeño harness C que reproduce la rutina fuente:

| Nombre | CRC calculado |
|---|---|
| `Fixture UL Game` | `41552A45` |
| `X` | `DB5FB40D` |
| cadena vacía | `00000000` |

Estos vectores comprueban el port C# contra la rutina derivada del código fuente, no contra una biblioteca creada por una instalación física de OPL.

## Muestras reales reportadas

| Nombre exacto reportado en `ul.cfg` | Prefijo de archivo reportado | CRC calculado por PS2-Manager | Estado |
|---|---|---|---|
| `Harry Potter to Kenja no Ishi` | `E8C54EAD` | `E8C54EAD` | Coincide |
| `Curious George` | `24DE05BF` | `24DE05BF` | Coincide |
| `Super Mario 64 ESP` | `32D7DD31` | `31745170` | No coincide |
| `Piglet el Gran Juego` | `02CAA445` | `270B457C` | No coincide |
| `Disney Bolt` | `485706CC` | `B8913F43` | No coincide |

Para Harry Potter se recibió un archivo `ul.cfg` de 64 bytes que contiene el título `Harry Potter to Kenja no Ishi` y el identificador `SLPM_654.65`; la lista aportada incluye `ul.E8C54EAD.SLPM_654.65.00`, `.01` y `.02`.

Para las otras muestras, los títulos y prefijos se basan en la tabla y los nombres de archivo compartidos por el usuario; todavía no se ha inspeccionado el registro binario correspondiente a cada uno. Por eso las coincidencias de Curious George y Harry Potter son evidencia útil, pero no sustituyen la comprobación byte a byte de todos los registros.

**No se debe ajustar el algoritmo para forzar que Super Mario 64 ESP produzca `32D7DD31`.** Primero hay que confirmar el campo `name[32]` real del registro de `SLUS_623.90`, incluyendo espacios, terminación NUL y bytes exactos. El prefijo del archivo por sí solo no prueba qué cadena produjo el CRC.

La función de OPL calcula el CRC sobre el nombre entregado a `crc32(game_name)`; el identificador del juego y el número de parte no forman parte de esa cadena.

## Pruebas automáticas

- `OplCrc32Tests.ComputeGameName_MatchesRealOplUlCfgAndPartFilename`: Harry Potter.
- `OplCrc32Tests.ComputeGameName_MatchesSecondReportedRealSample`: Curious George.
- Vectores derivados de la rutina C: `Fixture UL Game`, `X` y cadena vacía.

## Próximo paso para resolver las discrepancias

Necesitamos el registro exacto de `ul.cfg` para `SLUS_623.90`. Cada registro mide 64 bytes y el campo de nombre ocupa los primeros 32 bytes. Se puede compartir una extracción hexadecimal de esos 32 bytes y los 15 bytes del campo de imagen; no hace falta subir ningún archivo de juego. Así podremos confirmar si el nombre almacenado es realmente `Super Mario 64 ESP` y descartar espacios finales, diferencias de codificación o una asociación incorrecta entre título y archivo.
