# NOVA Workspace

NOVA Write, NOVA Grid, NOVA Deck and NOVA PDF — one native Windows office suite,
launched from **NOVA Desk**.

This repository hosts the release artifacts for the NOVA Workspace download.
The marketing site streams the suite zip from the latest release here.

## Download

Use the public site. This repository is private and its assets are served through
a Cloudflare Worker that holds a server-side token; no credential is ever sent to
a visitor's browser.

## Release layout

`NOVAWorkspace.zip` is published as byte-range parts:

    NOVAWorkspace.zip.part01
    NOVAWorkspace.zip.part02
    ...
    NOVAWorkspace.zip.partNN

Concatenated in name order, the parts are byte-for-byte the original zip.
The download Worker streams them back as one file named `NOVAWorkspace.zip`.

Splitting is required because GitHub's release-asset upload endpoint rejects a
single 154 MB request on slow uplinks.

## Licence

NOVA Workspace is free software under the MIT and Apache-2.0 licences.
