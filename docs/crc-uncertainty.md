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

| Cadena almacenada en `name[32]` de `ul.cfg` | Prefijo de archivo reportado | CRC calculado por PS2-Manager | Estado |
|---|---|---|---|
| `Harry Potter to Kenja no Ishi` | `E8C54EAD` | `E8C54EAD` | Coincide |
| `Curious George` | `24DE05BF` | `24DE05BF` | Coincide |
| `SLUS_623.90.Super Mario 64 ESP` | `32D7DD31` | `32D7DD31` | Coincide |
| `Piglet el Gran Juego`* | `02CAA445` | `270B457C`* | Pendiente de registro |
| `Disney Bolt`* | `485706CC` | `B8913F43`* | Pendiente de registro |

`Harry Potter to Kenja no Ishi` está respaldado por un registro real de 64 bytes compartido por el usuario, con identificador de imagen `ul.SLPM_654.65`; la lista aportada incluye archivos `ul.E8C54EAD.SLPM_654.65.xx`.

Para Super Mario 64 ESP también se recibió el registro completo de 64 bytes. El campo `name[32]` comienza con los bytes ASCII de `SLUS_623.90.Super Mario 64 ESP` y termina con NUL. El campo no contiene solamente el título comercial: el ID, el punto y el título están concatenados. Al calcular el CRC sobre la cadena completa almacenada —`SLUS_623.90.Super Mario 64 ESP`, seguida del NUL—, el resultado es `32D7DD31`, exactamente el prefijo de los archivos `ul.32D7DD31.SLUS_623.90.00`.

**Conclusión para Super Mario 64 ESP:** el algoritmo no estaba fallando. La prueba anterior calculaba el CRC de `Super Mario 64 ESP` sin el prefijo `SLUS_623.90.`; esa no es la cadena almacenada en este registro. No se debe eliminar el ID ni el punto antes de calcular el CRC.

Las muestras de Piglet y Disney Bolt siguen pendientes porque todavía no tenemos sus campos `name[32]` en hexadecimal. Los valores calculados marcados con asterisco corresponden a los títulos limpios reportados, no a registros binarios confirmados. No hay evidencia suficiente para atribuir esas discrepancias al algoritmo.

## Regla para el parser y el analizador

- Conservar la cadena original de `name[32]` para calcular el CRC, incluyendo cualquier ID prefijado que esté realmente almacenado.
- No reconstruir el nombre a partir del ID del archivo, ni eliminar automáticamente un prefijo antes del CRC.
- Si la interfaz necesita mostrar solo el título comercial, extraerlo en una propiedad de presentación separada; nunca sobrescribir el valor original usado para validar.
- Mantener la lectura limitada al primer byte NUL, como corresponde a una cadena C. Los bytes de relleno posteriores al NUL no forman parte del nombre pasado a `crc32()`.

## Pruebas automáticas

- `OplCrc32Tests.ComputeGameName_MatchesRealOplUlCfgAndPartFilename`: Harry Potter.
- `OplCrc32Tests.ComputeGameName_MatchesSecondReportedRealSample`: Curious George.
- `OplCrc32Tests.ComputeGameName_MatchesPrefixedTitleStoredInUlCfg`: Super Mario 64 ESP con el ID concatenado.
- Vectores derivados de la rutina C: `Fixture UL Game`, `X` y cadena vacía.

## Próximo paso para resolver las discrepancias restantes

Para confirmar Piglet y Disney Bolt, necesitamos únicamente los primeros 32 bytes del registro de cada juego en `ul.cfg` y, si es posible, los 15 bytes del campo de imagen. No hace falta compartir los archivos de juego completos.
