# PS2 Projects

Research and development workspace for modern PlayStation 2 storage and OPL tooling.

## Projects

- **PS2-Manager** — planned modern OPL game library, USB analyzer, installer, validator and repair assistant.
- **PS2IsoManager-reference** — external reference project; its source is not copied into this repository.

## Documentation

The `docs/` directory contains the technical research and specifications that define PS2-Manager.

## Development principle

PS2-Manager is designed as an OPL-compatible tool, not as a clone of USBUtil. Existing formats such as `ul.cfg` and `ul.*` are compatibility targets, while diagnostics, validation and intelligent installation are core differentiators.
