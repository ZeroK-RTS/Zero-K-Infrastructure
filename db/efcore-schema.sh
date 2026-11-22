#!/usr/bin/env bash
# Builds a database from the EF Core model and diffs it against the schema the EF6
# migrations produce - the measurement the whole port is steered by.
#
#     ./db/efcore-schema.sh            print the difference
#     ./db/efcore-schema.sh --check    fail if it is not the difference we expect
#     ./db/efcore-schema.sh --update   record the current difference as expected
#
# The difference is not expected to be empty. db/schema/efcore-gap.txt holds what is left
# and says why each part of it is there; --check fails when the gap changes in either
# direction, so closing a gap is as loud as opening one.
#
# The header lines are dropped before comparing: they carry the migration count and the
# name of the latest migration, which change on every migration and have nothing to say
# about whether the two models agree.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
MODE="${1:-show}"
EXPECTED=db/schema/efcore-gap.txt
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

body() { sed -n '/^tables: /,$p' "$1" | tail -n +2; }

./tools/dotnet.sh build ZkData.Core/ZkData.Core.csproj -v q --nologo >/dev/null

# Checked before anything is built or run: without it a stopped container surfaces as a
# forty-frame SqlClient stack trace in the middle of the output. Not conditional here - this
# script always builds its own zk_efcore database and never honours an outside connection string.
# --server-only because it CREATES that database a few lines down, so requiring it to exist
# already would fail every clean run.
./db/require-db.sh --server-only

ZK_CONNECTION_STRING="$(DB_NAME=zk_efcore ./db/connection-string.sh)" \
    ./tools/dotnet.sh run --project ZkData.Core --no-build -- create >/dev/null

DB_NAME=zk_efcore ./db/dump-schema.py --out "$WORK/efcore.txt" >/dev/null
body db/schema/schema.txt > "$WORK/ef6.body"
body "$WORK/efcore.txt"   > "$WORK/efcore.body"
diff "$WORK/ef6.body" "$WORK/efcore.body" > "$WORK/gap" || true

case "$MODE" in
  --update)
    { sed -n '1,/^$/p' "$EXPECTED" 2>/dev/null || true; } > "$WORK/preamble"
    [ -s "$WORK/preamble" ] || printf '# The remaining difference between the EF6 schema and the EF Core model.\n# GENERATED - run db/efcore-schema.sh --update.\n\n' > "$WORK/preamble"
    cat "$WORK/preamble" "$WORK/gap" > "$EXPECTED"
    echo "recorded $(wc -l < "$WORK/gap") lines of difference in $EXPECTED"
    ;;
  --check)
    sed -n '/^$/,$p' "$EXPECTED" | tail -n +2 > "$WORK/expected"
    if diff -q "$WORK/expected" "$WORK/gap" >/dev/null; then
      echo "the EF Core model differs from the EF6 schema exactly as recorded ($(wc -l < "$WORK/gap") lines)"
    else
      echo "the difference between the EF Core model and the EF6 schema has changed:"
      diff "$WORK/expected" "$WORK/gap" || true
      echo
      echo "if this is deliberate, re-record it with ./db/efcore-schema.sh --update"
      exit 1
    fi
    ;;
  *)
    cat "$WORK/gap"
    ;;
esac
