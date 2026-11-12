#!/bin/sh
# Build the NOVA Workspace macOS packages (.pkg installer and .dmg).
#
# Usage: build-macos.sh <bin-dir> <out-dir> <version> <label> <icon-dir>
#
#   bin-dir    folder holding novawrite, novagrid, novadeck, novapdf
#   out-dir    where the .pkg and .dmg are written
#   version    e.g. 1.0.7
#   label      intel | apple-silicon
#   icon-dir   folder with novawrite-1024.png … novapdf-1024.png
#
# Must run on macOS (sips, iconutil, pkgbuild, hdiutil, codesign). The GitHub
# macOS runners have all of them; there is no macOS SDK on the Windows build
# machine, which is why CI produces these.
#
# The .pkg installs the four applications into /Applications/NOVA Workspace
# and clears the quarantine flag on them, so no per-app right-click is needed.
# The .dmg is the drag-and-drop alternative.

set -eu

BIN_DIR=${1:?bin-dir required}
OUT_DIR=${2:?out-dir required}
VERSION=${3:?version required}
LABEL=${4:?label required}
ICON_DIR=${5:-}

HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

APPS="$WORK/root/Applications/NOVA Workspace"
SCRIPTS="$WORK/scripts"
mkdir -p "$APPS" "$SCRIPTS" "$OUT_DIR"

make_icns() {
  png=$1
  icns=$2
  set=$WORK/icon.iconset
  rm -rf "$set"
  mkdir -p "$set"
  for s in 16 32 128 256 512; do
    /usr/bin/sips -z "$s" "$s" "$png" --out "$set/icon_${s}x${s}.png" >/dev/null
    /usr/bin/sips -z $((s * 2)) $((s * 2)) "$png" --out "$set/icon_${s}x${s}@2x.png" >/dev/null
  done
  /usr/bin/iconutil -c icns "$set" -o "$icns"
}

make_app() {
  bin=$1
  display=$2
  ident=$3
  app="$APPS/$display.app"
  mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
  cp "$BIN_DIR/$bin" "$app/Contents/MacOS/$display"
  chmod 0755 "$app/Contents/MacOS/$display"
  if [ -n "$ICON_DIR" ] && [ -f "$ICON_DIR/$bin-1024.png" ]; then
    make_icns "$ICON_DIR/$bin-1024.png" "$app/Contents/Resources/AppIcon.icns"
  fi
  cat > "$app/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key><string>en</string>
  <key>CFBundleExecutable</key><string>$display</string>
  <key>CFBundleIconFile</key><string>AppIcon</string>
  <key>CFBundleIdentifier</key><string>$ident</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>$display</string>
  <key>CFBundleDisplayName</key><string>$display</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>LSMinimumSystemVersion</key><string>10.15</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.productivity</string>
</dict>
</plist>
EOF
  # Ad-hoc signature: lets macOS treat the bundle as a single sealed app even
  # though the build has no Developer ID certificate.
  /usr/bin/codesign --force --deep --sign - "$app" >/dev/null 2>&1 || true
}

make_app novawrite "NOVA Write" "in.novaworkspace.write"
make_app novagrid "NOVA Grid" "in.novaworkspace.grid"
make_app novadeck "NOVA Deck" "in.novaworkspace.deck"
make_app novapdf "NOVA PDF" "in.novaworkspace.pdf"

cat > "$APPS/README.txt" <<EOF
NOVA Workspace $VERSION ($LABEL)
================================

NOVA Write, NOVA Grid, NOVA Deck and NOVA PDF.

This build is signed ad-hoc, not notarized by Apple, so the first time you open
one of the applications macOS may say it cannot check it for malicious software.
Do one of these once:

  * right-click (or Control-click) the application and choose Open, then Open
    again, or
  * run this in Terminal after copying the folder to Applications:
        xattr -dr com.apple.quarantine "/Applications/NOVA Workspace"

Then the applications open normally from Finder, Launchpad or Spotlight.

NOVA Desk, the home screen on Windows, is not part of the macOS package.
Free for 30 days, then 6 rupees a month for one person.
https://nova-workspace.jayaswinjay.workers.dev
EOF

cat > "$SCRIPTS/postinstall" <<'EOF'
#!/bin/sh
# Installed by the NOVA Workspace package.
/usr/bin/xattr -dr com.apple.quarantine "/Applications/NOVA Workspace" 2>/dev/null || true
exit 0
EOF
chmod 0755 "$SCRIPTS/postinstall"

PKG="$OUT_DIR/NOVAWorkspace-macos-$LABEL.pkg"
/usr/bin/pkgbuild --root "$WORK/root" \
  --identifier "in.novaworkspace.suite" \
  --version "$VERSION" \
  --install-location "/" \
  --scripts "$SCRIPTS" \
  "$PKG" >/dev/null

# Drag-and-drop image: "NOVA Workspace" folder plus an Applications alias.
DMG_ROOT="$WORK/dmg"
mkdir -p "$DMG_ROOT"
cp -R "$APPS" "$DMG_ROOT/NOVA Workspace"
ln -s /Applications "$DMG_ROOT/Applications"
cp "$APPS/README.txt" "$DMG_ROOT/README.txt"

DMG="$OUT_DIR/NOVAWorkspace-macos-$LABEL.dmg"
rm -f "$DMG"
/usr/bin/hdiutil create -volname "NOVA Workspace" -srcfolder "$DMG_ROOT" \
  -ov -format UDZO "$DMG" >/dev/null

echo "$PKG"
echo "$DMG"
