# CRC32 de OPL — estado de verificación

## Estado actual

Se inspeccionó la implementación `crc32(const char *string)` de estos dos archivos del código fuente de Open PS2 Loader:

- `pc/iso2opl/src/iso2opl.c`
- `pc/opl2iso/src/opl2iso.c`

Ambos contienen el mismo algoritmo. `src/PS2Manager.IO/OplCrc32.cs` reproduce sus operaciones, en lugar de asumir el CRC-32 convencional.

## Detalles importantes del algoritmo de OPL

1. Polinomio: `0x04C11DB7`.
2. La tabla se construye con la condición de signo de un `int` de 32 bits, como aparece en el código C original.
3. La tabla se guarda en orden inverso: `crctab[255 - table]`.
4. Al terminar de construir la tabla, el código original reutiliza el valor que queda en `crc` como estado inicial; en la implementación examinada, ese valor es cero.
5. El índice de tabla es `byte ^ ((crc >> 24) & 0xFF)`.
6. El bucle es `do/while`, por lo que procesa también el byte NUL que termina el nombre.
7. No hay XOR final en la función fuente.

Como los desplazamientos de enteros con signo que se desbordan no son portables según el estándar C, C# reproduce explícitamente el comportamiento habitual de enteros de 32 bits en las plataformas para las que se escribió esta herramienta. Los nombres con bytes no ASCII requieren comprobación adicional.

## Vectores de regresión

Los vectores siguientes se verificaron con un pequeño harness C que reproduce la rutina fuente:

| Nombre | CRC |
|---|---|
| `Fixture UL Game` | `41552A45` |
| `X` | `DB5FB40D` |
| cadena vacía | `00000000` |

Estos vectores comprueban el port C# contra la rutina derivada del código fuente.

## Validación con datos reales de OPL

Se recibió un `ul.cfg` real de 64 bytes que contiene un registro con:

- Nombre del juego: `Harry Potter to Kenja no Ishi`
- Identificador de imagen: `SLPM_654.65`

La lista de archivos de la misma biblioteca proporcionada por el usuario incluye `ul.E8C54EAD.SLPM_654.65.00` y `ul.E8C54EAD.SLPM_654.65.01`. La implementación calcula `E8C54EAD` para el nombre del juego con su terminador NUL, coincidiendo con el prefijo CRC de ambos nombres de archivo.

Esto es una **validación real positiva de una muestra**. El prefijo `E8C54EAD` es el CRC; `SLPM_654.65` es el identificador de imagen, y no debe confundirse con el CRC.

La prueba se conserva en `OplCrc32Tests.ComputeGameName_MatchesRealOplUlCfgAndPartFilename` para detectar regresiones futuras.

## Otras muestras identificadas, pendientes de correlación

El usuario compartió además nombres de partes que parecen corresponder a otros juegos:

- CRC `02CAA445`, identificador `SCES_509.59`
- CRC `485706CC`, identificador `SLES_548.37`
- CRC `24DE05BF`, identificador `SLUS_213.54`
- CRC `32D7DD31`, identificador `SLUS_623.90`

Estos nombres confirman que la biblioteca contiene más grupos de archivos, pero **no son validaciones independientes del algoritmo todavía**, porque aún no tenemos los nombres de juego asociados a esos identificadores en sus registros de `ul.cfg`. No debemos inventar esos títulos ni asumir que el nombre visible del juego coincide con el registrado.

## Qué falta

Validar un segundo juego real, correlacionando el título exacto almacenado en `ul.cfg` con el prefijo CRC de su archivo `ul.*`. También queda pendiente comprobar nombres con caracteres no ASCII. No se requiere subir los archivos de juego completos: basta con el nombre exacto del registro y el nombre de archivo de una de sus partes.
