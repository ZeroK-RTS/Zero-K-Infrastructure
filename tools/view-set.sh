#!/usr/bin/env bash
# Writes ZeroKWeb.Host/view-set.props: the list of Zero-K.info views the Host must NOT compile,
# because PortedViews/ has its own copy of them.
#
#     ./tools/view-set.sh [output path]
#
# Extracted from run-host.sh so the Dockerfile can generate the same file the same way. Two
# implementations of this drifting apart would show up as a duplicate-view build error at best,
# and as the wrong copy of a view being served at worst.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
INVENTORY=Zero-K.info/view-port-inventory.txt
PROPS="${1:-ZeroKWeb.Host/view-set.props}"

[ -f "$INVENTORY" ] || { echo "missing $INVENTORY - run tools/view-port-report.sh --update" >&2; exit 2; }

{
    echo "<Project><ItemGroup>"
    grep -v '^child-action' "$INVENTORY" | grep -oE '[A-Za-z0-9_/.]+\.cshtml' | sort -u \
      | sed 's|/|\\|g; s|^|    <Content Remove="..\\Zero-K.info\\Views\\|; s|$|" />|'
    echo "</ItemGroup></Project>"
} > "$PROPS"
