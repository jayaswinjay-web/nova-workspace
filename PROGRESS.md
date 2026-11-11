# NOVA Workspace — build progress

The suite that ships today: **NOVA Desk** (a .NET Framework 4.8 WPF home screen) and **NOVA Write**,
**NOVA Grid**, **NOVA Deck** and **NOVA PDF** (native Rust applications), in one folder.

---

## Phase 1: the native applications

The four applications are the upstream clean-room Rust engines, re-branded. Their engines,
document models, layout, renderers, format readers and writers and command registries were kept
as they are.

- [x] NOVA Write — Word-style ribbon, paginated canvas, rulers, Backstage, sample document
- [x] NOVA Grid — ribbon, formula bar, name box, sheets, formulas, charts, conditional formatting
- [x] NOVA Deck — slide sorter, ribbon, masters and layouts, shapes, notes, transitions
- [x] NOVA PDF — viewer, tools rail, page organisation, forms, signatures, conversion
- [x] All four build in release, offline, with no errors and no warnings
- [x] Each one carries a `cargo xtask parity` check against its own feature catalog

## Phase 2: the re-brand

- [x] Every user-visible name, string, icon, window title, translation catalogue, licence notice
      and installer name now says NOVA
- [x] The settings folders are `NOVA Write`, `NOVA Grid`, `NOVA Deck`, `NOVA PDF`
- [x] The log files are `novawrite.log`, `novagrid.log`, `novadeck.log`, `novapdf.log`
- [x] The environment variables are `NOVAWRITE_*`, `NOVAGRID_*`, `NOVADECK_*`, `NOVAPDF_*`
      (223 files changed in one pass)
- [x] The command-line tools are `novawrite-cli`, `novagrid-cli`, `novadeck-cli`, `novapdf-cli`
- [x] The native deck extension is `.novadeck`, MIME `application/vnd.novaworkspace.deck+zip`;
      the old extension is gone from the source, the binaries, the docs and the installer
- [x] Internal crate, module and storage identifiers were left alone — they are never shown

## Phase 3: nothing leaves the machine

- [x] The update-checking subsystem was **removed** from all four applications: the dependency
      dropped from every manifest, the constants deleted, the start-up check and the self-update
      helper gone
- [x] Help ▸ Check for Updates still works and answers locally
- [x] The outbound URL tables in the PDF and Deck engines are empty
- [x] The contributor lists are plain labels, not links
- [x] The About boxes, the Help menus, the CLI metadata and the MCP server metadata carry no
      address
- [x] The sample document's community link became a `mailto:`
- [x] The PDF title bar's link button and its Help-menu link entries are gone

## Phase 4: NOVA Desk

- [x] A separate .NET Framework 4.8 WPF application (`novadesk.exe`), `net48`, one build-time package
      reference (the 4.8 targeting pack)
- [x] A gradient hero panel with **Open file…**
- [x] Four app cards, each with its own gradient tile, a hover lift and a coloured glow
- [x] A violet halo behind the hero, above the backdrop
- [x] A recent-documents list you can clear
- [x] Drag-and-drop and **Open file…**, routed to the right application by extension
- [x] A custom title bar with the NOVA mark and per-app accent colours
- [x] A `--capture <dir>` mode that renders the home screen to a PNG

## Phase 5: the bundle

- [x] `scripts\Build-NovaWorkspace.ps1` assembles `publish\NOVAWorkspace\` and
      `publish\NOVAWorkspace.zip`
- [x] No bundled runtime: the four applications are self-contained, NOVA Desk uses .NET Framework 4.8
      (in-box on Windows 10 1903+ / 11, one Windows Update on Windows 7 SP1 / 8.1)
- [x] The four applications are built for `x86_64-win7-windows-msvc` with `+crt-static`, so one
      binary serves Windows 7 SP1 through Windows 11
- [x] `win7-compat` (`nova-win7-compat`) supplies the Windows 8+ entry points the dependency tree
      imports, and says when to use OpenGL instead of DirectX 12
- [x] `Start NOVA Desk.vbs` launches the home screen the way a user double-clicks it (no console window; `Start NOVA Desk.cmd` kept as compatibility launcher)
- [x] `Install-NOVAWorkspace.cmd` installs per-user, with `/assoc` and `/remove`
- [x] The installer writes to `%LOCALAPPDATA%\Programs\NOVA Workspace` and creates
      `NOVA Desk.lnk` on the desktop and in the Start menu
- [x] File associations for `.docx`, `.doc`, `.rtf`, `.txt`, `.odt`, `.md`, `.xlsx`, `.xls`,
      `.csv`, `.tsv`, `.ods`, `.novadeck`, `.pptx`, `.ppt`, `.odp` and `.pdf`
- [x] The licences, notices and attribution files travel with the bundle

## Phase 6: verification

- [x] `scripts\Test-NovaLaunches.ps1` — every window starts from the built bundle and says what it
      should: `NOVA Desk`, `NOVA Write`, `Book1 — NOVA Grid`, `NOVA Deck`, `NOVA PDF`
- [x] `scripts\Scan-NovaBundle.ps1` — no other brand name and no outbound address in any shipped
      binary (ASCII and UTF-16LE) or text file
- [x] `scripts\Build-NovaWorkspace.ps1` — the bundle rebuilds from a clean tree, exit 0
- [x] The four applications build in release, offline, exit 0
- [x] NOVA Desk builds in release, exit 0

## Phase 7: documentation

- [x] `README.md`, `publish\DOWNLOAD.md`, `BUILD_STATUS.md`, `DECISIONS.md`, `FEATURES.md`,
      `FINAL_REPORT.md` and this file describe the suite that actually ships
- [x] `docs/screenshots/` holds the real windows: NOVA Desk by its `--capture` mode, NOVA Write,
      NOVA Grid and NOVA PDF by their headless screenshot examples, NOVA Deck through its own
      control channel
- [x] `scripts/Scan-NovaBundle.ps1` also checks the product-level addresses the suite must never
      offer

---

## Phase 8: NOVA's own examples and identity

- [x] NOVA Write ships **The Open Studio Field Guide** — an original community studio guide
      written for the suite (headings, lists, a table, a pay-what-you-can Membership section)
- [x] NOVA Grid ships **Workshop Budget**, **Studio Retail Dashboard** and **Workshop Register** —
      the studio's own numbers, across the year, with formulas, a totals row, conditional
      formatting and charts
- [x] NOVA Deck ships **Welcome to the Open Studio** — eight original slides with an attendance
      chart, a process diagram, a pricing table and a first-visit checklist
- [x] NOVA PDF's showcase document was already NOVA PDF's own; the xtask's marker-host constant
      (`mark.novapdf.invalid`) was aligned with the HTML (the old mismatch failed the demo build)
- [x] The app icons are a matched NOVA family — white NOVA mark on per-app NOVA gradients — for
      every app, all sizes (16…1024), plus multi-size `.ico`
- [x] The interface chrome is NOVA (warm-paper light theme, deep-ink dark theme, NOVA violet
      accents); Windows/Office greys and blues are gone
- [x] The suite wears one signature: **the NOVA thread**, a three-pixel violet gradient rail
      along the top edge of every window (`#4C1D95` → `#7C3AED` → `#A855F7` → `#D946EF`), and a
      title bar lit from above with 9 % NOVA violet. All four egui apps and NOVA Desk draw it;
      Focus, Full Screen and a running slideshow keep the document alone
- [x] The user-visible OOXML extension namespace, the xlsx `appName`, the control-channel banners,
      the OCR model folder and NOVA Desk's dev-discovery paths no longer name the upstream product

---

## Phase 9: the public site

- [x] `website/` — a static site (hand-written HTML/CSS/JS, Inter embedded, the NOVA marks and
      app icons, the current screenshots) with an anti-subscription, anti-Microsoft position and
      a comparison table against Microsoft 365
- [x] Hosted on Cloudflare Workers with Static Assets:
      **https://nova-workspace.jayaswinjay.workers.dev**
- [x] `/download` is a Worker route. The zip lives in the **private** GitHub repository
      `jayaswinjay-web/nova-workspace`; the Worker holds the token as the secret `GH_TOKEN` and
      streams the release asset to the browser with `Content-Disposition: attachment`
- [x] The 88 MB zip is published as byte-range parts, because GitHub's asset-upload
      endpoint rejects a single 88 MB request on this uplink. `scripts/Upload-NovaRelease.ps1`
      splits, uploads six at a time, resumes, and verifies every part's size
- [x] The Worker concatenates the parts into one response with `FixedLengthStream`. A plain
      `TransformStream` was killed with `exceededCpu` at ~85 MB (2010 ms CPU for 42 MB);
      `FixedLengthStream` measured **13 ms CPU for the whole 88 MB**
- [x] End-to-end verified: the download from the public URL is byte-for-byte the local zip
      (v1.0.6: SHA-256 `3DA00AC1…19ECB5D`; v1.0.5: `BA11FD98…4769D530FD`)
- [x] The site now prices the product: **30 days free, then ₹6 a month for one person**, with a
      pricing section, a nav link, a rewritten price row in the Microsoft comparison, and three
      new FAQ entries (what happens after day 30, do I need a card, why charge anything at all)
- [x] The headline changed from "kill the subscription" to "**kill the corporate monopoly**", and
      the three things being killed are named in the strip and the pillars: the corporate
      monopoly, the tracking, and the idiot king. The TVK anthem line lands in English as
      "**Even kings must answer now.**" under the hero headline and above the comparison table
- [x] Every "free forever" and "MIT / Apache-2.0" claim is gone from the site. The product is
      presented as ordinary commercial software with a private repository; the upstream licence
      notices still ship inside the bundle, because the law requires them to
- [x] The package documentation says the same thing: `README.txt` and `DOWNLOAD.md` inside the zip
      now carry the "free for 30 days, then ₹6 a month" pricing and the commercial-software note
- [x] Release `v1.0.1` carries the rebuilt zip. It was created as a **draft**, uploaded part by
      part, verified, and only then published — so `/releases/latest` never pointed at a
      half-uploaded release, and visitors always got a complete zip
- [x] `Scan-NovaBundle.ps1` now also checks for **our own** address: fatal in any binary (the
      software contains no outbound link, not even to us) and reported in the package
      documentation, where it is renewal information rather than a link the user is offered
- [x] **The editorial-device pass.** The user asked for the small UI skills from their other site
      (`certking.dpdns.org`) — the techniques only, explicitly *not* the palette. Borrowed and
      re-implemented in NOVA's own colours: the bracketed section index `[01]`–`[07]` with a
      hairline rule, hairline editorial rows (`01 / title / body / right-hand tag`) replacing the
      old boxed pillar cards, numbered hairline circle badges, a "How the 30 days work" vertical
      stepper, a big-number stat band (4 · 505/520 · 0 · ₹6), "The only plan" pill on the ₹6 card,
      a tilted outline card behind the hero screenshot, a footer trust-marker row, a skip link
      with `:focus-visible` rings, and a reveal stagger in `app.js` (siblings 70 ms apart, capped
      at 420 ms)
- [x] No framework and no dependency added for any of it: plain classes in `public/styles.css`,
      progressive enhancement in `public/app.js`. Verified at 1440 px and at mobile width with
      no horizontal overflow (`scrollWidth == clientWidth`), CSS braces balanced, every class in
      the markup resolved, and every device confirmed present in the **live** HTML/CSS/JS after
      deploy
- [x] **Release `v1.0.4`: the font box finds a family by any spelling of its name.** A Tamil user
      types "Marudham"; Windows lists the court-mandated face as "TAU-Marutham", so the box showed
      an empty list even with the font installed. `font_matches` now expands the query through
      `SAME_FAMILY` (`marudham`/`marutham`/`marutam`, `bamini`/`baminii`/`bhamini`/`baamini`,
      `aharathi`/`aharati`/`aarathi`/`arathi`), pinned by a new test. TAU-Marutham itself is still
      not bundled: its official files set `fsType = 8` (editable-embedding only) and carry no
      redistribution licence, so it is installed for the current user from the publisher instead
      (see `DECISIONS.md` section 21). Zip rebuilt at 92,243,456 bytes, `/download` verified
      byte-for-byte (SHA-256 `64F28F6B` ... `9BD753F5`), and all five executables pass the Windows 7
      import audit
- [x] **Release `v1.0.5`: three Tamil families ship inside NOVA Write.** `Hind Madurai` (already
      bundled) is joined by `Noto Sans Tamil` and `Noto Serif Tamil`, Regular and Bold, all SIL OFL
      1.1 with `fsType = 0`, so a Tamil document has a sans and a serif choice on any machine —
      including one that installed no Indic font at all. Noto Serif Tamil is the closest openly
      licensed look to the court-mandated TAU-Marutham. The four faces are embedded through
      `BUNDLED` and reachable from the `TAMIL` substitute lists; a new test
      (`the_noto_tamil_families_are_bundled_too`) pins coverage, shaping tables, real bold files and
      substitution. The two Noto faces are script-only (39 of 95 ASCII), so Hind Madurai stays first
      in the fallback order: it is the only bundled family that holds a mixed Tamil/English run in
      one face. TAU-Marutham itself is still not bundled — its own header forbids redistribution
      (`fsType = 8`, no licence grant) — and remains a per-user install.
- [x] **Release `v1.0.6`: Madhuram (TAU-Marutham) ships, and Tamil types on every machine.**
  - The court-mandated TAU-Marutham family is **bundled**, regular, bold and italic. The
    v1.0.4/v1.0.5 notes that it "cannot be bundled" stand corrected: `OS/2.fsType = 8` was read
    correctly but is only an embedding permission, and the redistribution grant was simply not
    found then. Tamil Virtual Academy's own copyright declaration (Chennai, 18 December 2015)
    puts its work under **CC BY-SA 4.0**, and its TAU statement says the series is
    "re-distributable with installable embedding rights" and "license-free for free-use by
    government departments and public in all Operating Systems". The three faces ship unmodified
    (535,772 bytes), with attribution, the declaration, the statement and the fsType reading in
    `Licenses/NOVA Write/TVA-TAU-Marutham.txt`; share-alike runs with the font file, not with the
    suite's own code. `TAU-Marutham` is first in `TAMIL_FALLBACKS` and first in the per-platform
    `TAMIL` substitute lists, so Tamil text renders in Madhuram on every machine.
  - Typing was the other half of the complaint ("it still comes as English"). `text.tamilType`
    maps the keys and composes a consonant with the vowel that follows it, on by default, on
    **both** layouts Tamil Nadu standardised — Tamil 99 and the typewriter (Remington)
    arrangement — because G.O. Ms. No. 17 (13 June 1999) approved Tamil 99 for computers *and*
    kept the typewriter layout for typewriters. Both tables were probed from this machine's
    installed layouts black box (`ToUnicodeEx`, one key at a time, unshifted/Shift/AltGr), not
    guessed; the engine keeps the typewriter's own duplicates and its ligatures (`^`=த்ர,
    `&`=க்ஷ, `*`=ஷ்ரீ), and Tamil still types on a machine with no Tamil keyboard at all.
  - The status bar's chip names what is on — தமிழ் 99, தமிழ் டைப் or EN — and a click steps to
    the next, so English, Tamil 99 and the typewriter layout are one control; `ui.tamilInput`
    and `ui.tamilKeyboard` are the commands, both saved with the editing preferences.
  - The interface faces (Inter, Source Sans) have no Tamil glyphs, so the bundled Tamil face is
    also installed into egui's font families for the chrome, the way the Georgian face already
    is: the chip draws on a machine with no Indic font.
  - One stale test fixed on the way: `crates/engine/src/sample.rs` still pinned the old
    handbook's word count (361) after the Phase 8 sample rewrite; it now pins 380.
- [x] `novawrite.exe` rebuilt for `x86_64-win7-windows-msvc` at **74,552,320 bytes**; the bundle
      and zip rebuilt (235.4 MB unpacked), all five executables still pass the Windows 7 import
      audit, the old-brand scan and the launch test, the engine, UI and fonts suites green
      (1,116 + 1,020 + 105 tests) and clippy clean.
- [x] **The typewriter table is the layout's own, checked against its source.** Reading the
      Windows Tamil layout's character table (KLID `00000449`, the file the machine loads) against
      my probe caught three punctuation keys I had swapped - `]` was printing ட instead of ஞ, the
      `'` key was unmapped instead of ட, and `?` was unmapped instead of ய. Also confirmed end to
      end through the real app: with the typewriter chip on, `l` types த and `e` composes ா after
      it (தா), `]` types ஞ, `'` types ட, `?` types ய. Fixed, retested, rebuilt and re-published.
- [x] **Bamini is the third Tamil keyboard, because Tamil Nadu types three.** The Bamini *font*
      cannot be bundled (its licence is not open) but a **key map** is an input method exactly like
      Tamil 99, so a typist who learned Bamini presses the same keys and gets real Unicode Tamil
      that Madhuram renders: `f`=க, `h`=ா, `T`=வு, `=`=ஸ்ரீ, `tzf;fk;`=வணக்கம். The table is the
      legacy font's own, taken from a working converter and cross-checked against an independent
      encoder, and it keeps the font's quirks (the signs on their own keys, whole syllables on the
      shifted consonant keys). The status bar's chip now cycles English, Tamil 99, the Tamil
      typewriter layout and Bamini. All suites green (engine 1,116 / UI 1,020 / fonts 105),
      clippy clean.
- [x] The engine and UI tests were corrected where the *tests* were wrong, not the code: Tamil 99's
      `f` is the pulli (not உ), the typewriter `bCidicd` gives வணக்கம் (the pulli sits after the
      first க), the famous Bamini word types in lower case on this table, and the shift cycle
      expects Bamini after the typewriter.
- [x] **The owner's own Bamini mapping sheet settled the table.** They sent a four-sheet
      spreadsheet (basic keys, shift keys, vowel signs, special keys, with practice words). It
      agrees with the shipped table **key for key**, including `jkpo;`=தமிழ்,
      `tzf;fk;`=வணக்கம், `mk;kh`=அம்மா, `,e;jpah`=இந்தியா and `gs;sp`=பள்ளி - all five now pinned
      by a test. Two rows of the sheet are typos (`jkpud;` for `jkpod;`, and `gs;sp` mis-scanned);
      one row needed code rather than transcription: the font ligates its `s` key after its `x`
      key, so `xs` is ப, which the composition now does by replacing the ப. All suites green
      (engine 1,116 / UI 1,020 / fonts 105), clippy clean, and the practice words re-verified
      end to end through the real app.
- [x] **The owner's "all keys" sheet finished the Bamini table, and it found three real gaps.**
      Their complete workbook lists every printable key, and four things it specifies were missing
      or wrong: the **front signs** (ெ ே ை) are typed *before* their consonant, which the font
      draws that way; `h` after a front sign merges the pair into ஒ/ோ and `s` into ிff; and `+`,
      `}` or `h` after the ஆ sign lengthens it to ிfff. All four are implemented (a front sign is
      now held by the session until the key that follows, which types its consonant first and the
      sign after it), plus three keys the basic sheet never listed (`@`=ளூ `~`=ஷ `H`=ர்). Eleven
      practice words from that sheet are pinned by a test, and the three hardest — இலங்கை,
      கொழும்பு, நூலகம் — were re-verified through the running app. All suites green (engine 1,121 /
      UI 1,020 / fonts 105), clippy clean.
- [x] Zip rebuilt at **92,715,693 bytes** (SHA-256 `3DA00AC1…19ECB5D`), the parts published as
      release **v1.0.6**, and `/download` verified byte-for-byte against the local zip:
      `Content-Length: 92715693`, `X-NOVA-Parts: 6`, identical SHA-256. All five executables still
      pass the Windows 7 import audit, the old-brand scan and the launch test.
- [x] The "everything is English" complaint was a missing keyboard, not a font.** The user's PC
      had `en-IN` and `en-US` but no Tamil keyboard, so every keystroke produced Latin text no matter
      which Tamil face was selected. Added `ta-IN` (Tamil 99 `0449:00020449`, Tamil Anjal
      `0449:00030449`) through `Set-WinUserLanguageList`; Windows loaded both layouts immediately, no
      sign-out. Verified end to end: NOVA Write stored `தமிழ் மொழி அம்மா ஸ்ரீ ௧௨௩` in a `.docx`
      and read back all 21 Tamil characters with zero `U+FFFD`.
- [x] The font box now tolerates Tamil spellings through `SAME_FAMILY` in
      `crates/ui-egui/src/widgets/combo.rs`, so "Marudham", "Marutham" and "Marutam" all find the
      same family.
- [x] Asset gate restored: 13 pre-existing brand and icon files (`assets/brand/*.svg`,
      `docs/brand/*.svg`, the app icons including the legacy `wordcraft.ico`/`.icns` names) gained
      `ATTRIBUTION.md` rows, so `cargo xtask assets` reports all 71 files attributed. Two clippy
      `-D warnings` failures (`updates_ui.rs`, `backstage.rs`) were fixed at the same time.
- [x] Zip rebuilt at **92,432,332 bytes** (SHA-256 `BA11FD98…4769D530FD`), the six parts published
      as release **v1.0.5**, and `/download` verified byte-for-byte against the local zip:
      `Content-Length: 92432332`, `X-NOVA-Parts: 6`, identical SHA-256. All five executables still
      pass the Windows 7 import audit, the old-brand scan and the launch test.
- [x] **Bamini is the only Tamil layout, because the owner asked for Bamini and only Bamini.**
      The Tamil 99 and Tamil typewriter tables were removed from the engine entirely (both
      `bamini()` peers, the consonant-plus-vowel and pulli-over-sign rules they were for,
      `Prefs::tamil_keyboard`, and the `ui.tamilKeyboard` command with its status-bar cycle),
      leaving one layout that stays on by default: the Bamini key map with the legacy font's
      key-order rules untouched. The status bar's chip became the simple English/Tamil switch
      it was before the second and third layouts arrived (தமிழ் பாமினி / EN, a click changing
      it). The engine tests were rebuilt around the layout that ships - every key of the
      table, the front-sign and combination rules, the owner's practice words, pass-through,
      one undo step - with a new one that types every mapped key and asks all four bundled
      Tamil families (Madhuram, Hind Madurai, Noto Sans Tamil, Noto Serif Tamil) to draw every
      codepoint that comes out, so "it must work in all Tamil fonts" is pinned, not assumed.
      All suites green (engine 1,113 / UI 1,019 / fonts 105), clippy clean.
- [x] Zip rebuilt at **92,765,065 bytes** (`novawrite.exe` 74,531,328), the six parts
      re-published as release **v1.0.6** with `-Force`, and `/download` verified
      byte-for-byte against the local zip: `Content-Length: 92765065`, `X-NOVA-Parts: 6`,
      SHA-256 `0D5A40F1…2B074494` on both sides. All five executables still pass the Windows 7
      import audit, the old-brand scan and the launch test.
- [x] The eleven practice words re-typed through the running app after the rebuild
      (`ui_shot` with real keystrokes, then `document.inspect`): தமிழ் வணக்கம் அம்மா தமிழன்
      இந்தியா பள்ளி கெ கொழும்பு நூலகம் இலங்கை யாழ்ப்பாணம் - all eleven correct.
- [x] **The `!` question was settled by the sources, and five shift forms came back with it.**
      The owner typed தனஸ்ரீ and got a literal `!`: they pressed
      Shift+1, which their own workbook and the web's standard Bamini table both leave literal —
      each maps ஸ்ரீ to `=` alone, which ships and works. The same check against
      the sheet and the web table restored five keys the map had lost (`#`=சூ, `%`=மூ, `^`=டூ, `&`=ரூ, `*`=ழூ) and the comma (`>`=`,`, because the comma key is இ
      in this encoding). All suites green (engine 1,115 / UI 1,019 / fonts 105), clippy clean.
- [x] Zip rebuilt at **92,765,233 bytes** for the shift-form fix, the six parts re-published as
      release **v1.0.6** with `-Force`, and `/download` verified byte-for-byte against the local
      zip: `Content-Length: 92765233`, `X-NOVA-Parts: 6`, SHA-256 `45D6548A…402A50A3` on both
      sides. All five executables still pass the Windows 7 import audit, the old-brand scan and
      the launch test, and the restored keys (`%`, `^`, `&`, `*`, `#`, `>`) were re-verified
      through the running app.

## Not shipped

| | |
|---|---|
| `src/Nova.Shell` | The earlier standalone WPF suite. It still builds and still runs its self-test, but the shipped home screen is NOVA Desk and the shipped applications are the native Rust ones. |
| `src/Nova.Core`, `src/Nova.UI.Shared`, `src/Nova.Formats`, `src/Nova.Formula`, `src/Nova.Write`, `src/Nova.Grid`, `src/Nova.Deck`, `src/Nova.Pdf` | The WinUI 3 attempt. It cannot be built here — the Windows App SDK and the NuGet packages are not reachable from this machine — and it is not part of the product. |

Both are kept in the tree because their notes and tests are worth having. Neither is in the
bundle.

## Removed

| | Why |
|---|---|
| `src/Nova.Hub.Launcher` (a Win32 `nova-hub.exe`) | Superseded by NOVA Desk, and it launched `nova-write.exe` and friends — names the suite never used. |
| `scripts/Build-NovaHub.ps1` | Built the launcher above. |
| `scripts/Build-Distribution.ps1` | Produced a second, competing download of the old WPF shell. There is one bundle now: `publish/NOVAWorkspace.zip`. |
| `vendor/craft-update` | The update checker. It was removed from all four applications, so nothing built it. |
| `publish/hub`, `publish/framework-dependent`, `publish/NOVAWorkspace-portable-x64`, `publish/NOVAWorkspace-portable-x64.zip` | Stale build outputs of the old shell. |
| `assets/nova/novahub.*` | Renamed to `novadesk.*` — the home screen is NOVA Desk, and the icon file's name is visible in Explorer and Task Manager. |
