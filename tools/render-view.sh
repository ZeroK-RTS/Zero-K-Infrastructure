#!/usr/bin/env bash
# Renders a real Zero-K view on .NET 9, with a model read from the database through EF Core.
#
#     ZK_CONNECTION_STRING=... ./tools/render-view.sh
#
# ZeroKWeb.Core answers "does it compile". This answers the question that one cannot: until
# now nothing in this repository had ever RENDERED a view - not CI, and not the Windows
# build, which compiles them but records nothing.
#
# The project can only contain the views that compile, because one that does not build
# produces no assembly to run. That set is derived here from the committed inventory rather
# than kept by hand, so it grows on its own as views are fixed.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
INVENTORY=Zero-K.info/view-port-inventory.txt
PROPS=ZeroKWeb.Render/view-set.props
trap 'rm -f "$PROPS"' EXIT

[ -f "$INVENTORY" ] || { echo "missing $INVENTORY - run tools/view-port-report.sh --update" >&2; exit 2; }

# Every view the inventory names is one that does not compile; remove exactly those.
{
    echo "<Project><ItemGroup>"
    grep -oE '[A-Za-z0-9_/.]+\.cshtml' "$INVENTORY" | sort -u \
      | sed 's|/|\\|g; s|^|    <Content Remove="..\\Zero-K.info\\Views\\|; s|$|" />|'
    echo "</ItemGroup></Project>"
} > "$PROPS"

kept=$(( $(find Zero-K.info/Views -name '*.cshtml' | wc -l) - $(grep -oE '[A-Za-z0-9_/.]+\.cshtml' "$INVENTORY" | sort -u | wc -l) ))
echo "including the $kept views the inventory says compile"

# Checked before anything is built or run: without it a stopped container surfaces as a
# forty-frame SqlClient stack trace in the middle of the output. Skipped when
# ZK_CONNECTION_STRING points somewhere else, since then the database is not ours to check.
# Run whenever the connection string points at the container this repository starts, however it
# got set. The first version skipped the check whenever ZK_CONNECTION_STRING was present, which
# meant the one invocation everybody actually uses - exporting it once per shell - was the one
# with no guard, and a stopped container still produced a forty-frame stack trace.
case "${ZK_CONNECTION_STRING:-}" in
    ""|*"127.0.0.1,14330"*|*"localhost,14330"*) DB_NAME="${DB_NAME:-zk_test}" ./db/require-db.sh ;;
esac

ZK_CONNECTION_STRING="${ZK_CONNECTION_STRING:-$(DB_NAME="${DB_NAME:-zk_test}" ./db/connection-string.sh)}" \
    ./tools/dotnet.sh run --project ZeroKWeb.Render
