# MODU-C keymap-only contribution rules

This is a ZMK firmware repository for the MODU-C split keyboard. The scope of
all requested implementation work is **key remapping only**.

## Allowed firmware change

Only edit this firmware source file:

- `modu-module/boards/shields/modu/modu.keymap`

The `bindings` in this file define the layouts for `default_layer` and
`lower_layer`. Preserve the existing physical-key order, layer names, device
tree structure, includes, and copyright header unless a requested keymap
change specifically requires otherwise.

## Separate keymap tooling

The repository-root `keymap-editor/` directory is an independent, local tool
for visually editing the allowed keymap. Files within that directory may be
created and maintained as needed, including its own dependencies and
documentation. The tool must not be imported by, referenced from, or coupled
to the firmware build.

`modu.keymap` remains the firmware source of truth. The tool may read it and
write an edited version of it, but must not rewrite unrelated firmware files.

The native editor targets Windows 10/11 x64 and is published as a portable,
self-contained executable. Build and test it from the repository root with:

```powershell
.\keymap-editor\test.ps1
.\keymap-editor\publish.ps1
```

The published application is written to
`keymap-editor/dist/ModuKeymapStudio.exe`. The tool's `bin/`, `obj/`, and
`dist/` directories are generated artifacts and must not be committed. The
application may invoke the existing root `build.ps1` after saving the keymap,
but it must not alter that script or any firmware build configuration.

## Do not modify firmware support files

Do not edit, add, remove, or reformat any other firmware source, board/shield
definition, build configuration, module configuration, documentation, scripts,
or CI files. In particular, do not change `build.ps1`, `build.bat`,
`modu.keymap`'s surrounding DTS structure, `.conf` files, `.yml` files,
`CMakeLists.txt`, `Kconfig*`, or files under `tools/`.

Do not update firmware dependencies, run firmware code generators, or make
incidental firmware cleanup changes. This `AGENTS.md` is the sole exception to
the keymap-only firmware rule because it records the project working agreement.

## Build verification

Use the existing build entry point; it invokes `west build` for both split
halves with the required board, shield, and extra ZMK modules:

```powershell
.\build.ps1 -ZmkApp C:\zmk\app
```

Equivalently on Windows:

```bat
build.bat C:\zmk\app
```

The ZMK app path must contain ASCII characters. A working ZMK development
environment, Zephyr SDK, and `west` installation are required. The generated
`build/` and `outputs/` artifacts are intentionally gitignored; do not commit
them. Do not alter the build scripts to work around an unavailable local ZMK
environment.

## Before handoff

Review `git diff -- modu-module/boards/shields/modu/modu.keymap` and confirm
that no tracked firmware file other than the keymap changed. Files within
`keymap-editor/` and this rules file are permitted when they are part of the
separate keymap-tool work. Report any unverified build accurately rather than
treating it as successful.
