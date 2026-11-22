#!/usr/bin/env bash
# Serves a real Zero-K view over HTTP on .NET 9 and checks the response.
#
#     ./tools/run-host.sh            request it once and check
#     ./tools/run-host.sh --serve    leave it running on http://127.0.0.1:5199
#
# --serve BUILDS FIRST. Nothing is listening until it prints "serving on ..."; a browser opened
# before that reports ERR_CONNECTION_REFUSED, which is the build still running, not a failure.
#
# Only the views the inventory says compile are included - a project that does not build
# serves nothing - so the set is generated here from the committed inventory.
#
# `child-action` views are the exception and are KEPT. They compile; what they cannot do is
# render the Html.Action call itself, and whether that call is reached depends on the request.
# TopMenu.cshtml is the case that matters: it guards its child action with
# Global.IsAccountAuthorized, so an anonymous request renders it in full, which is what the
# "TopMenu rendered inside the layout" check below proves. Dropping it because the inventory
# names it would delete the only end-to-end evidence that the site layout works, to avoid a
# failure that does not happen. That the shims throw when they ARE reached is checked in
# ZeroKWeb.Render instead.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
INVENTORY=Zero-K.info/view-port-inventory.txt
PROPS=ZeroKWeb.Host/view-set.props
trap 'rm -f "$PROPS"' EXIT

[ -f "$INVENTORY" ] || { echo "missing $INVENTORY - run tools/view-port-report.sh --update" >&2; exit 2; }

./tools/view-set.sh "$PROPS"

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

# `dotnet run` builds first, and building means the Razor generator compiling every view in the
# set - a minute or two, during which the only output is MSBuild warnings. With --serve that
# looks exactly like a server that has started and is ignoring you, so say what is happening.
# Reported as ERR_CONNECTION_REFUSED by someone who opened the browser before it was ready.
case " $* " in
    *" --serve "*)
        echo "building first - this takes a minute or two; the URL appears when it is ready" >&2
        ;;
esac

ZK_CONNECTION_STRING="${ZK_CONNECTION_STRING:-$(DB_NAME="${DB_NAME:-zk_test}" ./db/connection-string.sh)}" \
    ./tools/dotnet.sh run --project ZeroKWeb.Host -- "$@"
