
# Technical Specification: Storage Diagnostics and Integrity Analysis (`UlAnalyzer`)

## Background

Legacy Open PS2 Loader (OPL) environments utilizing USBExtreme formats rely on a sequential metadata file (`ul.cfg`) where each record occupies exactly 64 bytes. During installation processes—particularly when interrupted or failing midway through using tools like USBUtil—discrepancies arise between the registered metadata and the actual files present on the storage medium.

**PS2-Manager** implements a non-destructive cross-validation engine (`UlAnalyzer`) to audit storage health without modifying binary structures implicitly.

---

## Diagnostic Codes Specification

The analyzer reports structural anomalies using standardized OPL diagnostic codes:

### `OPL003` — Missing Part Fragments
* **Severity:** `Warning` / `Error` (depending on missing sequences)
* **Condition:** Triggered when an entry in `ul.cfg` declares a specific part count or image identifier, but the expected physical part files (`ul.<CRC>.<GAME_ID>.xx`) are missing or incomplete in the directory.
* **Context:** Common in failed USBUtil transfers where the metadata record was committed before file copying completed (e.g., incomplete installations like *Disney Princess Enchanted Journey*).

### `OPL004` — Orphan Storage Files
* **Severity:** `Warning`
* **Condition:** Triggered when physical fragment files (`ul.*`) exist in the storage root but lack a corresponding metadata record inside `ul.cfg`.
* **Context:** Leftover debris from deleted entries or partial manual file transfers.

---

## Safe Maintenance Workflow

1. **Parse:** Read and parse `ul.cfg` using `UlCfgParser`.
2. **Analyze:** Cross-reference metadata records against physical disk files using `UlAnalyzer`.
3. **Review:** Present identified anomalies (`OPL003`, `OPL004`) to the user via the diagnostic interface.
4. **Clean / Purge:** With explicit user confirmation, reconstruct a clean configuration binary using `UlCfgWriter` or manage disk files accordingly.
