# NOVA Workspace — final report

**NOVA Workspace** is one native Windows office suite: **NOVA Write**, **NOVA Grid**,
**NOVA Deck** and **NOVA PDF**, started from a single home screen, **NOVA Desk**.

No browser, no extension, no account, no cloud, no internet.

---

## What ships

| | |
|---|---|
| `novadesk.exe` | NOVA Desk — the home screen (.NET Framework 4.8 WPF) |
| `novawrite.exe` | NOVA Write — the word processor (native Rust) |
| `novagrid.exe` | NOVA Grid — the spreadsheet (native Rust) |
| `novadeck.exe` | NOVA Deck — the presentation app (native Rust) |
| `novapdf.exe` | NOVA PDF — the PDF reader and tools (native Rust) |
| `Start NOVA Desk.vbs` | The launcher (no console window; what the shortcuts use) |
| `Start NOVA Desk.cmd` | Compatibility launcher (brief console window) |
| `Install-NOVAWorkspace.cmd` | The optional per-user installer (`/assoc`, `/remove`) |
| `Licenses/` | Each application's licence, notice and attribution, plus the vendored crates' |

The download is `publish/NOVAWorkspace.zip` — **88.5 MB** (235 MB unpacked), one file, one set of
binaries for Windows 7 SP1 through Windows 11, x64. NOVA Write carries its own fonts, Tamil
included: Madhuram (TAU-Marutham) regular, bold and italic, and Tamil types on any keyboard with
the Bamini layout.

It is also served publicly from
**https://nova-workspace.jayaswinjay.workers.dev** — a Cloudflare Worker with static
assets, plus a `/download` route that streams the zip out of a **private** GitHub
release. The GitHub token is a Worker secret; visitors never receive it. The
streamed file was verified byte-for-byte against the local zip
(SHA-256 `B9032FFB…9A4D6611`, 92,744,133 bytes, release v1.0.6).

---

## Architecture

```
src/Nova.Hub/          NOVA Desk: the WPF home screen (net48, one build-time package reference)
reference/wordcraft/   NOVA Write: the document model, layout, .docx/.rtf/.odt, rendering
reference/gridcraft/   NOVA Grid: the formula engine, workbook model, .xlsx, charts
reference/deckcraft/   NOVA Deck: the presentation model, .novadeck and .pptx, rendering
reference/pdfcraft/    NOVA PDF: the PDF engine, .pdf I/O, forms, signatures, OCR
assets/nova/           The NOVA mark icons (16…1024 in a four-hue family)
assets/brand/          The NOVA mark and wordmark
scripts/               Build, package, verify and re-brand passes
publish/               The built bundle and the download
```

Each application is layered the same way: a model and an engine at the bottom, a command registry
in the middle, and a thin UI crate on top that can be swapped. The UI never owns behaviour: it
reads state and dispatches command ids.

**Everything is a command.** The ribbon, the menus, the keyboard shortcuts, the command palette,
the CLI, the MCP server and the JSON control channel all dispatch by the same id. A behaviour
cannot exist in one surface and be missing from another.

---

## The two decisions that shaped it

### The home screen is its own program

NOVA Desk is a separate WPF application, not a view inside one of the four. The four applications
keep their own windows, their own file associations and their own processes, so NOVA Desk can
start any of them, be replaced, or be skipped: each application still opens files from Explorer
on its own.

### The home screen does not look like Windows

A gradient hero panel, four app cards with their own gradient tiles, a hover lift with a coloured
glow, a violet halo behind the hero and a custom title bar. The one screen every user sees
carries the identity.

### The identity is NOVA's throughout

The four applications no longer wear another product's chrome. Every window uses the NOVA mark
and the NOVA palette — a warm paper light theme, a deep-ink dark theme, the NOVA violet accent —
and the application icons are a matched family: the white NOVA mark on per-app NOVA gradients.
The built-in samples are NOVA's own — The Open Studio Field Guide (NOVA Write), the Workshop
Budget, Studio Retail Dashboard and Workshop Register workbooks (NOVA Grid) and the Welcome to
the Open Studio deck (NOVA Deck). The internal Rust module names remain; nothing on screen
renders them.

---

## Nothing leaves the machine

- The update-checking subsystem was **removed** from all four applications, not merely switched
  off: the dependency is gone from every manifest, the constants are deleted, the start-up check
  and the self-update helper are gone.
- Help ▸ Check for Updates still works. It answers locally that this copy does not check for
  updates.
- No link in any interface leaves the app. The About boxes, the Help menus, the contributor
  lists, the CLI metadata and the MCP server metadata carry no address.
- Only the links inside the user's own documents follow, as they should.

---

## What was verified, and how

### The bundle scans clean

`scripts\Scan-NovaBundle.ps1` reads every shipped binary — ASCII and UTF-16LE — and every shipped
text file, and fails on another brand name in a person-readable capitalisation, or on any binary
carrying an address the suite must never offer:

```
RESULT: the bundle is free of the old brand.
```

The scan is deliberately honest about one boundary. The cloned engines keep their upstream Rust
package and module names, so every release binary still carries strings like `wordcraft_engine::`
in panic locations, log targets, thread names and egui widget ids. Removing those means renaming
the crates, which would change the engines rather than re-brand the surface — the decision is
recorded in `DECISIONS.md`. The scan lists them (`=== the binaries: internal Rust crate/module
stems ===`) instead of hiding them behind a blanket CLEAN. They are not user-visible: no screen,
dialog or help text renders them, and the addresses sections confirms the binaries carry no link to
anywhere. What a person does see is clean — the window titles, the About boxes, the file dialogs,
the in-document fonts and the saved-file properties (e.g. `NOVA Grid` in an xlsx's `appName`).

### Every window starts and says what it should

`scripts\Test-NovaLaunches.ps1` starts NOVA Desk through the launcher `.cmd`, exactly as a user
double-clicks it, and then each application the way the hub starts them:

```
  novadesk       RUNNING  NOVA Desk
  novawrite      RUNNING  NOVA Write
  novagrid       RUNNING  Book1 — NOVA Grid
  novadeck       RUNNING  NOVA Deck
  novapdf        RUNNING  NOVA PDF
```

### The applications build clean

All four build in release, offline, with zero errors and zero warnings. NOVA Desk builds in
release with zero errors.

### The breadth is measured, not claimed

Each application ships a `cargo xtask parity` check that compares its feature catalog with its
live command registry and fails if a claimed feature has no command behind it.

| App | Measure | Result |
|---|---|---|
| NOVA Write | Catalog features with a command | 505 / 520 (97.1%) |
| NOVA Grid | Excel commands implemented | 276 / 292 (94.5%) |
| NOVA Deck | Weighted breadth parity, 191 features | 84% |
| NOVA PDF | Acrobat features shipped | 452 shipped, 102 partial |

---

## Known limitations

- **Windows 7 SP1 / 8.1 / 10 / 11, x64.** One set of binaries runs on all four. The four Rust
  applications are built for `x86_64-win7-windows-msvc` with a static C runtime and a local
  compatibility crate, and are audited for Windows 8+ imports; NOVA Desk targets .NET Framework
  4.8, which is in-box on Windows 10 1903+ and Windows 11 and a one-time Windows Update install on
  Windows 7 SP1 and 8.1. The Windows 11 runtime path is exercised here; the Windows 7 path is
  verified statically, by import table (`scripts/Test-NovaWin7Imports.ps1` audits all five shipped
  executables), because no Windows 7 machine is part of the build environment.
- **The Tamil faces are Madhuram (TAU-Marutham), Hind Madurai, Noto Sans Tamil and Noto Serif
  Tamil.** Latha is Microsoft's and Bamini is a legacy font that maps Tamil letters onto Latin keys,
  so neither is bundled; see DECISIONS.md §21 for every name in that request, what ships in its
  place, and why TAU-Marutham's `fsType = 8` does not stop it.
- **Tamil typing is on by default, on the Bamini layout.** That is what "it still comes
  as English" asked for, and the price is that a keyboard typed on with no Tamil intent produces
  Tamil: the status bar's chip switches it, and the UI tests that type Latin say so in their own
  fixtures. The layout is the one Tamil typists learned first, and it types the way they press
  it: a front sign attaches to the consonant key right after it, `h` is ர before a ி/ீ/் sign and
  ா elsewhere, `s` is ௌ or ள by the key after it, and a sign with no consonant after it is typed
  alone rather than carried into the next word. A 69-word sweep through the running app
  types every word — the owner's practice words, the words they reported broken, and the Sanskrit
  letters (ஸ்ரீ, ஷ, ஹ, ஜ, க்ஷ) — correctly.
- **The parity numbers measure breadth, not fidelity.** A command existing does not mean it
  matches the original in every detail; each application's own `docs/target-app-parity.md` is the
  honest assessment.
- **NOVA PDF's text recognition needs its models.** Without them it says so rather than failing
  quietly.
- **The update checker is gone for good.** There is no automatic update path in this release.
  A new suite is a new download.

---

## Provenance and licences

The four engines are clean-room, permissively licensed upstream Rust projects, vendored under
`reference/`. Each ships its own `LICENSE-APACHE`, `LICENSE-MIT`, `NOTICE` and `ATTRIBUTION.md`,
and all of them travel in the bundle's `Licenses` folder.

NOVA Desk, the launcher, the installer, the icons and the re-brand work are original to this
project. **NOVA Workspace as a product is closed-source commercial software**: free for 30 days,
then ₹6 a month for one person, with the source kept in a private repository. The upstream licence
texts still travel with the bundle as the law requires — see the repository root's
`LICENSE-APACHE`, `LICENSE-MIT` and `NOTICE`. The NOVA name and marks are trademarks of the NOVA
Workspace team; each application's `docs/brand/LICENSE-brand.txt` holds the terms.

No Microsoft, Adobe, Avid or Autodesk iconography, artwork, fonts, templates or themes appear
anywhere in the suite. Every asset is original, public domain, CC0, or permissively licensed, and
every one is listed in the attribution file that ships with it.
