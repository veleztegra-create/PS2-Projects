# Phase 0 — Scope and Product Definition

## Status

Phase 0 establishes the project scope before implementation.

## Product goal

PS2-Manager is a modern Windows application for managing PlayStation 2 game libraries intended for use with Open PS2 Loader (OPL).

It is **not** simply a graphical replacement for USBUtil.

The product combines:

1. USB/library analysis.
2. OPL game management.
3. ISO installation.
4. Intelligent format selection.
5. Integrity validation.
6. Diagnostics and, later, assisted repair.
7. Backup/restore of OPL-related data.

## Main differentiator

A traditional USBUtil-style workflow answers:

> How do I put this game on my USB?

PS2-Manager should answer the broader question:

> What is on this PS2 device, is it healthy, what is wrong, and what is the safest way to install or manage this game?

## Planned capabilities

### Analyzer — first implementation target

Read-only analysis must be safe and must not modify the device.

It should eventually inspect:

- filesystem type;
- OPL directory structure;
- `ul.cfg`;
- UL game files;
- ISO games;
- game identifiers;
- missing UL parts;
- orphan UL files;
- invalid/inconsistent `ul.cfg` entries;
- fragmentation indicators where technically meaningful;
- overall library/USB status.

Initial command-line target:

`PS2Manager.exe --analyze E:`

### Game management

Planned:

- add games;
- remove games;
- rename games;
- detect Game ID;
- manage `ul.cfg`;
- manage optional cover art;
- verify installed games.

### Smart Install

The installer should inspect both the source image and the target filesystem before choosing an installation strategy.

Examples:

- ISO on a filesystem that supports direct ISO storage -> direct ISO when OPL-compatible.
- ISO exceeding FAT32's single-file limit -> UL/split representation.
- BIN/CUE -> inspect and validate before conversion/installation.

The application should hide format complexity from the normal user while exposing the decision in an advanced/details view.

### Diagnostics and repair

Future versions may provide:

- detailed health reports;
- backup before destructive operations;
- assisted repair;
- reconstruction of inconsistent metadata;
- safe cleanup of orphaned files.

There should be no generic "defragment" button that pretends conventional defragmentation is always the right solution.

## Supported input/management formats

### ISO

Primary disc-image input.

### BIN/CUE

Planned input support. BIN is not treated as a generic ISO with another extension.

The application should inspect the associated CUE and determine:

- track layout;
- data/audio tracks;
- CD/DVD characteristics;
- whether the image can be safely processed;
- Game ID where available.

Implementation is intentionally deferred until the PS2 BIN/CUE variants used in practice are documented.

### UL / USBExtreme format

Compatibility target for OPL USB libraries, including split game files and their metadata.

### ul.cfg

Binary OPL metadata file. Its exact byte layout, CRC behavior, media type values and compatibility rules will be documented separately before implementation.

## Filesystem targets

The project will account for:

- FAT32;
- exFAT;
- other filesystems only when OPL compatibility is verified.

Filesystem capabilities must influence installation decisions.

## Version roadmap

- **v0.1** — Read-only USB Analyzer.
- **v0.2** — Library management.
- **v0.3** — ISO/BIN-CUE installation.
- **v0.4** — Removal and maintenance.
- **v0.5** — Diagnostics and assisted repair.
- **v1.0** — Stable usable release.

These version numbers are planning targets, not release promises.

## Technical direction

- Product language: C#.
- Runtime/platform: modern .NET, Windows desktop.
- UI: separate from the core logic.
- Core must be testable without the UI.
- Prefer portable/no-admin deployment where practical.
- OPL and existing tools are compatibility/reference sources, not assumptions.

## Reference projects

PS2IsoManager is an important reference because it demonstrates a modern .NET/WPF implementation of ISO splitting, Game ID detection and `ul.cfg` management.

It is not the product definition for PS2-Manager.

Open PS2 Loader is the authoritative compatibility reference for behavior that depends on OPL.

USBUtil is a historical reference for legacy workflows and formats.

## Out of scope for Phase 0

- modifying an attached USB device;
- automatic repair;
- automatic deletion;
- conventional defragmentation;
- copying third-party source code into this repository.

## Phase 0 exit criteria

Before implementing the analyzer:

1. Document the `ul.cfg` structure.
2. Document UL filename/part rules.
3. Verify CRC behavior against OPL source.
4. Document CD/DVD media distinctions.
5. Research PS2 BIN/CUE variants.
6. Document FAT32/exFAT installation rules.
7. Define analyzer result/status model.
8. Define tests for valid, incomplete and inconsistent libraries.
