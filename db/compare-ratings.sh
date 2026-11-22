#!/usr/bin/env bash
# Runs the Whole History Rating pipeline twice - once on .NET Framework over EF6, once on
# .NET 9 over EF Core - against the same fixture, and compares what came out.
#
#     ./db/compare-ratings.sh
#
# This is the check §7 of the modernization plan asks for by name. Schema equality says the
# two models describe the same database; it says nothing about whether EF Core's LINQ
# translation returns the same ROWS IN THE SAME ORDER. WHR is iterative and order-sensitive,
# so a query that quietly changed shows up here as a rating that moved, and nowhere else.
#
# Both runs use the same pipeline source - ZkData/Ef/WHR/*.cs, linked into ZkData.Core
# rather than copied - so the only difference between them is the data layer underneath.
#
# Each run clears AccountRatings first and recomputes from the battles; the fixture's stored
# ratings are the live pipeline's output and would otherwise answer from cache.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
WORK="${ZK_TEST_BUILD_DIR:-$HOME/.cache/zk-dbtests}"
OUT="$(mktemp -d)"
# tools/dotnet.sh mounts only the repository, so the .NET 9 side has to write inside it.
MINE=".ratings-compare"
mkdir -p "$MINE"
trap 'rm -rf "$OUT" "$MINE"' EXIT

echo "== .NET 9 over EF Core"
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
    ./tools/dotnet.sh run --project ZkData.Core -- rate "/repo/$MINE/efcore.tsv"
cp "$MINE/efcore.tsv" "$OUT/efcore.tsv"

echo
echo "== .NET Framework over EF6"
./db/run-db-tests.sh --dump-ratings /src/ratings-ef6.tsv
cp "$WORK/ratings-ef6.tsv" "$OUT/ef6.tsv"

echo

# An empty file matches an empty file. Both exports losing their rows - a fixture that did not
# load, a filter that stopped matching - produced "identical ratings for all -1 accounts" and
# exit 0, which is the strongest claim this repository makes about the port passing on no data
# at all. The floor is simply that there are some, so no fixture change can move it.
accounts=$(( $(wc -l < "$OUT/ef6.tsv") - 1 ))
if [ "$accounts" -lt 1 ]; then
    echo "the EF6 export holds no accounts, so there is nothing to compare and any result" >&2
    echo "would agree with any other. Load the fixture with db/load-fixture.sh." >&2
    exit 2
fi

if diff -q "$OUT/ef6.tsv" "$OUT/efcore.tsv" >/dev/null; then
    echo "the two stacks produced identical ratings for all $accounts accounts."
    echo "G9 round-trips a float exactly, so identical text means identical bits."
    exit 0
fi

echo "the two stacks disagree. Largest differences by column:"
python3 - "$OUT/ef6.tsv" "$OUT/efcore.tsv" <<'PY'
import sys
def rows(path):
    with open(path) as f:
        head = f.readline().rstrip("\n").split("\t")
        return head, {r[0]: r for r in (l.rstrip("\n").split("\t") for l in f)}

head, a = rows(sys.argv[1])
_,    b = rows(sys.argv[2])

only_a, only_b = set(a) - set(b), set(b) - set(a)
if only_a: print("  only on .NET Framework:", sorted(only_a)[:10])
if only_b: print("  only on .NET 9:        ", sorted(only_b)[:10])

for col in range(1, len(head)):
    worst, who = 0.0, None
    for key in sorted(set(a) & set(b)):
        try:
            x, y = float(a[key][col]), float(b[key][col])
        except ValueError:
            if a[key][col] != b[key][col]: print("  %-13s account %s: %r vs %r" % (head[col], key, a[key][col], b[key][col]))
            continue
        if x != y and abs(x - y) > worst:
            worst, who = abs(x - y), (key, x, y)
    if who:
        print("  %-13s max delta %g on account %s (%s vs %s)" % (head[col], worst, who[0], who[1], who[2]))
    else:
        print("  %-13s identical" % head[col])
PY
exit 1
