#!/usr/bin/env bash
# Which Razor views actually RENDER when the host harness runs?
#
#     ./tools/rendered-views.sh            the count, and what never renders
#
# 122 views compile, and tools/view-port-report.sh checks that. This asks the other question, and
# they are not the same one: News/Index.cshtml compiled for weeks and threw on its first request.
#
# The host records every view Razor executes when ZK_RENDERED_VIEWS names a file - see
# RenderedViewRecorder in ZeroKWeb.Host/Program.cs. This runs the harness with it set and diffs the
# result against the tracked .cshtml files.
#
# It measures the harness, which runs with NO lobby server attached - deliberately, because the
# site has to render without one. A view whose ACTION asks Global.LobbyApi before choosing one
# cannot be reached that way at all, and would read here as uncovered when it is simply covered
# somewhere else. Measured, two are like that: Planetwars/PwMatchMaker and Tourney/TourneyIndex.
#
# So tools/stack.sh now records what IT rendered, and the list below is annotated from that file
# when it is there. The three Lobby/LobbyChat views are NOT in it - having a lobby server is not
# enough for those - which is the sort of thing a hardcoded "these are fine" list would have got
# wrong in the flattering direction.
#
# **A view in the "never rendered" list is not broken.** Most render perfectly well when something
# asks for them; /Home, /Forum and /Clans did, which is how they came to be in the harness at all.
# The list is a coverage measurement, not a defect report.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

LOG=rendered-views.tmp          # under the repo because that is what the container can see
trap 'rm -f "$LOG"' EXIT
rm -f "$LOG"

ZK_RENDERED_VIEWS="/repo/$LOG" ./tools/run-host.sh >/dev/null 2>&1 || {
    echo "the harness did not finish - run ./tools/run-host.sh to see why" >&2; exit 1; }

# The SAME exclusion on both sides, which it did not used to have. _ViewStart.cshtml runs on
# every request that is not a child action, so it was always in the rendered list - and it is
# filtered out of the tracked list below, because it is not a view anybody renders on purpose.
# Counting it in the numerator and not the denominator overstated the figure by exactly one, for
# as long as this script has existed.
#
# Found by arithmetic rather than by noticing: the header said "never rendered by it (8)" over a
# list of NINE, because the count came from 119 - rendered and the list came from comparing the
# two files. Two ways of computing the same thing is what caught it, which is the argument for
# having printed both.
EXCLUDE='_ViewStart|_ViewImports'
sort -u "$LOG" | sed 's|^/Views/|Zero-K.info/Views/|' | grep -vE "$EXCLUDE" > "$LOG.rendered"
git ls-files 'Zero-K.info/Views/*.cshtml' | grep -vE "$EXCLUDE" | sort > "$LOG.all"
rendered=$(wc -l < "$LOG.rendered")
all=$(wc -l < "$LOG.all")

echo "$rendered of $all views render when the harness runs."
echo
echo "never rendered by it ($((all - rendered))):"

# Annotated from the stack run when there is one to annotate from. tools/stack.sh writes
# rendered-views-stack.txt with the views IT rendered, and some of those are ones this
# measurement CANNOT reach: it runs with no lobby server, so a view whose action asks
# Global.LobbyApi before choosing one is invisible here and would otherwise read as uncovered.
#
# From a file rather than a hardcoded list on purpose. A list of "these are fine really" goes
# stale silently, and the stale direction is the flattering one.
#
# This file CAN still be stale - it outlives the run that wrote it, which is the point, since
# this script does not start a stack of its own. So its date is printed rather than trusted: a
# view that stopped rendering over there would go on being annotated here until the next stack
# run rewrites the file, and the only honest answer is to say when that was.
STACK=rendered-views.tmp-stack
comm -13 "$LOG.rendered" "$LOG.all" > "$LOG.missing"

# The arithmetic and the list have to agree, and this is here because once they did not. The
# count above is 119 - rendered; the list is what comm found. Those are two ways of computing
# the same number, and they diverge the moment the two files are built from different rules -
# which is exactly the bug that counting _ViewStart on one side only produced. Cheap to assert,
# and the only thing that would have said so out loud.
listed=$(wc -l < "$LOG.missing")
if [ "$listed" != "$((all - rendered))" ]; then
    echo "the count says $((all - rendered)) and the list has $listed - the rendered and tracked" >&2
    echo "sets are being built by different rules, so neither number means what it says." >&2
    rm -f "$LOG.rendered" "$LOG.all" "$LOG.missing"
    exit 2
fi

elsewhere=0
while read -r view; do
    [ -z "$view" ] && continue
    if [ -f "$STACK" ] && grep -qF "${view#Zero-K.info}" "$STACK"; then
        echo "  ${view#Zero-K.info/Views/}  - rendered by tools/stack.sh, against a lobby server"
        elsewhere=$((elsewhere + 1))
    else
        echo "  ${view#Zero-K.info/Views/}"
    fi
done < "$LOG.missing"

if [ -f "$STACK" ]; then
    echo
    echo "$((rendered + elsewhere)) of $all across both runs; $((all - rendered - elsewhere)) render nowhere yet."
    echo "(the stack half is from $STACK, written $(date -r "$STACK" '+%Y-%m-%d %H:%M'))"
else
    echo
    echo "(run ./tools/stack.sh to find out which of these render against a lobby server)"
fi

rm -f "$LOG.rendered" "$LOG.all" "$LOG.missing"
