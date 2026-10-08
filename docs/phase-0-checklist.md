# Phase 0 Checklist

## Product

- [x] Define PS2-Manager as broader than a USBUtil clone.
- [x] Define Analyzer as first implementation.
- [x] Define read-only first pass.
- [x] Define Smart Install concept.
- [x] Add BIN/CUE to planned input formats.
- [x] Separate core from UI.
- [x] Choose C#/.NET direction.

## Research still required before format implementation

- [x] Verify complete `ul.cfg` byte layout from OPL source.
- [x] Verify UL filename/CRC algorithm.
- [x] Verify UL part size and numbering.
- [x] Verify CD/DVD media values.
- [x] Verify ISO handling in current OPL.
- [ ] Research BIN/CUE variants used by PS2.
- [x] Define FAT32 rules.
- [x] Define exFAT rules.
- [x] Define how ISO games are discovered versus UL games.
- [x] Define analyzer status/severity model.
- [x] Define test fixtures for valid and damaged libraries.

## Implementation order

1. Research/specification.
2. Core models.
3. Binary parsers.
4. Analyzer.
5. Automated tests.
6. UI.
7. Installation workflows.
8. Maintenance/repair.
