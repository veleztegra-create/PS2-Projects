# PS2 Formats — Research Specification

This document records the formats PS2-Manager must understand. It is intentionally a living specification.

## ISO

Primary disc-image input.

Required future inspection:

- file size;
- ISO9660 structures;
- `SYSTEM.CNF`;
- Game ID;
- CD/DVD characteristics;
- validity/readability.

## BIN/CUE

Planned input format.

A BIN file must not be assumed to be a standalone ISO. The CUE file describes the track layout and may identify multiple tracks.

Before implementation, research must establish the PS2 variants that matter, including:

- single-track data images;
- multi-track images;
- audio/data track combinations;
- CD-based games;
- relationships between BIN and CUE filenames;
- Game ID extraction from supported layouts.

The installer must reject or clearly flag unsupported layouts rather than silently producing an invalid game.

## UL / USBExtreme

Legacy game-storage representation used by OPL-compatible USB workflows.

The implementation will treat:

- game metadata;
- UL file naming;
- part numbering;
- part counts;
- media type;
- CRC;
- split size

as separate, testable rules.

Exact rules must be verified against OPL source before coding.

## ul.cfg

Binary metadata associated with UL games.

Known project target:

- fixed-size records;
- display name;
- UL identifier;
- part count;
- media type;
- reserved/control bytes.

The exact offsets and byte values will be frozen only after cross-checking OPL source and independent implementations.

## ART

Optional artwork associated with Game ID.

Artwork management is planned, but artwork must never be required for game integrity.

## Compatibility rule

PS2-Manager targets **OPL compatibility**, not compatibility with one historical implementation alone.

When USBUtil, another manager and OPL disagree, the project should document the disagreement and use verified OPL behavior as the primary compatibility reference.
