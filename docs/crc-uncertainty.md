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

## Muestras y candidatos reportados

| Nombre candidato | ID reportado en archivos | CRC reportado/calculado | Estado |
|---|---|---|---|
| `Harry Potter to Kenja no Ishi` | `SLPM_654.65` | `E8C54EAD` | Coincidencia confirmada con registro real |
| `Curious George` | `SLUS_213.54` | `24DE05BF` | Coincidencia reportada; registro binario no incluido aquí |
| `SLUS_623.90.Super Mario 64 ESP` | `SLUS_623.90` | `32D7DD31` | Coincidencia confirmada con registro real |
| `Piglet el Gran Juego` | `SLES_516.66` | `270B457C` | CRC coincide con el nombre candidato |
| `Stitch Experiment 626` | `SCES_509.59` | `02CAA445` | CRC coincide con el nombre candidato |
| `Disney Bolt` (candidato) | `SLES_554.29` | `B8913F43` | ID/título aclarados por la lista de colección; nombre exacto de `ul.cfg` aún no comprobado |
| `Disney Princess: Enchanted Journey` (título comercial) | `SLES_548.37` | `485706CC` | Identidad del juego confirmada; el CRC no coincide con el título limpio y falta el nombre exacto de `ul.cfg` |

`Harry Potter to Kenja no Ishi` está respaldado por un registro real de 64 bytes compartido por el usuario, con identificador de imagen `ul.SLPM_654.65`; la lista aportada incluye archivos `ul.E8C54EAD.SLPM_654.65.xx`.

Para Super Mario 64 ESP también se recibió el registro completo de 64 bytes. El campo `name[32]` contiene `SLUS_623.90.Super Mario 64 ESP` seguido de NUL. Al calcular el CRC sobre esa cadena completa —incluidos el ID y el punto—, el resultado es `32D7DD31`, exactamente el prefijo de los archivos `ul.32D7DD31.SLUS_623.90.00`.

**Conclusión para Super Mario 64 ESP:** el algoritmo no estaba fallando. La prueba anterior calculaba el CRC de `Super Mario 64 ESP` sin el prefijo `SLUS_623.90.`; esa no es la cadena almacenada en este registro. No se debe eliminar el ID ni el punto antes de calcular el CRC.

## Corrección importante en la asociación de títulos

La lista de archivos proporcionada contiene distintos ID de juego que se habían asociado inicialmente a títulos incorrectos o mezclados:

- `SLES_516.66` aparece en catálogos públicos como **Disney's Piglet el Gran Juego**. El CRC de `Piglet el Gran Juego` es `270B457C`, coincidiendo con el prefijo reportado `ul.270B457C.SLES_516.66.xx`.
- `SCES_509.59` aparece en catálogos públicos como **Disney's Stitch: Experiment 626**. La cadena candidata `Stitch Experiment 626` produce `02CAA445`, coincidiendo con `ul.02CAA445.SCES_509.59.xx`.
- `SLES_554.29` corresponde a **Disney Bolt**. En la lista de archivos de la colección, el prefijo asociado es `B8913F43`; el título limpio `Disney Bolt` produce ese CRC. Es una coincidencia consistente, pero sin el registro binario no demuestra por sí sola que esa sea exactamente la cadena almacenada en `name[32]`.
- `SLES_548.37` corresponde a **Disney Princess: Enchanted Journey**, no a Disney Bolt. El prefijo observado es `485706CC`, que no coincide con el título limpio `Disney Princess - Enchanted Journey` en el cálculo actual. La identidad del juego ya está aclarada; queda pendiente descubrir la cadena exacta almacenada en `ul.cfg`, que podría incluir un prefijo, variante regional o texto personalizado.

Las dos primeras coincidencias son buenas candidatas, pero sin los bytes originales de esos registros no podemos asegurar que sean las cadenas exactas almacenadas en `name[32]`. La tercera discrepancia sigue abierta.

## Regla para el parser y el analizador

- Conservar la cadena original de `name[32]` para calcular el CRC, incluyendo cualquier ID prefijado que esté realmente almacenado.
- No reconstruir el nombre a partir del ID del archivo, ni eliminar automáticamente un prefijo antes del CRC.
- Si la interfaz necesita mostrar solo el título comercial, extraerlo en una propiedad de presentación separada; nunca sobrescribir el valor original usado para validar.
- Mantener la lectura limitada al primer byte NUL, como corresponde a una cadena C. Los bytes de relleno posteriores al NUL no forman parte del nombre pasado a `crc32()`.

## Pruebas automáticas

- `OplCrc32Tests.ComputeGameName_MatchesRealOplUlCfgAndPartFilename`: Harry Potter.
- `OplCrc32Tests.ComputeGameName_MatchesSecondReportedRealSample`: Curious George.
- `OplCrc32Tests.ComputeGameName_MatchesPrefixedTitleStoredInUlCfg`: Super Mario 64 ESP con el ID concatenado.
- `OplCrc32Tests.ComputeGameName_MatchesReportedPigletTitleCandidate`: candidato Piglet.
- `OplCrc32Tests.ComputeGameName_MatchesReportedStitchTitleCandidate`: candidato Stitch.
- Vectores derivados de la rutina C: `Fixture UL Game`, `X` y cadena vacía.

## Siguiente dato necesario

Para cerrar la discrepancia de `SLES_548.37`, comparte únicamente los primeros 32 bytes de su registro en `ul.cfg` (campo `name[32]`) y, si puedes, los 15 bytes siguientes del campo de imagen. No hace falta compartir archivos del juego.
