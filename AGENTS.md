# MODU-C keymap-only contribution rules

This is a ZMK firmware repository for the MODU-C split keyboard. The scope of
all requested implementation work is **key remapping only**, except for the
limited, approved configuration changes below.

## Allowed firmware change

Only edit this firmware source file:

- `modu-module/boards/shields/modu/modu.keymap`

The `bindings` in this file define the layouts for `default_layer` and
`lower_layer`. Preserve the existing physical-key order, layer names, device
tree structure, includes, and copyright header unless a requested keymap
change specifically requires otherwise.

## Approved battery and Windows compatibility exception

The approved battery and Windows compatibility request permits only these
additional firmware configuration changes:

- `modu-module/boards/shields/modu/modu_left.conf`: set
  `CONFIG_ZMK_BATTERY_REPORTING=n` and add `CONFIG_BT_CTLR_PHY_2M=n`.
- `modu-module/boards/shields/modu/modu_right.conf`: set
  `CONFIG_ZMK_BATTERY_REPORTING=n`.

These settings selectively follow the original project's referenced V4.1.1
configuration repository on `main`; experimental branch changes are excluded.
Source commits in `22sh22/modu-c-zmk-config`:

- `58eecc638009fdbf7ca011c5b9a5e316a365c63a`: disable left 2M PHY.
- `b07a49b5c2b727dbc7f307f1c7591a8b4a5537f4`: disable left battery reporting.
- `8110c3e927f395c3afc5eb0508ffdc102a7fc905`: disable right battery reporting.

Port these settings into the existing shield configuration files listed above;
the upstream `config/*.conf` paths belong to a separate user-config build.
Disabling battery reporting suppresses incorrect readings; it does not
implement accurate battery measurement. ZMK Studio must remain disabled.
Preserve the personal keymap, Feature layer, Bluetooth reset, bootloader and
reset delays, Windows editor, build scripts, and dependency versions.
This exception does not authorize any other configuration or firmware changes.

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
`keymap-editor/dist/UnofficialModuKeymapStudio.exe`. The tool's `bin/`, `obj/`, and
`dist/` directories are generated artifacts and must not be committed. The
application may invoke the existing root `build.ps1` after saving the keymap,
but it must not alter that script or any firmware build configuration.

## Do not modify firmware support files

Do not edit, add, remove, or reformat any other firmware source, board/shield
definition, build configuration, module configuration, documentation, scripts,
or CI files. In particular, do not change `build.ps1`, `build.bat`,
`modu.keymap`'s surrounding DTS structure, `.conf` files (except for the exact
settings authorized above), `.yml` files,
`CMakeLists.txt`, `Kconfig*`, or files under `tools/`.

Do not update firmware dependencies, run firmware code generators, or make
incidental firmware cleanup changes. In addition to the limited configuration
exception above, this `AGENTS.md` may be updated to record the project working
agreement.

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
that no tracked firmware file other than the keymap or the exact configuration
settings authorized above changed. For the battery and Windows compatibility
request, the final diff must be limited to the two approved `.conf` files and
this rules file, with no keymap or editor changes. Verify both generated
configurations disable battery reporting and ZMK Studio, the left disables
2M PHY, and both UF2 files are generated. Windows reconnection, split pairing,
and key and trackball input require separate hardware verification; do not
automatically flash devices as part of this request. Files within
`keymap-editor/` and this rules file are permitted when they are part of the
separate keymap-tool work. Report any unverified build accurately rather than
treating it as successful.
