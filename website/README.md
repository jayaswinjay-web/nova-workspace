# NOVA Workspace — website

Public marketing site for NOVA Workspace, deployed to **Cloudflare Workers with
Static Assets**.

Live at **https://nova-workspace.jayaswinjay.workers.dev**

## Why the download is not a static asset

Workers static assets cap each file at **25 MiB**. The suite zip is **88 MB**, so
`/download` is a Worker route instead.

The zip lives in the **private** GitHub repository
`jayaswinjay-web/nova-workspace` as release assets. GitHub's release-asset upload
endpoint rejects a single large request on a slow uplink, so the zip is published
as byte-range parts:

```
NOVAWorkspace.zip.part01 … NOVAWorkspace.zip.part06
```

Six 16 MB parts cover the 88 MB zip. The Worker discovers the parts from the
release's asset list (`X-NOVA-Parts` reports how many it found), so the part count
is never hard-coded: a bigger or smaller zip is the same script and the same
route.

Concatenated in name order they are byte-for-byte the original zip. The Worker
streams them back as one response named `NOVAWorkspace.zip`, with the exact
`Content-Length` of the original file.

The GitHub token is a Worker secret (`GH_TOKEN`) and never reaches the browser.

## Streaming notes

The first implementation copied chunks with a `TransformStream`. That burns CPU
per byte and the isolate was killed with `exceededCpu` at ~85 MB. The current
implementation pipes into a **`FixedLengthStream`**, Cloudflare's optimised
pass-through for responses with a known length. Measured with `wrangler tail`:
**13 ms CPU for the full 88 MB** (versus 2010 ms for 42 MB before).

`wrangler tail` is the tool that found this; keep it handy:

```powershell
wrangler tail nova-workspace --format json
```

## Before launch / TODO

- The contact address in the pricing section and the FAQ is the placeholder
  `hello@novaworkspace.invalid` (`.invalid` is a reserved TLD, so it can never
  resolve). Replace it with a real inbox before telling anyone to "write to us
  and the ₹6 is waived" — the copy invites people to email, so it has to work.
  It appears in `public/index.html` twice.
- The suite itself contains no payment or licence-enforcement code. The 30-day
  trial and the ₹6/month plan are what the site *says*; the enforcement has to
  be built into the applications before the site can make that promise for real.

## The editorial devices

The page borrows a set of small structural techniques from a sibling site
(`certking.dpdns.org`) — **the craft only, never the palette.** CertKing is a
white/indigo Next.js + Tailwind site; NOVA keeps its own violet-on-paper
colours and re-implements each device in plain CSS:

| Device | Where | What it does |
|---|---|---|
| Bracketed section index | `.kicker .idx` | `[04] ──── The confrontation` before every section title |
| Editorial rows | `.editorial-row` | `01 / title / body / right-aligned tag`, hairline-separated, hover tint — replaced the old boxed pillar cards |
| Numbered circle badge | `.circle-num` | hairline circle with a mono number, used by the stepper |
| Vertical stepper | `.stepper` + `.how` | "How the 30 days work", three steps |
| Big-number stat band | `.stat-band` / `.stat-num` | 4 · 505/520 · 0 · ₹6, hairline-divided |
| Tilted card | `.hero-stack::before` | a rotated outline card behind the hero screenshot |
| Plan badge | `.badge-plan` | "The only plan" pill on the ₹6 card |
| Trust-marker row | `.marker-row` | dot-led facts centred at the foot of the page |
| Skip link + `:focus-visible` | `.skip-link` | keyboard access, `#main` target |
| Reveal stagger | `app.js` | siblings in a group fade in 70 ms apart (capped at 420 ms) |

There is no build step and no Tailwind: the devices are class-based CSS in
`public/styles.css`, and the stagger writes `transition-delay` inline at load so
the order never depends on which observer fires first.

## Layout

```
website/
  wrangler.toml        Worker + static assets config
  src/index.js         /download proxy (GitHub release -> browser)
  public/
    index.html         the site
    404.html
    styles.css         NOVA palette + the editorial devices
    app.js             download feedback + scroll reveals and stagger
    robots.txt
    sitemap.xml
    _headers
    assets/            NOVA marks, app icons, screenshots, Inter fonts
```

## Deploy

```powershell
cd D:\wrkspasce\website
wrangler deploy
```

First-time setup only:

```powershell
wrangler secret put GH_TOKEN     # paste a token that can read the private repo
```

`GITHUB_REPO` is a plain var in `wrangler.toml`.

## Local preview

```powershell
cd D:\wrkspasce\website
wrangler dev
```

Static-only preview (no `/download`):

```powershell
cd D:\wrkspasce\website\public
python -m http.server 8931
```

## Publishing a new build

```powershell
# 1. rebuild the bundle
.\scripts\Build-NovaWorkspace.ps1

# 2. upload the zip as byte-range parts to a NEW tag (resumable; skips parts
#    already there; creates the release as a draft and publishes it only after
#    every part is uploaded and verified)
.\scripts\Upload-NovaRelease.ps1 -Tag v1.0.5 -PartMB 16 -Parallel 6

# 3. nothing else - the site reads /releases/latest, so the new tag is live
```

The Worker always reads `/releases/latest`, so a new release tag needs no site
redeploy. The draft-then-publish order matters: while a release is a draft it
is invisible to `/releases/latest`, so visitors keep receiving the complete
previous zip instead of a half-uploaded one.

If an upload fails with `HTTP 408: Upload body timed out due to inactivity`,
just run the script again — it skips the parts that are already there and
retries only the missing ones.

## Verify a deployment

```powershell
curl.exe -sS --ssl-no-revoke -o verify.zip https://nova-workspace.jayaswinjay.workers.dev/download
Get-FileHash verify.zip -Algorithm SHA256
Get-FileHash D:\wrkspasce\publish\NOVAWorkspace.zip -Algorithm SHA256
```

The two hashes must match. They did on 2026-11-11 for release v1.0.6
(`45D6548A…402A50A3`, `Content-Length: 92765233`, `X-NOVA-Parts: 6`; the same check for v1.0.5
gave `BA11FD98…4769D530FD`).
