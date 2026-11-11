# NOVA Workspace — build status

One native Windows office suite, built from one folder.

| Piece | What it is | Built by |
|---|---|---|
| **NOVA Desk** (`novadesk.exe`) | The home screen: app grid, recents, file routing | .NET Framework 4.8 WPF, `src/Nova.Hub` |
| **NOVA Write** (`novawrite.exe`) | Word processor | Rust, `reference/wordcraft` |
| **NOVA Grid** (`novagrid.exe`) | Spreadsheet with a formula engine | Rust, `reference/gridcraft` |
| **NOVA Deck** (`novadeck.exe`) | Presentations (`.novadeck`, `.pptx`) | Rust, `reference/deckcraft` |
| **NOVA PDF** (`novapdf.exe`) | PDF reader and tools | Rust, `reference/pdfcraft` |

No browser, no extension, no account, no cloud, no internet: nothing in the suite opens a web
page or checks for updates.

---

## Build

```powershell
# 1. the four native applications: one binary for Windows 7 SP1 through Windows 11
$env:LIB=''
$env:CARGO_NET_OFFLINE='true'
$env:RUSTFLAGS='-C target-feature=+crt-static'
cargo +nightly build --release --offline --manifest-path reference\wordcraft\Cargo.toml -p novawrite --target x86_64-win7-windows-msvc -Z build-std=std,panic_abort
cargo +nightly build --release --offline --manifest-path reference\gridcraft\Cargo.toml -p novagrid  --target x86_64-win7-windows-msvc -Z build-std=std,panic_abort
cargo +nightly build --release --offline --manifest-path reference\deckcraft\Cargo.toml -p novadeck  --target x86_64-win7-windows-msvc -Z build-std=std,panic_abort
cargo +nightly build --release --offline --manifest-path reference\pdfcraft\Cargo.toml  -p novapdf   --target x86_64-win7-windows-msvc -Z build-std=std,panic_abort

# 2. NOVA Desk
C:\Users\jayas\AppData\Local\Microsoft\dotnet\dotnet.exe build src\Nova.Hub\Nova.Hub.csproj -c Release

# 3. the bundle (all four apps + NOVA Desk + launchers + licences + zip)
powershell -ExecutionPolicy Bypass -File scripts\Build-NovaWorkspace.ps1
```

`scripts\Build-NovaWorkspace.ps1` writes `publish\NOVAWorkspace\` (unpacked) and
`publish\NOVAWorkspace.zip` (the download). It copies the built applications from each
repository's `target\x86_64-win7-windows-msvc\release\`, publishes NOVA Desk beside them, and writes
`Start NOVA Desk.vbs` (the hidden launcher, what the shortcuts point at), `Start NOVA Desk.cmd`
(compatibility launcher, shows a brief console window) and `Install-NOVAWorkspace.cmd`.

There is no bundled runtime. NOVA Desk runs on .NET Framework 4.8, which is built into Windows 10
(1903 and later) and Windows 11 and installs once through Windows Update on Windows 7 SP1 and 8.1;
the four applications need nothing installed at all.

### Current bundle

| | |
|---|---|
| `novawrite.exe` | 69.7 MB (carries the bundled Latin *and* Tamil fonts) |
| `novagrid.exe` | 37.0 MB |
| `novadeck.exe` | 52.0 MB |
| `novapdf.exe` | 75.0 MB |
| `novadesk.exe` | 0.06 MB (a .NET Framework assembly: WPF itself comes from the framework) |
| unpacked | 234.0 MB |
| `NOVAWorkspace.zip` | 87.9 MB |

---

## Publish the site

```powershell
# 1. build the bundle (above), then publish the zip as resumable byte-range parts
powershell -ExecutionPolicy Bypass -File scripts\Upload-NovaRelease.ps1 -PartMB 16 -Parallel 6

# 2. deploy the site (static assets + the /download Worker)
cd website
wrangler deploy
```

The site is live at **https://nova-workspace.jayaswinjay.workers.dev**. `/download` streams the
zip out of the private release, so the browser never sees a GitHub token. The streamed file has
been verified byte-for-byte against `publish\NOVAWorkspace.zip`
(SHA-256 `B9032FFB…9A4D6611`, release v1.0.6; v1.0.5 was `BA11FD98…4769D530FD`).

---

## Verify

```powershell
# every window starts from the built bundle and says what it should
powershell -ExecutionPolicy Bypass -File scripts\Test-NovaLaunches.ps1

# no old brand and no outbound address anywhere in the shipped files
powershell -ExecutionPolicy Bypass -File scripts\Scan-NovaBundle.ps1

# the shipped executables import nothing Windows 7 lacks
powershell -ExecutionPolicy Bypass -File scripts\Test-NovaWin7Imports.ps1
```

`Test-NovaLaunches.ps1` starts NOVA Desk through `Start NOVA Desk.vbs` and each application the
way the hub starts them, then reports every window title:

```
  novadesk       RUNNING  NOVA Desk
  novawrite      RUNNING  NOVA Write
  novagrid       RUNNING  Book1 — NOVA Grid
  novadeck       RUNNING  NOVA Deck
  novapdf        RUNNING  NOVA PDF
```

`Test-NovaWin7Imports.ps1` reads the import table of all five shipped executables and fails on a
DLL Windows 7 lacks (`combase.dll`, the WinRT and UCRT API sets, `windows.data.pdf.dll`) or an
entry point it lacks (`WaitOnAddress`, `ProcessPrng`, `GetSystemTimePreciseAsFileTime`). It ends
with `RESULT: every executable loads on Windows 7 SP1 - no Windows 8+ DLL or entry point.` It is the
audit the Windows 7 claim rests on: the `win7-compat` crate supplies the Windows 8+ entry points the
dependency tree imports, and this script is what proves they are no longer in the import table.
Those shims export their plain names only in release (`#[cfg_attr(not(debug_assertions), no_mangle)]`):
a raw-dylib import object carries a whole DLL's stubs, so a debug build that references any *other*
combase, WinRT or `windows.data.pdf` function pulls one in and it defines the same names
(`LNK2005`, then `LNK1169`). Release — the only build that ships, and the only one that must run on
Windows 7 — compiles exactly what it always did; in debug the shims keep their mangled names, so
`cargo test` links and the real imports are used on the build machine.

`Scan-NovaBundle.ps1` reads every shipped binary (ASCII and UTF-16LE) and every shipped text file,
and fails on another brand name in a person-readable capitalisation, or on a binary carrying an
address the suite must never offer. It also reports the lower-case internal Rust module stems
(`wordcraft_`, `deckcraft-…`) that are compiled into the engines and are not user-visible - nothing
in the interface renders them. It ends with:

```
RESULT: the bundle is free of the old brand.
```

---

## Install

`Install-NOVAWorkspace.cmd` (inside the bundle) installs to
`%LOCALAPPDATA%\Programs\NOVA Workspace`, registers the file associations for `.docx`, `.doc`,
`.rtf`, `.txt`, `.odt`, `.md`, `.xlsx`, `.xls`, `.csv`, `.tsv`, `.ods`, `.novadeck`, `.pptx`,
`.ppt`, `.odp` and `.pdf`, and creates the `NOVA Desk` shortcut. It needs no administrator
rights and can be undone from the same script.

---

## Notes

- **Bare `dotnet` fails** on this machine — it is the runtime-only stub. Use the full SDK path.
- **`$env:LIB` invalid** — set `$env:LIB=''` before a Cargo build.
- **Stop `nova*` processes before rebuilding** — a running application locks its own `.exe`.
- **NOVA Desk needs .NET Framework 4.8** (in-box on Windows 10 1903+ / Windows 11; a Windows Update
  install on Windows 7 SP1 and 8.1). The launcher checks the registry and explains it; the four
  applications do not need it.
- **The apps are built for `x86_64-win7-windows-msvc`** with `+crt-static`, so the import table is
  `kernel32` + `ntdll` plus DLLs that exist on Windows 7. `win7-compat` (`D:\wrkspasce\win7-compat`)
  shadows the remaining Windows 8+ entry points; audit any rebuild with
  `python F:\vhtemp\opencode\pe_imports.py <exe>`.
- **`src/Nova.Shell`** is the earlier standalone WPF suite. It still builds and runs
  (`NovaOffice.exe --selftest`), but it is **not** part of the shipped bundle: the shipped home
  screen is NOVA Desk in `src/Nova.Hub`, and the four shipped applications are the native Rust
  ones. The old `NovaOffice.sln` (WinUI 3 / Windows App SDK) was never built — it needs NuGet
  packages this machine cannot reach — and is not part of the product.

---

## Screenshots

`docs/screenshots/` holds the rendered windows:

| File | What it shows |
|---|---|
| `nova-desk.png` | NOVA Desk, the home screen (rendered by `novadesk.exe --capture <dir>`) |
| `nova-write.png` | NOVA Write, rendered headlessly by the `ui_shot` example |
| `nova-grid.png` | NOVA Grid, rendered headlessly by the `snapshot` example |
| `nova-deck.png` | NOVA Deck, photographed through its own control channel |
| `nova-pdf.png` | NOVA PDF, rendered headlessly by the `shot` example |
