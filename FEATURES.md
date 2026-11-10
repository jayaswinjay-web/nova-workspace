# NOVA Workspace — what it does

Four native Windows office applications and one home screen, in one folder, with no network.

---

## The suite

| App | What it is | Opens |
|---|---|---|
| **NOVA Desk** | The home screen: app grid, recents, file routing | — |
| **NOVA Write** | Word processor | `.docx` `.doc` `.dotx` `.rtf` `.txt` `.odt` `.md` `.xml` |
| **NOVA Grid** | Spreadsheet with a formula engine | `.xlsx` `.xlsm` `.xls` `.csv` `.tsv` `.ods` |
| **NOVA Deck** | Presentations | `.novadeck` `.pptx` `.pptm` `.ppt` `.odp` |
| **NOVA PDF** | PDF reader and tools | `.pdf` |

Each application is a native Rust binary with its own window, its own ribbon and its own file
associations. NOVA Desk is a .NET 8 WPF program. Nothing runs in a browser, and nothing needs an
extension, an account or a network.

---

## Breadth, measured

The engines carry a feature catalog that is compared with the live command registry, so the
numbers below are generated, not estimated.

| App | Measure | Result |
|---|---|---|
| NOVA Write | Catalog features with a command | **505 / 520 (97.1%)** |
| NOVA Grid | Excel commands implemented | **276 / 292 (94.5%)** |
| NOVA Deck | Weighted breadth parity, 191 features | **84%** |
| NOVA PDF | Acrobat features shipped | **452 shipped, 102 partial** |

Each application also ships a `cargo xtask parity` check that regenerates its checklist and fails
if a claimed feature has no command behind it.

---

## NOVA Write

A Word-style window: a ribbon over a paginated page canvas with rulers, a Backstage view, a
status bar, and a sample document that opens with the program.

- **Text**: styles, character and paragraph formatting, lists, indentation, spacing, columns,
  tables, images, fields, equations, footnotes and endnotes.
- **Tamil**: the Madhuram (TAU-Marutham) typefaces ship with the suite, so Tamil reads on a
  machine that installed no Indic font at all, and Tamil input mode types Tamil on any keyboard
  with the Bamini layout alone (its front signs typed before their consonant, as the font always
  did), the status bar switching between English and Tamil.
- **Review**: comments, tracked changes, compare, spelling and proofing.
- **References**: a table of contents, captions, cross-references, a bibliography and an index.
- **Mailings**: merge fields, envelopes, labels.
- **Files**: `.docx` written as a real OOXML package with styles, numbering, tables and pictures;
  `.rtf`, `.txt`, `.odt`, `.md` and `.xml` read; PDF export and printing.
- **Automation**: every ribbon command has an id, reachable from the command palette, the
  keyboard, `novawrite-cli`, an MCP server and a JSON control channel.

## NOVA Grid

An Excel-style window: a ribbon, a formula bar, a name box, a sheet tab strip and a status bar.

- **Formulas**: a full engine with 500-odd functions, dynamic arrays, `LET`/`LAMBDA`, error values
  and cycle detection.
- **Cells**: number formats, fonts, fills, borders, alignment, wrapping, merging, conditional
  formatting (colour scales and data bars), cell styles and themes.
- **Data**: sorts, AutoFilter, validation, tables, pivot tables, what-if analysis and a full
  recalculation pass.
- **Charts**: 17 chart types with their own design tabs.
- **Files**: `.xlsx` written with formulas and cached values and read back in full; `.csv` and
  `.tsv`; PDF export and printing.

## NOVA Deck

A PowerPoint-style window: a slide sorter with rendered thumbnails, a ribbon, a notes pane and a
slide canvas.

- **Slides**: masters and themes, 11 layouts, placeholders, section headers and duplicate slides.
- **Objects**: shapes, connectors, freeforms, text boxes, tables, charts, pictures and media, with
  align, distribute, group, z-order and smart snapping.
- **Text**: bullets, numbering, autofit, effects and a thesaurus.
- **Presentation**: transitions, animations, a full-screen slide show and a presenter view.
- **Files**: `.novadeck` — NOVA Deck's own format, a zip of the presentation plus its media, MIME
  type `application/vnd.novaworkspace.deck+zip` — and `.pptx` written with masters, layouts,
  themes, gradients, transitions and speaker notes.

## NOVA PDF

An Acrobat-style window: a menu bar, a tools rail and a page canvas.

- **Read**: a paginated viewer, thumbnails, bookmarks, search with highlighted hits, text
  selection and copy, zoom, rotation and a two-page spread.
- **Organise**: reorder, rotate, delete, insert, extract, split and merge pages.
- **Edit**: fill and sign forms, add comments, highlights, ink and stamps, redact, compress,
  optimise and protect documents.
- **Convert**: rasterise pages to images, create a PDF from images, export text, and run text
  recognition when the models are installed.
- **Automation**: a tool table reachable from `novapdf-cli`, an MCP server and a control channel.

---

## Suite-wide

- **One folder.** The bundle carries its own .NET 8 desktop runtime, so it runs on a clean
  machine. The installer is optional, per-user and reversible.
- **No network.** Nothing opens a web page or checks for an update. The About boxes, the Help
  menus and the contributor lists are plain text; only the links inside your own documents follow.
- **Every app is scriptable.** Each one exposes its commands over a CLI, an MCP server and a
  loopback JSON control channel with a per-launch token.
- **One command registry.** The ribbon, the menus, the shortcuts, the command palette, the CLI
  and the MCP server all dispatch by the same command id, so behaviour cannot drift between them.
- **No panics on hostile input.** A malformed document or a bad parameter produces an error the
  user can act on, never a crash and never lost work.
- **Open formats.** `.docx` and `.xlsx` are real OOXML packages; `.novadeck` is documented in the
  source; everything else follows its published specification.

---

## Deliberately absent

| Not there | Why |
|---|---|
| Update checker | The suite is installed as one folder; there is nothing to fetch, and the old checker reached out to an API that does not exist. |
| Any outbound link | The suite never sends the user to another site. |
| Telemetry, accounts, cloud | No data leaves the machine. |
| A browser or an extension | Everything runs as a native Windows program. |
