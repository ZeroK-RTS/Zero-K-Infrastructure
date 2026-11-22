#!/usr/bin/env bash
# Downloads a Spring engine so the unitsync P/Invokes can be called for real.
#
#     ./tools/fetch-engine.sh              # prints the directory to use
#     ZK_DOTNET_MOUNT=$(./tools/fetch-engine.sh) ./tools/dotnet.sh test Tests.Portable/...
#
# The engine is NOT downloaded into the repository, and that is not tidiness: this checkout is
# synced by Synology Drive, which replicates anything in the tree and restores it after deletion.
# It goes under the user cache instead, and is kept once fetched.
#
# Same URL the product itself uses - EngineDownload.cs builds
# {BaseSiteUrl}/engine/{platform}/{name}.zip - so this is the ordinary distribution channel, not a
# scrape. linux64 because that is what SpringPaths picks on a 64-bit Unix.
set -euo pipefail

VERSION="${ZK_ENGINE_VERSION:-105.1.1-2457-g8095d30}"
DEST="${ZK_ENGINE_DIR:-$HOME/.cache/zk-engine/$VERSION}"
URL="https://zero-k.info/engine/linux64/$VERSION.zip"

if [ ! -f "$DEST/libunitsync.so" ]; then
    echo "fetching $VERSION (~42MB) from $URL" >&2
    mkdir -p "$DEST"
    tmp="$(mktemp -d)"
    trap 'rm -rf "$tmp"' EXIT
    curl -fsSL --max-time 600 -o "$tmp/engine.zip" "$URL"
    unzip -o -q "$tmp/engine.zip" -d "$DEST"
    [ -f "$DEST/libunitsync.so" ] || { echo "no libunitsync.so in $VERSION - wrong platform or layout" >&2; exit 1; }

    # Deliberately NOT chmod +x. The archive gives its binaries mode 0664 - spring-dedicated will
    # not run as extracted - and that is exactly what EngineDownload.FixPermissions exists to fix.
    # Leaving it faithful is what lets Tests.Portable/UnixPermissionsTests show the difference
    # against a real engine rather than a temp file. To run one by hand: chmod +x "$DEST/spring*".
    echo "unpacked into $DEST" >&2
fi

# A map too, because the calls that still need System.Drawing.Common - GetMinimap, GetHeightMap,
# GetMetalMap - do nothing without one. The smallest on springfiles (333KB), fetched from the same
# place AutoRegistrator's WebFolderSync uses. maps/ is where unitsync looks inside a data
# directory.
MAP_FILE="${ZK_ENGINE_MAP:-bluescreen_fields_v2.sd7}"
if [ ! -f "$DEST/maps/$MAP_FILE" ]; then
    echo "fetching map $MAP_FILE (~333KB)" >&2
    mkdir -p "$DEST/maps"
    curl -fsSL --max-time 300 -o "$DEST/maps/$MAP_FILE" \
        "https://springfiles.springrts.com/files/maps/$MAP_FILE"
fi

echo "$DEST"
