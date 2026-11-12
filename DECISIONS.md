# NOVA Workspace — decisions log

The decisions that shaped the shipped suite, and why.

---

## 1. The home screen is its own application

**Decision.** NOVA Desk (`src/Nova.Hub`) is a separate .NET 8 WPF program (`novadesk.exe`), not a
view inside one of the four applications.

**Why.** The four applications are Rust engines with their own windows, their own file associations
and their own lifetime. A hub that lives inside one of them would tie the other three to that
application's process and its bugs. A separate launcher can start any of them, be replaced, or be
skipped entirely — the applications still open files from Explorer on their own.

**Consequence.** The hub has to find its siblings on disk. It looks next to itself, in an `apps/`
folder beside itself, and in the build folder, and says so plainly if it cannot.

---

## 2. The home screen does not look like Windows

**Decision.** NOVA Desk has its own visual identity: a gradient hero panel, four app cards with
their own gradient tiles, a hover lift with a coloured glow, a violet halo behind the hero, and a
custom title bar.

**Why.** A suite of four office applications that opens into the stock Windows shell reads as a
thin wrapper. The home screen is the one screen every user sees; it should carry the brand.

**Consequence.** WPF only, no NuGet packages: `net8.0-windows7.0`, `UseWPF`, zero package
references, so the bundle stays offline and self-contained.

---

## 3. Keep the engines, replace the surface

**Decision.** The four applications keep the upstream Rust engines — document model, layout, format
readers and writers, formula engine, renderers, command registry. Only the user-visible surface is
NOVA's: names, icons, strings, translations, window titles and licence notices.

**Why.** The engines are the product. They are clean-room, permissively licensed
(MIT OR Apache-2.0), tested, and far more capable than anything that could be written from
scratch in the time available. Rewriting them would have thrown away years of work for no gain.

**Consequence.** Internal crate, module and storage identifiers keep their original names. They
are never shown to a user and are not part of the product.

---

## 4. Nothing leaves the machine

**Decision.** The suite never opens a web page, never checks for an update, and never phones home.
The update-checking subsystem was removed from all four applications rather than merely disabled.

**Why.** The updater pointed at repositories that do not exist and contacted a public API on every
start. A feature that cannot work and that reaches out to the network is a liability, not a
feature. The suite is installed as one folder, so there is nothing to fetch.

**Consequence.** Help ▸ Check for Updates still works and says, locally, that this copy does not
check for updates. The About boxes, the Help menus, the contributor lists and the CLI metadata
carry no addresses. Only the links inside the user's own documents follow.

---

## 5. `.novadeck`, and no other old extension

**Decision.** NOVA Deck's native format is `.novadeck`, MIME type
`application/vnd.novaworkspace.deck+zip`. The old `.deckcraft` extension is gone from the source,
from the binaries, from the docs and from the installer.

**Why.** The file extension is part of the product's identity. A user who saves a deck should not
see the name of a product that is not theirs.

**Consequence.** The legacy MIME constant was deleted rather than emptied: an empty MIME string
made `sniff()` call `slice::windows(0)`, which panics.

---

## 6. One folder, no installer required

**Decision.** The suite ships as one folder: four self-contained executables and a .NET Framework
4.8 home screen, launched by `Start NOVA Desk.vbs` (a `wscript` launcher — GUI process, so no
console window). `Start NOVA Desk.cmd` stays as a compatibility launcher that shows a brief console
window. The installer is optional and per-user.

**Why.** "Zero install" is the strongest distribution promise, and every Windows from 7 SP1 up
already has, or can get in one Windows Update, the one runtime the home screen needs. Shipping a
private runtime was the earlier answer; dropping it costs the user nothing and takes about a
hundred megabytes out of the download (see §20).

**Consequence.** The installer writes to `%LOCALAPPDATA%\Programs\NOVA Workspace`, needs no
administrator rights, and can be undone.

---

## 7. Every data name the user can see is NOVA's

**Decision.** The settings folders are `NOVA Write`, `NOVA Grid`, `NOVA Deck` and `NOVA PDF`; the
log files are `novawrite.log`, `novagrid.log`, `novadeck.log` and `novapdf.log`; the environment
variables are `NOVAWRITE_*`, `NOVAGRID_*`, `NOVADECK_*` and `NOVAPDF_*`; the command-line tools
are `novawrite-cli`, `novagrid-cli`, `novadeck-cli` and `novapdf-cli`.

**Why.** A folder in `%APPDATA%` or a log file on disk is visible to the user and to their tools.
Names that belong to another product are noise at best and a support question at worst.

**Consequence.** 223 files changed in one pass. The environment-variable prefixes are read by
scripts and by users; the old ones are gone, not aliased.

---

## 8. The build is verified, not assumed

**Decision.** Two scripts decide whether a build is shippable:
`scripts\Test-NovaLaunches.ps1` starts every window from the built bundle and reports its title,
and `scripts/Scan-NovaBundle.ps1` reads every shipped binary (ASCII and UTF-16LE) and every
shipped text file and fails on another brand name or an address the suite must never offer.

**Why.** "It compiles" is not "it ships". The scan is the guard against a stale brand string or a
surviving hyperlink in a binary that nobody would think to look at.

**Consequence.** The launch test starts the hub through the launcher `.cmd` exactly as a user
double-clicks it, and the scan runs over the bundle, not over the source tree.

---

## 9. The bundle carries one honest download

**Decision.** `publish/NOVAWorkspace.zip` is the only download. The superseded distributions of
the earlier WPF shell are gone.

**Why.** Two zip files with similar names in the same folder is a support problem before it is a
feature.

---

## 10. The old shell stays, but out of the product

**Decision.** `src/Nova.Shell` (the earlier standalone WPF suite) still builds and still runs its
self-test. It is not in the bundle, and the README says so.

**Why.** It is a working fallback with its own formula-engine tests. Deleting it would throw away
tests; shipping it would ship two suites. The answer is to keep it and say plainly which one
ships.

---

## 11. The abandoned WinUI 3 attempt stays in the tree, out of the product

**Decision.** The `NovaOffice.sln` WinUI 3 solution and its projects are kept in the tree. They
cannot be built here — the Windows App SDK and 30-odd NuGet packages are not reachable from this
machine — and they are not part of the shipped suite.

**Why.** The same reason as the old shell: the notes are worth keeping, and a build that cannot
run is not a product.

---

## 12. Screenshots show the product that ships

**Decision.** The screenshots in `docs/screenshots/` are rendered from the shipped applications:
NOVA Desk by its own `--capture` mode, NOVA Write, NOVA Grid and NOVA PDF headlessly by their
screenshot examples, and NOVA Deck through its own control channel.

**Why.** A screenshot of a component that is not shipped misleads whoever reads the docs next.

---

## 13. Licence and provenance stay visible

**Decision.** Each application ships its own `LICENSE-APACHE`, `LICENSE-MIT`, `NOTICE` and
`ATTRIBUTION.md` in the bundle's `Licenses` folder. The vendored third-party crates keep their own
licences.

**Why.** The engines came from elsewhere. The licence terms require the notices to travel with
the code, and a user deserves to know what they are running.
## 14. Strings a user can see, not just the screen

**Decision.** "User-visible" covers more than window chrome: file properties, log paths, settings
paths, control-channel banners, and OOXML extension namespaces written into documents a user
saves. Each was renamed to NOVA this pass.

**What changed.**
- The OOXML extension namespace `urn:wordcraft:canvas-xfrm` — written into every `.docx` with a
  rotated canvas — is now `urn:novaworkspace:write-canvas-xfrm` (read and written by the same
  constant, so round-trips stay consistent).
- The xlsx `<fileVersion appName="..."/>` written into every workbook NOVA Grid saves is now
  `appName="NOVA Grid"` (Excel treats it the same as it did the old value; it is in the file, not
  the UI).
- The NOVA Grid control-channel stderr banner and its MCP hint say `novagrid-cli` and `NOVA Grid`
  rather than the engine's internal name.
- The OCR models' FHS install path is `share/novapdf/models` (the Linux/FreeBSD tar layouts and
  the `Models::dirs_beside_exe` search list).
- NOVA Desk's dev-machine discovery no longer embeds the upstream engine folder names in the
  shipped `novadesk.dll` constant pool; it enumerates `reference/` at runtime.
- The NOVA Write canvas namespace test (`roundtrip.rs`) was updated to match, and NOVA Grid's
  `xlsx` test asserts `appName="NOVA Grid"`.

**Why.** The first scan was case-sensitive, looked only for the CamelCase forms, and checked only
binaries against a short list of addresses. These strings were already sitting in the shipped
bytes. `Scan-NovaBundle.ps1` now matches the CamelCase brand case-sensitively, the internal
lower-case stems case-insensitively (reported, not hidden), and checks addresses in the binaries
strictly and in the text files as attribution provenance.

**Consequence.** The binaries contain no name a user could read as the old product. The internal
Rust module stems remain in the binaries and are reported by the scan rather than hidden; nothing
on screen renders them. The one boundary honestly stated: the upstream Rust package and module
names are still what the binaries were compiled from, and the scan says so.

## 15. The examples are NOVA's own, too

**Decision.** Every built-in example document — NOVA Write's Field Guide, NOVA Grid's Workshop
Budget / Store Dashboard / Register, NOVA Deck's sample deck — is original writing for this suite,
an Open Studio fictional community print shop that the four examples share.

**Why.** The shipped examples were as much a signature of the product as the chrome. The
NOVA Write sample document, the budget and sales workbooks, and the sample deck were all carrying
another product's voice and palette. Rewriting them as one fictitious "Open Studio" universe makes
the examples part of the NOVA identity and shows the same features that the old ones did.

**What changed.**
- NOVA Write: `crates/engine/src/sample.rs` now builds **The Open Studio Field Guide** — title,
  subtitle, headings, bullets, a table of shared equipment, a numbered list, a pay-what-you-can
  Membership section and a page-numbered footer.
- NOVA Grid: three samples under `crates/engine/src/sample.rs`:
  **Workshop Budget** (`budget`, the studio's monthly budget), **Studio Retail Dashboard**
  (`sales`, attendance across Mar–Sep with Apprentices / Regulars / Guests) and
  **Workshop Register** (`grades`, anonymized workshop participants with IFS grading).
- NOVA Deck: **Welcome to the Open Studio** — eight slides with a section header, an attendance
  chart, a process diagram, a pricing table, a two-column first-visit slide and a closing star.
- NOVA PDF: the showcase document was already NOVA PDF's own original content (OFL fonts,
  original prose, `example.invalid` links, `mark.novapdf.invalid` annotation markers). The xtask
  already expected `mark.novapdf.invalid`, so `showcase.html` was aligned to it — the old mismatch
  would have failed the demo build.

**Consequence.** The examples no longer rehearse another product's brand. The same texts and
numbers feed the app screenshots, the READMEs and the Start screen, so the whole first-run
experience is coherent: NOVA marks, NOVA colours, NOVA words.

---

## 16. The site is a Worker, and the download is streamed from a private release

**Decision.** The marketing site is a Cloudflare Worker with Static Assets, deployed at
`nova-workspace.jayaswinjay.workers.dev`. The 88 MB zip is a release asset in the **private**
GitHub repository `jayaswinjay-web/nova-workspace`; the `/download` route streams it to the
browser. The GitHub token is the Worker secret `GH_TOKEN` and never reaches a visitor.

**Why not a static asset.** Workers static assets cap each file at 25 MiB. The zip is 88 MB.

**Why the zip is parts.** GitHub's release-asset endpoint rejects a single 88 MB upload on
this uplink (HTTP 408, "upload body timed out"); ten 16 MB parts upload six-at-a-time in about
four minutes and can be resumed. Concatenated in name order they are byte-for-byte the original
zip, and `scripts/Upload-NovaRelease.ps1` verifies every part's size against the local file.

**Why `FixedLengthStream`.** The first Worker copied chunks through a `TransformStream`. That
costs CPU per byte: `wrangler tail` reported `exceededCpu` at ~85 MB, with 2010 ms of CPU for
42 MB. Piping the same bytes into a `FixedLengthStream` — Cloudflare's pass-through for a
response with a known length — measured **13 ms of CPU for the whole 88 MB**.

**Consequence.** One public URL, one file, no credential exposure, and a download that is
byte-identical to the built zip (verified by SHA-256). The site is anti-subscription and
anti-Microsoft in tone but factual in every claim: prices, licences, feature counts and
telemetry behaviour are the ones the suite can actually back up.

---

## 17. The product is priced, and the source stays private

**Decision.** NOVA Workspace is **closed-source commercial software**: free for 30 days, then
**₹6 per month per person**. The site leads with "Kill the corporate monopoly" — not "kill the
subscription" — because the suite now has a subscription of its own, and the honest target is the
monopoly, not the payment model. The repository stays private and the source is not published.

**What changed in the copy.** The site dropped every "free forever" and "MIT / Apache-2.0"
claim, gained a pricing section (the 30-day trial, the ₹6/month individual plan, and an explicit
"there is no enterprise plan"), and the three things being killed are now named: the corporate
monopoly, the tracking, and the idiot king. The FAQ answers what happens after the 30 days, and
the price row in the Microsoft comparison became "30 days free, then ₹6/month — ₹72 a year".

**What did not change.** The upstream engines are still MIT/Apache-2.0 and their notices still
ship in `Licenses/` — that is required and has nothing to do with how the product is sold. The
bundle's brand scan and licence attribution are unchanged.

**Consequence.** The site now promises a trial and a monthly plan that the applications do not yet
enforce: there is no licence check in the four engines or in NOVA Desk, and no payment gateway is
wired up. The contact address is a placeholder (`hello@novaworkspace.invalid`, a reserved TLD).
Both are listed under "Before launch / TODO" in `website/README.md`. Until they are built, the
pricing is a published price, not an enforced one.

---

## 18. Borrow the craft from CertKing, not the colours

**Decision.** The NOVA site adopts the small structural techniques from the user's other site,
`certking.dpdns.org` — and nothing else. "I don't want the color, I want only the small some
skills from there." So the borrowed set is layout and typography craft: the bracketed section
index with a hairline rule, hairline editorial rows, numbered circle badges, a vertical stepper,
a big-number stat band, a plan badge, a tilted outline card behind the hero artifact, a footer
trust-marker row, a skip link with visible focus rings, and a scroll-reveal stagger.

**What was deliberately not taken.** CertKing's palette (white / near-black / indigo `246 77%
60%`) stays on CertKing. NOVA keeps its violet-on-paper scheme. CertKing's stack (Next.js,
Tailwind, shadcn token system) was not taken either: the NOVA site has no build step, so each
device is written as a plain class in `public/styles.css` and the stagger is a few lines of
progressive-enhancement JavaScript in `public/app.js`.

**Why editorial rows replaced the pillar cards.** Four bordered cards read as a feature grid; the
same four rules as hairline rows with a number, a title, a body and a right-aligned tag read as a
statement. The suite is making four promises, not selling four features, and the rows say that
better.

**Consequence.** The site gained roughly 4 KB of CSS and 1 KB of JS and no new dependency. The
devices degrade cleanly: without JavaScript nothing is hidden, because the `reveal` class is only
added by the script; with `prefers-reduced-motion` the transitions are switched off entirely.

---

## 19. One thread through the suite

**Decision.** Every NOVA Workspace window wears the same signature: a three-pixel violet gradient
rail along the very top edge, and a title bar lit from above. The rail ramps from deep violet ink
`#4C1D95` through the NOVA violet `#7C3AED` at 30 %, to `#A855F7` at 65 %, and orchid `#D946EF`
at the right edge. The title bar keeps its own fill at the bottom and mixes 9 % NOVA violet into
the top edge, so the bar reads as a surface with a light above it rather than a flat strip.

**Where it lives.** One implementation per codebase, deliberately copied rather than shared: the
four egui apps each have `THREAD_H`, `THREAD_STOPS`, `mix`, `thread_color`, `thread()` and
`lit_bar()` in `crates/ui-egui/src/theme.rs`; NOVA Desk has `ThreadBrush` and
`TitleBarLightBrush` in `App.xaml`. They draw the same four stops, so a change to the ramp is four
small edits, not a cross-repo dependency. The four apps already shared the NOVA violet accent
(`#7C3AED` light, `#A78BFA` dark) and the warm-paper light theme; the thread is the first piece of
chrome that is visibly *ours* rather than merely consistent.

**Where it does not go.** Focus mode, Full Screen and a running slideshow are the document alone,
so they get no rail and no lit bar. The rail is drawn over the title bar, never inside the canvas,
so it cannot be mistaken for content or exported into a PDF or a deck.

**Why a rail and not a logo.** A wordmark in the title bar competes with the document name and
breaks on narrow windows. A 3 px rail costs nothing, survives every window width, never covers a
control, and is recognisable at a glance in a screenshot or a taskbar thumbnail. It is the
smallest mark that still reads as a family.

**Consequence.** All four apps and NOVA Desk were rebuilt and their screenshots regenerated. The
rail is painted with a 48-column mesh (two triangles per column) once per frame: the cost is
negligible next to a repaint, and it is skipped entirely when the window is a bare document view.

---

## 20. Windows 7 SP1 and Windows 8.1 are supported, not just named

**Decision.** One set of binaries runs on every supported Windows, x64, from Windows 7 SP1 through
Windows 11. Nothing in the download is chosen by the version of Windows at install time: the same
`novawrite.exe`, `novagrid.exe`, `novadeck.exe`, `novapdf.exe` and `novadesk.exe` start everywhere.

**Why the home screen moved to .NET Framework 4.8.** NOVA Desk was `net8.0-windows10.0.19041.0`
with a private .NET 8 desktop runtime in the bundle. .NET 8 does not support Windows 7 or 8.1 at
all, so that combination could never start there. .NET Framework 4.8 is the one WPF runtime that is
in the box on Windows 10 (1903 and later) and Windows 11 *and* installs on Windows 7 SP1 and 8.1.
The hub was retargeted to `net48` and the bundled runtime, the `runtime/` folder, `DOTNET_ROOT` and
the `.dll`/`.deps.json`/`.runtimeconfig.json` payload were removed. `novadesk.exe` is now a 59 KB
assembly that uses the framework the machine already has.

**Why the applications use a separate Rust target.** Rust's default Windows target builds `std`
against the Windows 10 API surface, and `std` is only the first layer: the dependency tree above it
imports entry points that simply do not exist on Windows 7. The release build therefore uses
nightly with `-Z build-std=std,panic_abort --target x86_64-win7-windows-msvc` and
`-C target-feature=+crt-static`. The static C runtime removes the Universal CRT and
`vcruntime140.dll` imports (which a stock Windows 7 does not have), and the win7 `std` uses the
pre-Windows-8 implementations of threading, time and random. The result imports only `kernel32.dll`
and `ntdll.dll` at the base layer.

**Why a local crate for the rest.** A few dependencies still declare Windows 8+ entry points as
load-time imports, and the Windows loader resolves *every* import before `main` runs. `win7-compat`
(`nova-win7-compat`) defines the handful that the tree uses — `CoIncrementMTAUsage`,
`CoCreateFreeThreadedMarshaler`, `CoTaskMemFree`, `RoGetActivationFactory`, `PdfCreateRenderer`
(the in-box Direct2D PDF renderer) and `PathCchStripPrefix` (the PathCch API set, which only NOVA
PDF's print path pulled in) — as `#[no_mangle]` functions. The linker resolves the dependency's
references to these definitions instead of writing a DLL import, so `combase.dll`, the WinRT API
set, `windows.data.pdf.dll` and `api-ms-win-core-path-l1-1-0.dll` disappear from the import table.
Each shim forwards to the real system implementation through `LoadLibraryW`/`GetProcAddress` when it
exists, so Windows 8, 8.1, 10 and 11 behave exactly as before; on Windows 7 it returns the answer
the API would have given there (`REGDB_E_CLASSNOTREG` for WinRT, `E_NOTIMPL` for the PDF print
renderer, a zero cookie for MTA accounting, and the documented prefix-stripping for PathCch —
`S_FALSE` when there is no prefix, exactly as the probe of the real DLL showed).

**Why the plain names are release-only.** A raw-dylib import object carries the stubs for a whole
DLL, so the moment a build references any *other* combase, WinRT or `windows.data.pdf` function the
linker pulls that object in and it defines these six names as well — `LNK2005` for each, then
`LNK1169`. That is exactly what happened to `cargo test` on Windows: the development graph
references more of those DLLs than the shipped release does, so the app binary would not link in
debug. The shims are therefore exported with `#[cfg_attr(not(debug_assertions), no_mangle)]`: a
release build (the only one that ships, and the only one that has to run on Windows 7) compiles the
same `#[no_mangle]` functions it always did, while a debug build keeps them mangled, links against
the real imports and runs on the build machine, which is Windows 10 or later. `install()` and the
crate's own tests still call the functions either way.

**Why OpenGL on 7 and 8.1.** wgpu's Windows backend is DirectX 12, which does not exist before
Windows 10. All four applications now enable eframe's `glow` feature as well, and
`nova_win7_compat::needs_open_gl()` — `RtlGetVersion` below 10.0 — selects `Renderer::Glow` at
start-up. Windows 10 and 11 keep wgpu and are unchanged. Enabling `glow` changes the
`eframe::App::on_exit` signature to take the GL context, which is why all three implementations of
it take an unused `_gl` parameter.

**What the audit checks.** A Windows 7 machine is not part of the build environment, so the claim
rests on the import table, not on a screenshot from a VM. `scripts/Test-NovaWin7Imports.ps1` parses
the PE import descriptors of all five shipped executables and fails on a DLL Windows 7 lacks
(`combase.dll`, `windows.data.pdf.dll`, `api-ms-win-core-path-l1-1-0.dll`, the WinRT and UCRT API
sets) or an entry point it lacks (`WaitOnAddress`, `ProcessPrng`,
`GetSystemTimePreciseAsFileTime`). It ends with `RESULT: every executable loads on Windows 7 SP1`
on the current release. The Windows 11 runtime path is exercised separately: all five windows start
from the built bundle under `scripts/Test-NovaLaunches.ps1`, which exercises the wgpu renderer, and
NOVA Desk's own `--capture` exercises the WPF path.

**Consequence.** The download no longer carries a runtime: the 154 MB zip of the previous release
became an 87.9 MB one (234 MB unpacked), and `/download` went from ten byte-range parts to six.
On Windows 7 SP1 and 8.1 the user installs .NET Framework 4.8 once through Windows Update; the
launcher checks the registry first and says so in plain words instead of letting Windows show a bare
runtime error, and the four applications still start directly from the folder without it. On Windows
10 and 11 nothing changed but the download size.

---

## 21. Tamil is bundled, and the legacy Tamil faces are not

**Decision.** NOVA Write ships Tamil typefaces. Four families are embedded in the
binary through `BUNDLED` in `crates/fonts/src/fontdb/face.rs`, the same way the Latin faces already
are, so they are present on every machine whatever the OS installed:

| Family | Files | Role |
|---|---|---|
| **TAU-Marutham** (Madhuram) | Regular, Bold, Italic | the first Tamil fallback and the first Tamil substitute, so Tamil text renders in Madhuram on every machine (licence below) |
| **Hind Madurai** | Regular, Medium, SemiBold, Bold | the default Tamil face for the font list; also carries full printable ASCII, so a paragraph mixing Tamil and English stays in one face at one size |
| **Noto Sans Tamil** | Regular, Bold | a neutral Tamil sans for the font list and for documents that name it |
| **Noto Serif Tamil** | Regular, Bold | a Tamil serif |

The Hind and Noto families are SIL Open Font License 1.1 (licence texts in
`reference/wordcraft/assets/fonts/`, rows in `reference/wordcraft/ATTRIBUTION.md`); TAU-Marutham
is CC BY-SA 4.0, and its notice travels with the bundle. Tamil characters that the document's font
lacks resolve to them through `script_fallbacks`, and a document that names a Tamil font resolves to a
Tamil face through the `TAMIL` substitute list in `crates/fonts/src/word.rs` — Windows' own
`Nirmala UI` and `Latha` first where they exist, the bundled faces last and always.

**Why Hind Madurai stays first.** It is the only bundled Tamil family that also covers printable
ASCII, so it is the only one that can hold a mixed Tamil/English run in a single face. The two Noto
families are script-only (39 of 95 ASCII codepoints: digits, punctuation, a few symbols), so a
Latin character in the same run falls back to a Latin face — exactly what Word does with them. They
are therefore offered as a choice, not as the default substitute.

**What the font has to cover.** Tamil is a complex script: the shaper reorders the vowel sign
behind its consonant, so the fallback face must carry OpenType layout tables. Hind Madurai has the
`akhn` and `abvm` features and the `taml`/`tml2` script records, and covers every Tamil vowel,
consonant, vowel sign, mark, the Tamil digits and the punctuation of the block, plus printable
ASCII — so a paragraph mixing Tamil and English stays in one face at one size. Two tests pin it:
`tamil_always_has_a_bundled_face_to_fall_back_to` (bundled faces only, no system fonts) and
`a_tamil_document_font_is_substituted_for_a_tamil_face` (the families Tamil Nadu documents name).

**Why not the four faces that were asked for, one by one.**
- **Latha** is Microsoft's, licensed with Windows. Redistributing it in a commercial product is not
  permitted, and the repository's asset policy already forbids Microsoft faces outright. A document
  that names Latha gets Nirmala UI where Windows has it, and the bundled face everywhere else.
- **Bamini** (and BAMINII, Vanavil, Avvaiyar, Kalaham, SunTommy, Vijay) are legacy Tamil fonts: they
  map Tamil letters onto Latin keyboard positions instead of the Tamil Unicode block, so they cannot
  render Unicode Tamil text at all, and their licences are not open. Bundling one would show
  nothing but the wrong letters. They stay in the substitute list by name, so an old `.docx` that
  says "BAMINII" renders as Tamil rather than as tofu.
- **Marudham / TAU-Marutham** (Tamil Virtual Academy, "Madhuram" — the face the High Court of
  Madras mandates for Tamil orders) is a genuine Unicode Tamil family and is **bundled**. The
  first pass over it concluded the opposite, on two facts: the `OS/2.fsType` of all three files is
  `8` (editable embedding — permission to embed in a document, not a grant to ship the file), and
  the embedded name-table licence record reads only `Tamilnadu Virtual Academy/ order
  no:10336/20/12/2016`, an internal order reference that says nothing to third parties. The grant
  was then found elsewhere, and it is what the bundling rests on:

  * Tamil Virtual Academy's own copyright declaration (Chennai, 18 December 2015) places **all**
    of its work under **CC BY-SA 4.0**, to be attributed on use.
  * Its TAU font statement says the TAU series fonts are "**re-distributable with installable
    embedding rights**" — which is exactly the `fsType = 8` permission the files carry — and
    "**license-free for free-use by government departments and public in all Operating Systems**".

  Read together those two are a redistribution grant, so the three faces ship unmodified as data
  files: `TAU-Marutham.ttf`, `TAU-Marutham Bold.ttf`, `TAU-Marutham Italic.ttf` (190,212 /
  196,928 / 148,632 bytes; byte-identical to the official `tamilvu.org/tkbd/tau_fonts.zip`).
  Share-alike runs with the **font file**, not with this suite, whose own code is untouched by it:
  the files travel with `Licenses/NOVA Write/TVA-TAU-Marutham.txt` (attribution, the CC BY-SA 4.0
  declaration, the TAU statement and the fsType reading) and rows in
  `reference/wordcraft/ATTRIBUTION.md`. Its family names are `TAU-Marutham`, `TAU-Marutham Bold`
  and `TAU-Marutham Italic`, all reporting subfamily `Unicode`, which `face_names()` in
  `crates/fonts/src/fontdb/names.rs` maps to `Regular` so the family appears once in the font
  list; each carries 423 glyphs (95/95 ASCII, 72/128 Tamil block, 10/10 Tamil digits). It stands
  **first** in `TAMIL_FALLBACKS` and first in the per-platform `TAMIL` substitute lists, so Tamil
  text in a non-Tamil font renders in Madhuram on every machine; `tau_marutham_ships_in_every_build`
  pins it and the Noto Serif Tamil note no longer claims the opposite.
- **Aharathi** has no identifiable Unicode Tamil family of that name; nothing was bundled under a
  name that could not be verified. If the owner has the file and its licence, the same mechanism
  takes three lines.

**The font box finds a family by any of its spellings.** Tamil names are transliterated several
ways, so a Tamil user typing the court face's everyday spelling ("Marudham") used to get an empty
list even with the font installed, because the family is listed as "TAU-Marutham". `font_matches`
in `crates/ui-egui/src/widgets/combo.rs` now expands the query through `SAME_FAMILY`
(`marudham`/`marutham`/`marutam`, `bamini`/`baminii`/`bhamini`/`baamini`,
`aharathi`/`aharati`/`aarathi`/`arathi`) before matching, so any spelling finds the family. A test
(`font_matches_finds_a_family_by_another_spelling_of_its_name`) pins it. This does not invent a
face: it only stops the name from hiding one that is installed.

**Consequence.** `novawrite.exe` carries about 1.4 MB of Tamil typefaces (1,405,739 bytes across
eleven files) and grew by 694,272 bytes in the release that added Madhuram; one of the four
applications gains a second script, and the download carries fonts the machine may not have.
Tamil text in NOVA Write now reads on a Windows installation with no Indic fonts, on macOS and on
Linux, which is the case the request was about — in Madhuram, the face Tamil courts are asked to
set their orders in.

**Typing is Tamil on every machine, not English.** Bundled fonts answer "what reads as Tamil";
typing answers "how Tamil gets in". A Windows Tamil keyboard is not installed on most machines,
and where it is, the layouts are static: probed from the machine with Tamil installed, every key
delivers its character on its own, so the key for க followed by the key for ஆ yields `கஆ`, not
`கா`. There is no composition, and on a machine with no Tamil keyboard at all the keys are Latin
and produce Latin. So **Tamil input mode is on by default** and does the composition in the
document:

* `text.tamilType` (`crates/engine/src/cmd/text/tamil.rs`) maps each typed character through the
  Bamini table (`f`=க, `h`=ா, `j`=த, `l`=ட, `;`=், `M`=ஆ … the signs and whole syllables of the
  font's own keys `T`=வு `$`=கூ `b`=டி `=`=ஸ்ரீ `@`=ளூ `~`=ஷ `H`=ர்; digits and any key the
  encoding leaves alone pass through), passes a character the keyboard already delivered as Tamil
  straight through, and applies the font's key-order rules below. There is no other composition:
  a Bamini consonant already carries its inherent vowel, and a typist reaches the sign forms
  from the sign keys, so the consonant-plus-vowel rule the Windows layouts needed was an
  artifact of static layouts, not something Bamini needs.
* The mode is a preference, `Prefs::tamil_input`, **true by default**, persisted with the rest of
  the editing preferences. Typing while it is off is ordinary Latin typing.
* Because grouped typing is one undo step for `text.insert`, `text.tamilType` joins the same
  typing group: a word of Tamil is one Undo, as a word of English is.
* Tests: `crates/engine/src/tests/tamil.rs` and the `ui.tamilInput` tests in
  `crates/ui-egui/src/tests.rs`. Bamini's table is the legacy font's own, taken from the
  owner's mapping sheets and pinned by its tests.

**Both Tamil Nadu layouts shipped, until the owner asked for Bamini and only Bamini.** The
Government's 1999 order (G.O. Ms. No. 17) approved **Tamil 99** for computers *and*, in the same
order, kept the **mechanical typewriter (Remington) layout** for typewriters — "the present
layout and sequencing of mechanical typewriter keyboard is to be maintained", with ஃ replaced
by ஹ — and the suite carried both until November 2026: both were probed from the machine with
Tamil installed, black box (`ToUnicodeEx`, one key at a time, unshifted, Shift and AltGr;
`Tamil 99` is HKL `0x00020449`, the typewriter `Tamil` is HKL `0x00000449`), and the typewriter
table kept the layout's own duplicates (ப on H and Y, க on I and K, த on L and O, ட on [ and ]),
its ligatures on the US keyboard's shifted characters (`^`=த்ர `&`=க்ஷ `*`=ஷ்ரீ `>`=ஸ்ரீ) and
its unshifted digits and punctuation Latin, as the layout delivers them. They are gone now: the
owner's request was one layout, the Bamini one, and `text.tamilType` maps it alone.

**Bamini is the one, and it is a font, not a keyboard.** Bamini is the legacy Tamil encoding a
generation of typists learned first: its glyphs sit on the Latin keys, so `f` is க, `h` is ா and the
font ligates them. The font is not redistributable, so it is not bundled: but a
*key mapping* is an input method, exactly like Tamil 99, and shipping it costs nothing — the same
keys now produce real Unicode Tamil that Madhuram renders.

The table is the owner's own, and it took two of their documents to pin. A first pass took it
from a working converter with three hundred rules and cross-checked it against an independent
encoder; the owner then supplied a four-sheet keyboard mapping of theirs (basic keys, shift keys,
vowel signs, special keys, with practice words), and the two agree **key for key** — including
`jkpo;` = தமிழ், `tzf;fk;` = வணக்கம், `mk;kh` = அம்மா, `jkpod;` = தமிழன், `,e;jpah` = இந்தியா
and `gs;sp` = பள்ளி, all pinned by a test. Two of the sheet's rows are typos (`jkpud;` for
`jkpod;`, and `gs;sp` read as பாளி).

**The font's key-order rules were the real work, and only the owner's second document showed
them.**

**`=` is the ஸ்ரீ key, and only that.** The owner pressed Shift+1 (`!`) for ஸ்ரீ and got a literal `!`, so both sources were checked: their own workbook leaves `!` literal (its ஸ்ரீ row is `=`), and the web's standard Bamini table has no `!` entry at all. They agree, and the app follows them. A complete workbook listing every printable key spelled out four rules the first table
had no way to express — the font draws them, not the key map:

* **The front signs are typed *before* their consonant.** `n` (ெ), `N` (ே) and `i` (ை) are drawn
  before the letter they belong to, while Tamil stores them after it. So a front sign is held by
  the session until the key that follows (`Session::tamil_front`), which types its own character
  first and the sign after it: `if` is கை, `kfpik` is மகிமை, `,yq;if` is இலங்கை. The sign belongs
  to the key right after it, so `nts;is` is வெள்ளை and `jPf;i\` is தீக்ஷை. Without this the sign
  lands in front of its consonant and the word reads backwards. A sign with no consonant after it
  is typed alone at the next key (as the converter emits it), so it never slides onto the next
  word's first consonant.
* **`h` and `s` complete the pair.** `h` after a front sign merges it into ொ or ோ (`nfh` = கொ),
  and `s` completes it into ௌ (`nfs` = கௌ).
* **`h` is ர before a ி, ீ or ் sign.** The font draws ர with the ா shape, so a typist presses `h`
  for either: `h` is ா normally (`fh` = கா) and ர when the next key is `p`, `P` or `;` (`fhp` =
  கரி, `fh;` = கர், `Fhp` = குரி). The ா/ூ/ொ/ோ is typed at once and corrected on the next key
  (`Session::tamil_ra`), so typing stays immediate.
* **`s` is ௌ or ள by the next key.** ௌ is ெ + a consonant + `s`, but the same key is ள, and a
  vowel sign after it proves that reading: `nfs;` is கெள், `nts;is` is வெள்ளை. The ௌ is typed at
  once and corrected when a vowel-sign key follows (`Session::tamil_au`); `s` after ே is ள, never
  ௌ.
* Any command other than `text.tamilType` ends all three waits (`Session::end_tamil_wait`), so a
  click, a format change or an undo can never leave a sign to attach to a later word.
* **`+`, `}` and `h` lengthen the sign before them.** The font draws those three keys as marks on
  the syllable beside them, so `F}` is கூ and `E}` is நூ.
* **`xs` is ஔ.** The font ligates its ள key after its ஒ key, so the composition replaces the ஒ
  rather than typing ள after it.

The number row carries the whole ூ syllables the font drew there (`#`=சூ, `$`=கூ, `%`=மூ, `^`=டூ, `&`=ரூ, `*`=ழூ), Shift+full stop is the comma (the comma key is இ in this encoding), the apostrophe keys type the Tamil quotation marks (`'`=“ `"`=” Shift+backslash=’), and `@`=ளூ, `~`=ஷ, `H`=ர் are the special keys the basic sheet never listed. It keeps
the font's other quirks: the signs sit on their own keys, and the shifted consonant keys give the
whole syllables the font drew there (`T`=வு, `=`=ஸ்ரீ, `b`=டி).

* Bamini is the only layout, so there is no layout preference any more: `Prefs::tamil_keyboard`
  and `ui.tamilKeyboard` are gone (a saved settings file that still names a `tamilKeyboard`
  value is read as its default `tamil_input` and nothing else). `ui.tamilInput` sets the mode
  (`value` true or false, no value toggling it). The status bar's chip names what is on —
  **தமிழ் பாமினி** or **EN** — and a click switches it, so English and Tamil are one control.
* Every UI test that types Latin turns the mode off in its own fixture, and says why in a
  comment; the engine pins the default directly (`tamil_input_is_on_by_default`). The mode's own
  tests leave it on. That is the price of a default that changes what the keyboard produces, and
  it is paid once per test rather than by weakening the default.
* The typing itself is proven end to end, not only in unit tests: a 69-word sweep through the real
  app path (a keyboard event through egui to `text.tamilType`) types every word correctly — the
  owner's seventeen practice words, the common words they reported broken, and the Sanskrit
  (vada mozhi) letters (`ஸ்ரீ` `ஷண்முகம்` `ஹரி` `ஜனவரி` `விஷ்ணு` `லக்ஷ்மி` `க்ஷேத்ரம்`
  `நமஸ்காரம்` `ஸ்தலம்` `புஷ்பம்` `கிருஷ்ணா` `சஷ்டி` `ஆஸ்திரேலியா` `ஸ்ரீராமன்` `ஹரிஹரன்`
  `க்ஷணம்` `தீக்ஷை` `ஜெயம்`).

---

## 22. Platform packages: one suite, six downloads

**Decision.** Every supported platform gets its own zip under the same release version, served by
`/download?platform=...`. The Windows x64 package stays the only one that includes NOVA Desk; the
other packages carry the four applications and say so in their `README.txt`.

| Package | Built with | Runs on |
|---|---|---|
| `NOVAWorkspace.zip` | `x86_64-win7-windows-msvc` + `build-std` | Windows 7 SP1 / 8.1 / 10 / 11, x64 |
| `NOVAWorkspace-win-x86.zip` | `i686-pc-windows-msvc` | Windows 10 / 11, 32-bit |
| `NOVAWorkspace-win-arm64.zip` | `aarch64-pc-windows-msvc` | Windows 11 ARM64 |
| `NOVAWorkspace-macos-intel.zip` | `x86_64-apple-darwin` | macOS, Intel |
| `NOVAWorkspace-macos-apple-silicon.zip` | `aarch64-apple-darwin` | macOS, Apple Silicon |
| `NOVAWorkspace-ubuntu.zip` | `x86_64-unknown-linux-gnu` | Ubuntu / Linux, x64 |

**Windows x86 is Windows 10/11 only.** The 32-bit PDF engine imports `combase.dll`,
`windows.data.pdf.dll` and the WinRT core API set, which do not exist on Windows 7. The Win7 shim
cannot close that gap on i686 either: a whole-DLL import object conflicts with the shim's
`no_mangle` exports (`LNK2005`/`LNK1169`), so the x86 target links without the shim. Windows 7
users stay on the x64 package, which is where Win7 support is proven.

**Every other package is built by CI.** `.github/workflows/build-and-release.yml`
clones the four NOVA snapshot repositories (`nova-wordcraft`, `nova-gridcraft`, `nova-deckcraft`,
`nova-pdfcraft`) into `reference/` and builds on GitHub-hosted runners: `windows-latest` for ARM64,
`ubuntu-latest` for Linux, `macos-15` and `macos-15-intel` for macOS. The workflow is also the
only practical macOS build path: there is no macOS SDK or Xcode on the development machine.
The macOS packages are unsigned and unnotarized, and their README says how to clear the
quarantine attribute. The Linux package is built and checked locally in a Debian container as
well, which is how the GTK3/X11 runtime dependency in its README was verified.

**The local x86 build needs NASM.** `pdfcraft` pulls `aws-lc-rs`, whose assembly build fails on
i686 without NASM on `PATH`; the CI jobs install it with `choco`/`apt`/`brew` for the same reason.
