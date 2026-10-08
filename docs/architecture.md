# PS2-Manager — Initial Architecture

## Principles

- Core logic must not depend on the UI.
- File parsing and validation must be deterministic.
- Read-only analysis must be safe.
- Destructive operations require explicit user intent.
- External formats are represented by explicit models rather than scattered byte manipulation.
- Every compatibility rule should have a documented source.

## Planned solution

```
PS2-Manager/
├── src/
│   ├── PS2Manager.Core/
│   ├── PS2Manager.IO/
│   ├── PS2Manager.Services/
│   └── PS2Manager.UI/
├── tests/
│   └── PS2Manager.Core.Tests/
└── docs/
```

### PS2Manager.Core

Domain models, format rules, validation results and interfaces.

### PS2Manager.IO

Filesystem and binary I/O, ISO/BIN-CUE readers, `ul.cfg` parsing and UL file inspection.

### PS2Manager.Services

Analyzer, library manager, installer, verification and later repair workflows.

### PS2Manager.UI

Windows desktop presentation layer. It must call services rather than implement format logic itself.

## First vertical slice

The first implementation should be:

```
Target path
   ↓
Filesystem inspection
   ↓
OPL structure discovery
   ↓
ul.cfg parser
   ↓
UL file inventory
   ↓
Consistency checks
   ↓
Read-only analysis report
```

No writing to the target device is required for this first slice.
