# MODU-C keymap-only contribution rules

This is a ZMK firmware repository for the MODU-C split keyboard. The scope of
all requested implementation work is **key remapping only**.

## Allowed change

Only edit this tracked source file:

- `modu-module/boards/shields/modu/modu.keymap`

The `bindings` in this file define the layouts for `default_layer` and
`lower_layer`. Preserve the existing physical-key order, layer names, device
tree structure, includes, and copyright header unless a requested keymap
change specifically requires otherwise.

## Do not modify

Do not edit, add, remove, or reformat any firmware source, board/shield
definition, build configuration, module configuration, documentation, scripts,
or CI files. In particular, do not change `build.ps1`, `build.bat`,
`modu.keymap`'s surrounding DTS structure, `.conf` files, `.yml` files,
`CMakeLists.txt`, `Kconfig*`, or files under `tools/`.

Do not update dependencies, run code generators, or make incidental cleanup
changes. This `AGENTS.md` is the sole exception to the keymap-only rule because
it records the project working agreement.

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
that no tracked file other than the keymap (plus this rules file when it is
being introduced or deliberately maintained) changed. Report any unverified
build accurately rather than treating it as successful.
