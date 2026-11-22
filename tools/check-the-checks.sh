#!/usr/bin/env bash
# Every check in tools/ fails when it can see nothing.
#
#     ./tools/check-the-checks.sh
#
# A check that examines an empty list reports success. That is not a hypothetical: moving CI
# onto tools/mono-buildable.proj pointed the item guard in tools/build-website.sh at a file
# naming no items, so it went from 314 items to 0 and stayed green for a commit. Nothing failed,
# which is precisely the problem - a guard that has stopped guarding looks exactly like a guard
# with nothing to report.
#
# Most checks here find their subjects with `git ls-files <pattern>`. A directory rename, a
# project move, or a glob that stops matching empties that list without any error. So this runs
# each one with a `git` on PATH that returns nothing for `ls-files` and forwards everything else
# to the real git, and requires a non-zero exit. A traceback does not count: a crash is not a
# diagnosis, and the message is what tells the next person what actually broke.
#
# A check that does not find its subjects through git ls-files says so in itself:
#
#     blinding-exempt: <why blinding it proves nothing>
#
# which is the same shape as `// not-in-a-project:` in check-orphaned-sources.py.
#
# It also answers a second question about the same subject: **is each check actually RUN?** A
# check nobody invokes is the quietest version of a check that sees nothing - it reports success
# by never reporting at all, and it looks exactly like a check that is working. Five were added in
# one day and each had to have its workflow step remembered by hand, which is not a system. So
# every tools/check-* must be invoked by some file under .github/workflows/, and a mention in a
# comment does not count: comments in those workflows name check paths to explain what a step is
# for, so matching the path anywhere would have passed on a step that had been deleted.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SHIM="$(mktemp -d)"
trap 'rm -rf "$SHIM"' EXIT

cat > "$SHIM/git" <<'SHIMEOF'
#!/usr/bin/env bash
for argument in "$@"; do [ "$argument" = "ls-files" ] && exit 0; done
exec git "$@"
SHIMEOF
chmod +x "$SHIM/git"

blinded=0
exempt=0
failures=0

for check in $(git -C "$REPO" ls-files 'tools/check-*'); do
    [ "$check" = "tools/check-the-checks.sh" ] && continue

    reason="$(grep -oP 'blinding-exempt:\s*\K.*' "$REPO/$check" | head -1)"
    if [ -n "$reason" ]; then
        printf '   exempt  %-30s %s\n' "$(basename "$check")" "$reason"
        exempt=$((exempt + 1))
        continue
    fi

    case "$check" in
        *.py) run=(python3 "$REPO/$check") ;;
        *.js) run=(node "$REPO/$check") ;;
        *)    run=("$REPO/$check") ;;
    esac

    output="$(cd "$REPO" && PATH="$SHIM:$PATH" "${run[@]}" 2>&1)"
    status=$?

    if [ "$status" -eq 0 ]; then
        printf '   FAIL    %-30s passed while seeing nothing\n' "$(basename "$check")"
        failures=$((failures + 1))
    elif grep -q 'Traceback (most recent call last)' <<<"$output"; then
        printf '   FAIL    %-30s crashed instead of reporting it\n' "$(basename "$check")"
        failures=$((failures + 1))
    else
        printf '   ok      %-30s %s\n' "$(basename "$check")" "$(tail -1 <<<"$output" | cut -c1-70)"
        blinded=$((blinded + 1))
    fi
done

echo

# Is each check run by CI at all? Comments are stripped first - `#` to end of line, which is the
# whole of YAML's comment syntax - so a step that explains itself by naming a script does not
# stand in for a step that invokes one.
unwired=0
WORKFLOWS="$(cat "$REPO"/.github/workflows/*.yml 2>/dev/null | sed 's/#.*//')"
if [ -z "$WORKFLOWS" ]; then
    echo "found no workflows to read - every check would look unwired" >&2
    exit 2
fi
for check in $(git -C "$REPO" ls-files 'tools/check-*'); do
    case "$WORKFLOWS" in
        *"$check"*) ;;
        *)
            printf '   FAIL    %-30s is in the repository and nothing in CI runs it\n' \
                "$(basename "$check")"
            unwired=$((unwired + 1))
            ;;
    esac
done

if [ "$unwired" -ne 0 ]; then
    echo >&2
    echo "$unwired check(s) exist and never run." >&2
    echo "Add a step to a workflow, or delete the check - an uninvoked check is worse than" >&2
    echo "no check, because it reads as one." >&2
    exit 1
fi

# This script is a check, so it answers to its own rule.
if [ "$((blinded + exempt))" -eq 0 ]; then
    echo "found no checks to blind - this check would pass by seeing nothing" >&2
    exit 2
fi

if [ "$failures" -ne 0 ]; then
    echo "$failures check(s) do not fail usefully when they can see nothing." >&2
    echo "Give it a guard that exits non-zero when its subject list is empty, or write" >&2
    echo "    blinding-exempt: <why blinding it proves nothing>" >&2
    echo "in the file if it does not find its subjects through git ls-files." >&2
    exit 1
fi

echo "$blinded check(s) fail when blinded, $exempt exempt, and all $(git -C "$REPO" ls-files 'tools/check-*' | wc -l | tr -d ' ') are run by CI"
