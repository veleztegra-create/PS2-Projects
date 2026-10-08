# OPL Compatibility Matrix

## Purpose

This document defines the compatibility rules PS2-Manager should use when analyzing and installing games for Open PS2 Loader (OPL).

The key principle is:

> PS2-Manager should distinguish what the Windows filesystem can store from what the target OPL configuration can actually consume.

OPL behavior changes over time, so this document records verified current behavior and identifies rules that must remain version-aware.

## USB filesystem support

Current OPL documentation states that USB/MX4SIO/iLink support includes:

- FAT32
- exFAT
- MBR/GPT partition tables in current OPL documentation
- partial file fragmentation support up to 64 fragments in OPL v1.2.0 beta rev1893 and later

FAT32 remains important because it is widely used by legacy OPL setups.

For FAT32:

- a single file cannot exceed the filesystem's approximately 4 GiB limit;
- therefore an ISO larger than 4 GiB cannot be stored as one plain ISO file;
- USBExtreme/UL is the compatibility path for games that exceed that limit.

For exFAT:

- current OPL documentation lists exFAT as supported for USB-class devices;
- a large ISO does not need to be split merely because of the FAT32 4 GiB limit;
- PS2-Manager must still consider OPL version and target-device compatibility before treating exFAT as universally safe.

### Important implementation rule

The installer must not make a decision from the filename extension alone.

It should evaluate:

1. target filesystem;
2. target partition/layout;
3. target OPL compatibility profile/version, when known;
4. source image type;
5. source image size;
6. CD/DVD media type;
7. whether the chosen storage representation is discoverable by OPL.

## USB game representations

For the USB path, current OPL documentation identifies:

- plain ISO;
- USBExtreme/UL.

Current OPL source also has explicit internal game-format representations for USBExtreme/UL and ISO games.

Both representations may coexist on the same USB device.

### Plain ISO

For USB, the established directory convention is:

- `DVD/` for DVD ISO games;
- `CD/` for CD ISO games.

The ISO must be compatible with OPL's expected disc-image format.

For CD images, OPL documentation specifically calls out ISO9660 with 2048-byte sectors. A BIN/CUE source therefore cannot be blindly renamed to `.iso`; its track/sector layout must be inspected and, when necessary, converted.

### ISO filename convention

OPL supports an optional Game-ID-prefixed ISO naming convention:

`<GAME_ID>.<Game Name>.iso`

This can speed up game-list discovery.

PS2-Manager should treat this as a compatibility/optimization convention rather than assuming every valid ISO must use it.

Filename length and special-character restrictions should be handled conservatively because the OPL documentation recommends short filenames and avoiding special characters.

### UL / USBExtreme

UL games are represented by:

- `ul.cfg`
- one or more `ul.<CRC>.<GAME_ID>.<NN>` files

The UL files and `ul.cfg` are associated through the OPL-specific naming and metadata rules documented in `docs/ul-format.md`.

UL games are not placed in `CD/` or `DVD/` as normal ISO files. The analyzer should therefore keep ISO discovery and UL discovery as separate pipelines.

## Discovery model for PS2-Manager

The analyzer should conceptually scan a USB target in this order:

1. Detect filesystem and partition information.
2. Inspect root-level OPL-relevant structures.
3. Inspect `CD/` for ISO candidates.
4. Inspect `DVD/` for ISO candidates.
5. Inspect root-level UL artifacts and `ul.cfg`.
6. Correlate each UL record with its expected part files.
7. Inspect optional OPL-related data separately.
8. Produce a unified game library without pretending the underlying formats are identical.

This allows a device to contain both ISO and UL games.

## ISO versus UL classification

A game should have a format classification independent from its filename extension:

- `Iso`
- `Ul`
- later `Zso` if explicitly supported by the selected target profile
- `Unknown` / `Unsupported`

For an ISO candidate, the analyzer should determine:

- path (`CD/` or `DVD/`);
- file size;
- disc/media classification;
- Game ID when it can be extracted;
- whether the filename follows the optional ID convention;
- whether the file exceeds the target filesystem's single-file limit;
- whether the file appears structurally readable.

For a UL candidate, the analyzer should determine:

- Game ID;
- game name;
- media;
- expected part count;
- actual part numbers;
- missing parts;
- orphan parts;
- CRC/name consistency;
- total reconstructed size where possible.

## Fragmentation

Fragmentation is a first-class health signal, but PS2-Manager should not expose a generic "Defragment" action in the first releases.

Current OPL documentation states that game files should ideally be defragmented and that newer OPL revisions support partial file fragmentation up to 64 fragments.

For the analyzer, the useful distinction is:

- contiguous / healthy;
- fragmented but within the target OPL capability;
- fragmented beyond the target capability;
- fragmentation status unavailable.

The exact implementation of physical-fragment inspection belongs to the filesystem/IO layer and should not be faked at the application layer.

## Smart Install decision matrix

### FAT32 target

| Source | Size | Preferred representation |
|---|---:|---|
| DVD ISO | <= 4 GiB | Plain ISO in `DVD/` |
| DVD ISO | > 4 GiB | UL |
| CD ISO | <= 4 GiB | Plain ISO in `CD/` |
| CD ISO | > 4 GiB | UL, subject to source/media validation |
| BIN/CUE | varies | Inspect first; convert only when compatible |

The 4 GiB boundary is a filesystem constraint, not an OPL game-size rule.

### exFAT target

| Source | Size | Preferred representation |
|---|---:|---|
| DVD ISO | <= 4 GiB | Plain ISO |
| DVD ISO | > 4 GiB | Plain ISO when target OPL profile supports it |
| CD ISO | <= 4 GiB | Plain ISO |
| CD ISO | > 4 GiB | Plain ISO when target OPL profile supports it |
| BIN/CUE | varies | Inspect/convert as required |

PS2-Manager should not automatically choose UL on exFAT simply because the ISO is larger than 4 GiB.

## OPL version profiles

Compatibility should eventually be represented as a profile rather than hard-coded assumptions.

Example conceptual model:

- `LegacyFat32`
- `ModernFat32`
- `ModernExFat`
- `Unknown`

A future implementation can attach verified OPL-version/revision capabilities to these profiles.

This is important because current OPL documentation differs from older USB-mode guidance: older documentation described FAT32/FAT16 only, while current OPL documentation lists exFAT and newer fragmentation behavior.

## Analyzer severity implications

Suggested statuses:

- **Healthy** — representation and metadata are internally consistent.
- **Warning** — usable but non-optimal or dependent on a compatibility assumption.
- **Error** — the game is incomplete, structurally invalid, or cannot be represented safely on the selected target.
- **Unknown** — insufficient evidence to determine compatibility.
- **Orphan** — file exists without a valid corresponding library entry.
- **Missing** — metadata expects a file/part that is absent.

The analyzer should never report a game as healthy merely because the expected filename exists.

## BIN/CUE boundary

BIN/CUE remains a separate research task.

The OPL documentation specifically notes that CD BIN/CUE images may use sector sizes that are not directly usable as OPL ISO images. Therefore PS2-Manager should inspect the CUE and the associated BIN tracks before installation.

At minimum, the future parser should identify:

- track count;
- track modes;
- sector size;
- data versus audio tracks;
- referenced BIN files;
- whether the layout can safely be converted to an OPL-compatible ISO.

No automatic conversion should occur until the source is classified as supported.

## Sources

Primary compatibility reference:

- Open PS2 Loader current README.
- Open PS2 Loader USB-mode documentation.
- Open PS2 Loader PC tools/source for UL and metadata behavior.

These sources should be rechecked when implementing target-version-specific compatibility profiles.
