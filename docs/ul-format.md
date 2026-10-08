# UL / ul.cfg Technical Notes

## Sources

Primary implementation reference: Open PS2 Loader PC tools and current source.

- `pc/iso2opl/src/iso2opl.c`
- `pc/opl2iso/src/opl2iso.c`
- `include/supportbase.h`

These notes record observed implementation behavior; they are not a claim that every historical USBExtreme implementation is identical.

## ul.cfg

OPL's PC tooling treats each record as **64 bytes**.

The current OPL PC code reads records as:

| Offset | Size | Meaning |
|---|---:|---|
| 0x00 | 32 | Game name |
| 0x20 | 15 | Image identifier, normally `ul.<GAME_ID>` |
| 0x2F | 1 | Part count |
| 0x30 | 1 | Media |
| 0x31–0x3F | 15 | Padding/reserved |

The writer zeroes the complete structure before filling fields and sets `pad[4] = 0x08`. Because the padding begins at 0x31, this places the observed magic byte at **0x35**.

This resolves an apparent discrepancy in secondary documentation that lists the magic byte without showing the complete reserved range.

### Media values

OPL's PC tooling writes:

- `0x12` = CD
- `0x14` = DVD

The export tool also rejects unknown media IDs.

### Important parser rule

The analyzer must not assume that a non-64-byte `ul.cfg` is safe to parse as complete records.

A file whose size is not divisible by 64 should be reported as malformed/truncated metadata.

## UL part naming

OPL's PC tooling generates names in this form:

`ul.<CRC32>.<GAME_ID>.<NN>`

where:

- CRC32 is derived from the **game name**;
- GAME_ID is the startup/game identifier;
- NN is a zero-based, two-digit part number.

Example shape:

`ul.A1B2C3D4.SLUS_123.45.00`

The exact game ID normalization used by a particular tool must be retained when implementing compatibility logic.

## Part size

The OPL PC ISO-to-UL implementation writes a fixed-size chunk buffer and generates sequential part files.

The commonly documented target is **1 GiB per part**. This should be confirmed from the current constant in the OPL source before the first implementation of the splitter, rather than hard-coding the value from third-party documentation alone.

## CRC32

OPL does not use the common reflected CRC32 implementation normally found in application libraries.

The PC tooling:

1. builds a 256-entry table using polynomial `0x04C11DB7`;
2. stores entries using the reversed table index `255 - table`;
3. processes the null-terminated game-name string;
4. produces a 32-bit value formatted as eight uppercase hexadecimal characters.

Therefore PS2-Manager should implement an explicit `OplCrc32` algorithm and test it against known OPL-generated filenames.

Do **not** substitute .NET's generic CRC32 implementation without verifying byte-for-byte compatibility.

## Analyzer implications

A future read-only analyzer can establish:

1. Is `ul.cfg` present?
2. Is its length divisible by 64?
3. How many records exist?
4. Is each record's image field plausible?
5. Does the recorded part count match the actual `ul.*` files?
6. Are expected part numbers present with no gaps?
7. Do the filenames' CRC/Game ID components agree with the record?
8. Are media values recognized?
9. Are there orphan UL files with no metadata record?

## Next verification

Before implementing the parser/splitter, confirm the exact current OPL chunk-size constant and inspect how the runtime discovers USB UL files. Also document the distinction between the legacy USBExtreme/UL representation and OPL's newer direct ISO support.
