#!/bin/sh
# Build the NOVA Workspace Debian package.
#
# Usage: build-deb.sh <bin-dir> <out-dir> <version> <arch> [icon.svg] [licenses-dir]
#
#   bin-dir       folder holding novawrite, novagrid, novadeck, novapdf
#   out-dir       where NOVAWorkspace-linux-<arch>.deb is written
#   version       e.g. 1.0.7
#   arch          amd64 | arm64
#
# Runs on any Debian/Ubuntu host with dpkg-deb (CI, Docker, WSL). It installs
# to /opt/nova-workspace, puts launchers in /usr/bin, adds desktop entries and
# an icon, and registers the package so `apt remove nova-workspace` cleans up.

set -eu

BIN_DIR=${1:?bin-dir required}
OUT_DIR=${2:?out-dir required}
VERSION=${3:?version required}
ARCH=${4:?arch required}
ICON=${5:-}
LICENSES=${6:-}

case "$ARCH" in
  amd64|x86_64) ARCH=amd64 ;;
  arm64|aarch64) ARCH=arm64 ;;
esac

HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

ROOT="$WORK/root"
PKG="$ROOT/opt/nova-workspace"
mkdir -p "$PKG/licenses" "$ROOT/usr/bin" "$ROOT/usr/share/applications" \
         "$ROOT/usr/share/icons/hicolor/scalable/apps" \
         "$ROOT/usr/share/doc/nova-workspace" "$ROOT/DEBIAN"

for bin in novawrite novagrid novadeck novapdf; do
  test -f "$BIN_DIR/$bin" || { echo "missing $BIN_DIR/$bin" >&2; exit 1; }
  cp "$BIN_DIR/$bin" "$PKG/$bin"
  chmod 0755 "$PKG/$bin"
  ln -s "/opt/nova-workspace/$bin" "$ROOT/usr/bin/$bin"
done

if [ -n "$LICENSES" ] && [ -d "$LICENSES" ]; then
  cp -R "$LICENSES/." "$PKG/licenses/"
fi

if [ -n "$ICON" ] && [ -f "$ICON" ]; then
  cp "$ICON" "$ROOT/usr/share/icons/hicolor/scalable/apps/nova-workspace.svg"
else
  cp "$HERE/../windows/../../assets/brand/nova-mark.svg" \
     "$ROOT/usr/share/icons/hicolor/scalable/apps/nova-workspace.svg" 2>/dev/null || true
fi

cat > "$PKG/README.txt" <<EOF
NOVA Workspace $VERSION (Linux $ARCH)
=====================================

This package installs NOVA Write, NOVA Grid, NOVA Deck and NOVA PDF.

  novawrite [file]    word processor   .docx .doc .rtf .txt .odt .md
  novagrid  [file]    spreadsheet      .xlsx .xls .csv .tsv
  novadeck  [file]    presentations    .novadeck .pptx .ppt
  novapdf   [file]    PDF viewer       .pdf

They are also in the application menu (Office) and open supported files from
the file manager.

Requirements: a desktop session with OpenGL and the usual GTK3/X11 runtime
libraries (the package pulls them in as dependencies).

NOVA Desk, the home screen on Windows, is not part of the Linux package.

Free for 30 days, then 6 rupees a month for one person.
https://nova-workspace.jayaswinjay.workers.dev
EOF

cat > "$ROOT/usr/share/doc/nova-workspace/copyright" <<EOF
NOVA Workspace is commercial software.
Copyright 2026 NOVA Workspace Team.

The third-party components keep their own licences; the full licence and
attribution files are under /opt/nova-workspace/licenses/.
EOF

# One launcher per application.
write_desktop() {
  name=$1; bin=$2; generic=$3; mimes=$4; categories=$5
  cat > "$ROOT/usr/share/applications/nova-$bin.desktop" <<EOF
[Desktop Entry]
Type=Application
Version=1.0
Name=$name
GenericName=$generic
Comment=Part of NOVA Workspace
Exec=$bin %F
Icon=nova-workspace
Terminal=false
Categories=$categories
MimeType=$mimes
StartupNotify=true
EOF
}

write_desktop "NOVA Write" novawrite "Word Processor" \
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document;application/msword;application/rtf;text/plain;application/vnd.oasis.opendocument.text;text/markdown;" \
  "Office;WordProcessor;"
write_desktop "NOVA Grid" novagrid "Spreadsheet" \
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;application/vnd.ms-excel;text/csv;text/tab-separated-values;" \
  "Office;Spreadsheet;"
write_desktop "NOVA Deck" novadeck "Presentation" \
  "application/vnd.openxmlformats-officedocument.presentationml.presentation;application/vnd.ms-powerpoint;application/x-novadeck;" \
  "Office;Presentation;"
write_desktop "NOVA PDF" novapdf "PDF Viewer" \
  "application/pdf;" \
  "Office;Viewer;"

INSTALLED_KB=$(du -sk "$ROOT" | cut -f1)

cat > "$ROOT/DEBIAN/control" <<EOF
Package: nova-workspace
Version: $VERSION
Section: office
Priority: optional
Architecture: $ARCH
Installed-Size: $INSTALLED_KB
Maintainer: NOVA Workspace Team <hello@novaworkspace.invalid>
Homepage: https://nova-workspace.jayaswinjay.workers.dev
Depends: libc6, libgtk-3-0, libxkbcommon0, libxkbcommon-x11-0, libwayland-client0, libx11-6, libxcb1, libgl1, libegl1, libfontconfig1, libfreetype6, libudev1, libasound2 | libasound2t64
Recommends: fonts-noto-core
Description: NOVA Workspace - native office suite
 NOVA Write, NOVA Grid, NOVA Deck and NOVA PDF in one native suite.
 Opens Word, Excel, PowerPoint, OpenDocument and PDF files offline.
 .
 No account, no tracking, no subscription service. Free for 30 days,
 then 6 rupees a month for one person.
EOF

cat > "$ROOT/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database -q /usr/share/applications || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -q -t /usr/share/icons/hicolor || true
fi
exit 0
EOF
cp "$ROOT/DEBIAN/postinst" "$ROOT/DEBIAN/postrm"
chmod 0755 "$ROOT/DEBIAN/postinst" "$ROOT/DEBIAN/postrm"

mkdir -p "$OUT_DIR"
OUT="$OUT_DIR/NOVAWorkspace-linux-$ARCH.deb"
dpkg-deb --build --root-owner-group "$ROOT" "$OUT" >/dev/null
echo "$OUT"
