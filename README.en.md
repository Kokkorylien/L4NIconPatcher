# L4N Icon Patcher

English | [简体中文](README.md)

A lightweight Windows utility that adds survivor portraits and incapacitation icons to **L4N Survivor model VPKs** for *Left 4 Dead 2*. It uses native WinForms, requires no installation, and supports instant English / Simplified Chinese switching.

## Download and run

Download `L4NIconPatcher-v1.1.0-win.zip` from [Releases](https://github.com/Kokkorylien/L4NIconPatcher/releases/latest), **extract the entire archive**, and run `L4NIconPatcher.exe`.

- Requires Windows 10/11 and .NET Framework 4.8.
- The release includes `bin/vpk.exe` and its required DLLs. Keep the folder structure intact.
- Click **English / 简体中文** in the upper right to switch languages. Your choice is saved for the next launch.

## Usage

1. **Source icons:** copy a VPK containing survivor portraits in File Explorer, then press **Ctrl+V** in the first field.
2. **Target model:** paste the L4N model VPK into the second field.
3. Click **Generate VPK**, then **Open output folder** when finished.

You can also paste a quoted path, type a path, drop a file into a field, or use Browse. Each field accepts one file.

The source should contain `materials/vgui/s_panel_*.vtf`; the target should contain `models/l4n/s/<model-name>/`. The tool selects one portrait and incap pair from the source and creates the matching VTF/VMT files for every target model folder. It does not automatically map different source survivors to different target models. If no separate incap icon is available, the portrait is reused.

The output is saved beside the target as `<target-name> [+图标].vpk`. Both languages use the same filename rule. The source and target VPKs are preserved; generating again replaces an existing output VPK with that name.

## Path protection

- Input names and the VPK index are checked before extraction. Blank names, trailing spaces or periods, reserved device names, and unsafe paths are rejected.
- The VPK format's single-space root-directory marker is handled correctly.
- Processing stops if an extraction or output folder already exists. Move or rename it first; existing directories are preserved.
- Only directories created by the current operation are cleaned up. Cleanup failures are reported.

## Build from source

Install Visual Studio 2022 with the **.NET desktop development** workload and .NET Framework 4.8 development tools.

```powershell
msbuild L4NIconPatcher.sln /p:Configuration=Release
msbuild tests/BoundaryTests.csproj /p:Configuration=Release
./tests/bin/BoundaryTests.exe
./scripts/build-release.ps1
```

`tools/vpk/` is copied into the output's `bin/` folder. The release script discovers MSBuild and produces a portable ZIP with runtime dependencies, both READMEs, third-party notices, and a SHA-256 checksum file. Tests generate minimal VPK fixtures and cover unsafe paths, file paste, language switching and saved preferences, plus complete generation and content checks using the real vpk.exe.

## Third-party components

`vpk.exe` and the bundled DLLs are Valve components. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for attribution and applicable terms. No game or Workshop model content is included.

[Author](https://space.bilibili.com/2100612)
