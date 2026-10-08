# Analyzer Test Fixtures

## Purpose

These fixtures define the minimum synthetic USB/library scenarios that PS2-Manager must analyze deterministically before real-device testing.

They are intentionally small and do not need to be bootable PS2 games. Their purpose is to exercise filesystem discovery, UL metadata parsing, cross-checks, ISO discovery and error reporting.

## Fixture policy

- Deterministic and repeatable.
- No copyrighted game images required.
- Binary metadata generated from documented OPL rules.
- Tests assert both detected condition and severity.
- Each fixture isolates one principal fault where practical.
- Real USB devices remain integration-test material; synthetic fixtures are the unit-test baseline.

Current OPL source confirms the UL writer uses 64-byte records, the OPL-specific CRC algorithm, 1 GiB UL parts, and media values 0x12 for CD and 0x14 for DVD. See the current OPL iso2opl source.

## Fixture layout

tests/fixtures/
├── usb-empty/
├── usb-valid-iso/
├── usb-valid-ul/
├── usb-mixed/
├── usb-ul-missing-part/
├── usb-ul-orphan-part/
├── usb-ul-bad-crc/
├── usb-ul-bad-cfg-length/
├── usb-ul-invalid-media/
├── usb-ul-part-count-mismatch/
├── usb-iso-wrong-location/
├── usb-iso-too-large-fat32/
└── usb-unknown-content/

## Fixture 01 — Empty USB

Structure: empty root.

Expected: zero games, no OPL library invented, and a clean baseline result.

## Fixture 02 — Valid ISO library

Structure: DVD/SLUS_000.00.Example Game.iso

Expected:
- one ISO game;
- media DVD;
- Game ID SLUS_000.00;
- no UL records;
- no missing parts;
- no orphan UL files;
- severity Healthy.

The ISO may be a synthetic minimal ISO containing a valid SYSTEM.CNF. The analyzer must not require a complete retail game image.

## Fixture 03 — Valid UL library

Structure:
ul.cfg
ul.<CRC>.<GAME_ID>.00
ul.<CRC>.<GAME_ID>.01

Metadata:
- Game name: Fixture UL Game
- Game ID: SLUS_000.00
- Media: DVD (0x14)
- Parts: 2

Expected:
- one UL game;
- parts 00 and 01 found;
- CRC matches OPL algorithm;
- Game ID matches;
- recorded part count matches actual parts;
- severity Healthy.

## Fixture 04 — Mixed ISO + UL

Structure:
CD/SCUS_000.01.Fixture CD.iso
DVD/SLUS_000.00.Fixture DVD.iso
ul.cfg
ul.<CRC>.<GAME_ID>.00

Expected: three games total, ISO and UL remain separately classified, and no false duplicate is created solely because formats differ.

## Fixture 05 — Missing UL part

ul.cfg declares parts = 3, but only parts .00 and .01 exist.

Expected: missing part .02, severity Error, and the game must not be reported Healthy.

## Fixture 06 — Orphan UL part

ul.cfg has one valid record, while a second UL part belongs to another CRC/Game ID with no corresponding record.

Expected: one valid UL game plus one orphan artifact. The orphan must not be silently attached to another game. Severity at least Warning.

## Fixture 07 — Bad CRC/name relationship

The UL filename contains a CRC that does not match the recorded game name.

Expected: UL record is readable, Game ID is parseable, CRC/name validation fails, and the diagnostic identifies the mismatch.

## Fixture 08 — Invalid ul.cfg length

ul.cfg is 65 bytes: one complete 64-byte record plus one extra byte.

Expected: non-integral record length detected, no out-of-bounds read, severity Error, and exact remainder reported.

## Fixture 09 — Invalid media byte

A valid 64-byte record contains media byte 0x99.

Expected: record parses safely, media is Unknown, and the analyzer does not reinterpret the value as CD or DVD.

## Fixture 10 — Part-count mismatch

ul.cfg declares two parts, but .00, .01 and .02 exist.

Expected: extra part reported and no silent truncation.

## Fixture 11 — ISO in wrong location

Structure: PS2/SLUS_000.00.Example.iso

Expected: distinguish a valid ISO located outside an OPL USB game location from a corrupt image. Suggested result: UnsupportedLocation or Warning, and do not count it as a normal OPL USB game.

## Fixture 12 — ISO too large for FAT32

Structure: DVD/SLUS_000.00.Large Example.iso

The fixture does not need to allocate a real 4+ GiB file. The filesystem layer should support a synthetic file-size provider or sparse test file.

Expected on FAT32 profile: ISO detected, single-file limit exceeded, installation planning severity Error, Smart Install recommendation UL.

Expected on exFAT profile: size alone does not trigger the FAT32 error; plain ISO may remain the recommendation when the selected OPL profile supports it.

Current OPL documentation lists ISO, ZSO and USBExtreme/UL formats and documents CD and DVD USB/SMB directories. It also states that DVD5/DVD9 images can be used when the filesystem supports files above 4 GiB.

## Fixture 13 — Unknown content

Structure: README.TXT, random.bin and an unrelated folder.

Expected: zero games, no crash, and optional reporting of ignored/unrecognized content.

## Future fixtures

Add after the first analyzer implementation:
- truncated UL part;
- duplicate Game ID;
- duplicate game name;
- wrong UL part-number sequence;
- filename Game ID differs from ul.cfg Game ID;
- invalid ISO PVD;
- ISO missing SYSTEM.CNF;
- malformed SYSTEM.CNF;
- BIN/CUE with data and audio tracks;
- BIN/CUE with unsupported sector size;
- fragmented file on a real filesystem;
- exFAT library;
- ZSO when explicitly supported;
- corrupted filesystem metadata detected by the filesystem layer.

## Test assertions

Analyzer tests should eventually assert:
1. discovered games;
2. game format;
3. Game ID;
4. game name;
5. media;
6. expected and actual part counts;
7. missing and orphan artifacts;
8. severity;
9. diagnostic code;
10. Smart Install recommendation where applicable.

Diagnostic codes should be stable identifiers rather than tests matching human-readable UI text.

## Proposed diagnostic identifiers

- OPL001 — malformed ul.cfg length
- OPL002 — invalid media value
- OPL003 — missing UL part
- OPL004 — orphan UL part
- OPL005 — UL CRC mismatch
- OPL006 — UL part-count mismatch
- OPL007 — ISO outside expected OPL location
- OPL008 — ISO exceeds target filesystem single-file limit
- OPL009 — unsupported or unknown game representation
- OPL010 — duplicate Game ID

These identifiers are proposals until the analyzer core defines the final diagnostic taxonomy.