# Technical Specification: Orphan Record Detection and Safe Purging in `ul.cfg`

## Background

Legacy Open PS2 Loader (OPL) environments utilizing USBExtreme formats rely on a sequential metadata file (`ul.cfg`) where each record occupies exactly 64 bytes. During installation processes—particularly when interrupted or failing midway through using tools like USBUtil—metadata records may be written to `ul.cfg` before data fragment files (`ul.*`) are fully created or properly written to storage.

This results in **orphan records**: valid configuration entries pointing to game IDs or CRC structures that lack corresponding physical fragments on the storage medium.

## Core Principles of PS2-Manager

1. **Non-Destructive Diagnostics by Default:** Parsing and analysis components must never alter or purge files implicitly. Discrepancies are reported purely as structured `Diagnostic` items.
2. **Cross-Validation Layer:** The storage analyzer (`UlStorageAnalyzer`) correlates parsed metadata from `ul.cfg` with the actual file listings present in the target directory.
3. **Deterministic Round-Trip Rebuilding:** When a user explicitly requests cleanup or purging, the system serializes a filtered list of records using `UlCfgWriter`, ensuring correct structural padding (`0x00` padding and reserved offsets) without corrupting healthy entries.

---

## Diagnostic Specifications

### `OPL008` — Orphan Record Detected
* **Severity:** `Warning`
* **Condition:** Triggered when an entry exists within `ul.cfg` (parsed via `UlCfgParser`) but no matching chunk files (`ul.<CRC>.<GAME_ID>.*`) are discovered in the storage root.
* **Intended Action:** Prompts the user or automated repair assistant to review incomplete installations.

---

## Safe Purging Workflow

To remove orphan records safely without risking data loss:

1. **Parse:** Read and parse `ul.cfg` using `UlCfgParser`.
2. **Analyze:** Cross-reference records against disk files using `UlStorageAnalyzer`.
3. **Filter:** Exclude confirmed orphan records based on user consent or repair policies.
4. **Backup:** Generate a safety backup of the original configuration file (e.g., `ul.cfg.bak`).
5. **Serialize & Write:** Reconstruct a clean configuration binary using `UlCfgWriter.Serialize(...)` and overwrite `ul.cfg`.
