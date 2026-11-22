#!/usr/bin/env python3
"""An action that writes to the database must not be reachable by GET.

    ./tools/check-get-writes.py

The link-shaped half of tools/check-antiforgery.py. That one looks at forms; this one looks at
the other end, because a link cannot carry a token and never could - an <a href> is a GET, so the
only defence is that the action refuses one. Nothing about a state-changing GET looks wrong in a
view: it is an ordinary link, and a third-party page can fire it with the visitor's cookies by
being loaded.

**What counts as a write is the EF/LINQ-to-SQL call, not a guess**: SaveChanges, SubmitChanges,
InsertOnSubmit, InsertAllOnSubmit, DeleteOnSubmit, DeleteAllOnSubmit - and EntityFramework
.Extensions' bulk pair, `.Update(x => new ...)` and `.Delete()`, which issue SQL straight at the
server and never go near SaveChanges. Those two were missing at first, and what they hid was
PlanetwarsAdmin's ResetRatings: a GET that rewrote every PlanetWars battle and rating in the
database. `Update` is matched with a word boundary, so UpdateLastRead and UpdateMission are not
mistaken for it. That is deliberately narrow and checkable.

**Where it looks for those calls is the other half, and it was too small twice.** First it read
only the action's own body, which is how /Factions/LeaveFaction sat unnoticed - three lines, the
SaveChanges one call away in a helper beside it. Then it followed calls, but only into the two
CONTROLLER directories, which is how ImportIpnPayment sat unnoticed: /Contributions/Ipn is four
lines, and the write is in ZkData/PayPal/. That one was not academic. The postback asking PayPal
whether it had sent the notification ran AFTER the row was written, so an anonymous request
recorded a contribution, granted kudos to an account of the sender's choosing and mailed out a
redeem code before PayPal said it had sent nothing.

So writer definitions are now read from Zero-K.info/, ZkData/ and ZeroKWeb.Core/ - the site, its
data layer and the port's compat layer - while ACTIONS still come only from the controller
directories.

**A write does not have to look like a call.** `MiscVar.DefaultEngine = engine` is one line of
assignment, and behind the property is a setter that calls SetValue, which calls StoreDbValue,
which opens a context and saves. Matching calls could never see it, so /Engines/MakeDefault - a
link that changes the default engine for every player and starts a Steam depot rebuild - read as
an action that writes nothing. Properties whose SETTER writes are therefore collected the same
way methods are, and an assignment to one counts as a write. All eight of them live in MiscVar,
and three of the four actions assigning one had already been closed by hand. The cost of that is one false follow today, and it is worth stating what pays for
it: calls are matched by NAME, so a controller calling Foo() is taken to reach any Foo() in those
531 files that writes. Only names actually defined there are followed, which keeps an unrelated
framework method out, but two methods that share a name are one method to this script.

It still does NOT see an action whose only effect is on the lobby server, out in the other
process - Tourney's JoinBattle and RemoveBattle were of that kind, found by reading rather than
by this, and ten more GET-reachable actions call that API today. That wants its own check,
because the lobby API is an interface and its members can be classified one by one; guessing at
them here would not be checkable. A check that is honest about its edges is worth more than one
that guesses at them.

**[HttpPost] is half the rule, and this script used to stop there.** A cross-site page cannot
make a browser send a GET-shaped write, but it can auto-submit a form, so POST without a validated
token is not a defence. ForumController.SubmitPost - the site's most-used write - was [HttpPost]
with no [ValidateAntiForgeryToken], while both forms that post to it emitted
@Html.AntiForgeryToken() faithfully on every page. The token was sent and thrown away, and both
CSRF checks passed: tools/check-antiforgery.py saw a token in the view, and this one saw
[HttpPost] and skipped. So a POSTing action that writes must also validate, or say why it cannot:

    [NoAntiForgeryTokenByDesign("PayPal is the caller and has no session here")]

All four exemptions are the same shape - the caller is not a browser. A game client posting a
command, PayPal posting a notification, GitHub posting a signed webhook. Each authenticates some
other way and the reason has to say which.

An action is reachable by GET unless it carries [HttpPost]. Every action that writes and is
reachable must say which it is, on the method:

    [WritesOnGetByDesign("records that the thread was read")]
    [WritesOnGetNotYetFixed("the vote links are raw HTML built in a helper")]

The second kind is counted and printed every run. It is a list that shrinks, not a decision.
"""
import re
import subprocess
import sys

SIGNATURE = re.compile(
    r"^[ \t]*public[ \t]+(?:async[ \t]+)?"
    r"(?:ActionResult|Task<ActionResult>|IActionResult|Task<IActionResult>)[ \t]+(\w+)[ \t]*\(",
    re.MULTILINE)
WRITE = re.compile(r"\b(?:SaveChanges|SubmitChanges|InsertOnSubmit|InsertAllOnSubmit"
                   r"|DeleteOnSubmit|DeleteAllOnSubmit|Update|Delete)\s*\(")
# Any method at all, not only actions - the helpers an action can call. Used to answer
# "does this name, called from an action, end in a write?"
ANY_METHOD = re.compile(
    r"^[ \t]*(?:public|private|protected|internal)[ \t]+(?:static[ \t]+)?(?:async[ \t]+)?"
    r"[\w<>,\[\]\.\?]+[ \t]+(\w+)[ \t]*\(",
    re.MULTILINE)

# A call to something by name. Deliberately crude: it matches `Foo(` and `Bar.Foo(`, and the
# caller only follows names it has actually seen defined in these files, so the crudeness costs
# false FOLLOWS rather than false positives.
CALL = re.compile(r"\b(\w+)\s*\(")

# `Foo = ` but not `Foo == `, `Foo >= `, `Foo != `. Matched against the names of properties whose
# setter writes, never on its own - `acc.HasKudos = true` is an assignment too, and it is not a
# write until something saves.
ASSIGN = re.compile(r"\b(\w+)\s*=(?![=>])")

# A property declaration: the `{` is required, so a field (`... x = 1;`) and a method (`... x(`)
# are not mistaken for one, and `class`/`struct`/`interface`/`enum` are excluded by name because
# `public class MiscVar {` has exactly the same shape.
#
# The brace is matched across a newline, and that is not cosmetic. Requiring it on the same line
# found MiscVar.DefaultEngine, which is written on one, and silently missed ZklsMaxUsers,
# PlanetWarsMode, PlanetWarsNextMode and PlanetWarsNextModeTime, which put it on the next. The
# control said so: taking [HttpPost] off SetZklsMaxPlayers left this check green.
PROPERTY = re.compile(
    r"^[ \t]*(?:public|internal|protected)[ \t]+"
    r"(?:(?:static|readonly|virtual|override|sealed|new|abstract)[ \t]+)*"
    r"(?!class\b|struct\b|interface\b|enum\b|record\b)[\w<>,\[\]\.\?]+[ \t]+(\w+)\s*\{",
    re.MULTILINE)
SETTER = re.compile(r"\bset\s*(?:\{|=>)")

HTTP_POST = re.compile(r"\[\s*HttpPost\s*[\]\(]")
TOKEN = re.compile(r"\[\s*ValidateAntiForgeryToken\s*\]")
NO_TOKEN = re.compile(r'\[NoAntiForgeryTokenByDesign\(')
BY_DESIGN = re.compile(r'\[WritesOnGetByDesign\("([^"]*)"\)\]')
NOT_YET = re.compile(r'\[WritesOnGetNotYetFixed\("([^"]*)"\)\]')


def helpers():
    """The files read for writer DEFINITIONS only - never for actions.

    The site, its data layer and the port's compat layer: where a controller's helpers actually
    live. Kept separate from controllers() because the two lists answer different questions, and
    because a check that found no controllers and plenty of helpers would still be blind.
    """
    listed = subprocess.run(["git", "ls-files", "Zero-K.info/", "ZkData/", "ZeroKWeb.Core/"],
                            capture_output=True, text=True, check=True)
    return [line for line in listed.stdout.splitlines() if line.endswith(".cs")]


def controllers():
    # BOTH controller directories, because both are deployed. ZeroKWeb.Host ships
    # HarnessController alongside every linked site controller, so a GET that writes in it is as
    # live as one in Zero-K.info/Controllers - and this glob did not look at it. The limit was a
    # glob rather than a decision; it finds nothing new today, and now it would.
    listed = subprocess.run(["git", "ls-files",
                             "Zero-K.info/Controllers/*.cs", "ZeroKWeb.Host/Controllers/*.cs"],
                            capture_output=True, text=True, check=True)
    return [line for line in listed.stdout.splitlines() if line]


def body_of(text, start):
    """The method body, by brace matching. Returns None if it does not close."""
    open_brace = text.find("{", start)
    if open_brace < 0:
        return None
    depth = 0
    for index in range(open_brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[open_brace:index]
    return None


def attributes_above(text, start):
    """The attributes immediately above a signature, stopping at a blank line or code.

    An attribute may span several physical lines - a long reason wraps - and reading bottom-up
    those continuations arrive BEFORE the `[` that opens them. Treating one as code stopped the
    walk, so every attribute above it became invisible: a three-line
    [NoAntiForgeryTokenByDesign("...")] hid the [HttpPost] over it and the action read as
    reachable by GET. Continuations are gathered and folded into the attribute they belong to.
    """
    head = text[:start].rstrip("\n")
    found, pending = [], []
    for line in reversed(head.split("\n")):
        stripped = line.strip()
        if stripped.startswith("["):
            found.append(" ".join([stripped] + pending))
            pending = []
        elif stripped.startswith("//") or stripped.startswith("/*") or stripped.startswith("*"):
            continue
        elif pending or stripped.endswith("]"):
            # Part of a multi-line attribute, read bottom-up. Only reachable from directly above
            # a signature, where the alternative is a brace, a comment or a blank line - none of
            # which ends in a bracket.
            pending.insert(0, stripped)
        else:
            break
    return found


def writing_methods(paths):
    """Every method in these files that writes, directly or through another one of them.

    Transitive, not one level, because PerformLeaveFaction in turn calls
    ClansController.PerformLeaveClan, and ImportIpnPayment reaches SaveChanges through
    AddPayPalContribution. Only names DEFINED in these files are followed, so an unrelated
    framework method that happens to share a name is not chased - see the module docstring for
    what that costs when two methods HERE share one.
    """
    bodies = {}
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in ANY_METHOD.finditer(text):
            body = body_of(text, match.end())
            if body is not None:
                bodies.setdefault(match.group(1), []).append(body)

    writes = {name for name, found in bodies.items()
              if any(WRITE.search(body) for body in found)}

    # Closure: a method that calls a writer is a writer. Repeat until nothing new appears -
    # the graph is small and this is clearer than ordering it by hand.
    changed = True
    while changed:
        changed = False
        for name, found in bodies.items():
            if name in writes:
                continue
            called = {c for body in found for c in CALL.findall(body)}
            if called & writes:
                writes.add(name)
                changed = True
    return writes


def writing_properties(paths, writers):
    """Properties whose SETTER writes, directly or by calling something that does.

    Only the setter half is read: a getter that writes would be a different and stranger bug, and
    reading the whole property would make every MiscVar getter - they share a cache with the
    setters - look like one.
    """
    found = {}
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in PROPERTY.finditer(text):
            body = body_of(text, match.end() - 1)
            if body is None:
                continue
            setter = SETTER.search(body)
            if not setter:
                continue
            tail = body[setter.start():]
            if WRITE.search(tail) or (set(CALL.findall(tail)) & writers):
                found[match.group(1)] = path
    return found


def scan(paths, helper_paths):
    writing, by_design, not_yet, untokened = [], [], [], []
    writers = writing_methods(paths + helper_paths)
    setters = set(writing_properties(paths + helper_paths, writers))
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in SIGNATURE.finditer(text):
            body = body_of(text, match.end())
            if body is None:
                continue
            # Its own body, a call to something that ends in a write, or an assignment to a
            # property whose setter does.
            if (not WRITE.search(body)
                    and not ({c for c in CALL.findall(body)} & writers)
                    and not ({a for a in ASSIGN.findall(body)} & setters)):
                continue
            attributes = attributes_above(text, match.start())
            # The attribute itself, not the word anywhere inside one. A reason that mentions
            # HttpPost - "needs HttpPost and a token" is the obvious thing to write - used to
            # read as the action HAVING it, and the action was then skipped entirely. Found by
            # writing exactly that sentence.
            line = text.count("\n", 0, match.start()) + 1
            if any(HTTP_POST.search(attribute) for attribute in attributes):
                # POST, so no link reaches it - but a cross-site form can, unless the token is
                # validated or the action says why its caller cannot send one.
                if not any(TOKEN.search(a) for a in attributes) \
                        and not any(NO_TOKEN.search(a) for a in attributes):
                    untokened.append((path, line, match.group(1)))
                continue
            where = (path, line, match.group(1))
            design = next((BY_DESIGN.search(a) for a in attributes if BY_DESIGN.search(a)), None)
            pending = next((NOT_YET.search(a) for a in attributes if NOT_YET.search(a)), None)
            if design:
                by_design.append(where + (design.group(1),))
            elif pending:
                not_yet.append(where + (pending.group(1),))
            else:
                writing.append(where)
    return writing, by_design, not_yet, untokened


# A check that looks at nothing passes. These scripts find their subjects through
# git ls-files, so a directory rename, a project move or a glob that stops matching
# leaves them scanning an empty list and reporting success - which is how a 314-item
# guard in tools/build-website.sh ran as a 0-item guard, green, for one commit.
def main():
    paths = controllers()
    if not paths:
        print("found no controllers - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    helper_paths = helpers()
    if not helper_paths:
        # Blinding the second list is quieter than blinding the first: every action is still
        # read, the output still names a number of controllers, and only the writes that live
        # one call outside them stop being seen. That is the failure this check just had.
        print("found no helper sources - writes outside the controllers would be invisible",
              file=sys.stderr)
        return 2

    unmarked, by_design, not_yet, untokened = scan(paths, helper_paths)

    for path, line, name, in unmarked:
        print("%s:%d: %s writes to the database and a GET can reach it" % (path, line, name))
    if unmarked:
        print("\n%d action(s) change state on a GET with nothing said about it." % len(unmarked))
        print("Add [HttpPost] and [ValidateAntiForgeryToken], and make the view use Html.PostLink")
        print("- or say why it is right, with [WritesOnGetByDesign(\"...\")] on the method.")
        return 1

    for path, line, name in untokened:
        print("%s:%d: %s writes and takes POST, but never validates the token" % (path, line, name))
    if untokened:
        print("\n%d POSTing action(s) change state with no anti-forgery token validated."
              % len(untokened))
        print("Add [ValidateAntiForgeryToken] - or, if the caller is not a browser and cannot")
        print('send one, say so with [NoAntiForgeryTokenByDesign("...")] on the method.')
        return 1

    print("no action writes on a GET without saying why in %d controller(s), following calls"
          " into %d more file(s) (%d by design, %d not yet fixed)"
          % (len(paths), len(helper_paths), len(by_design), len(not_yet)))
    if not_yet:
        print("\nstill reachable by GET, and should not be:")
        for path, line, name, reason in not_yet:
            print("  %s:%d  %s - %s" % (path, line, name, reason))
    return 0


if __name__ == "__main__":
    sys.exit(main())
