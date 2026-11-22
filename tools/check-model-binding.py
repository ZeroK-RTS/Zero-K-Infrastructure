#!/usr/bin/env python3
"""A request-bound entity must not be written to the database whole.

    ./tools/check-model-binding.py

Model binding fills every public settable property it can find a form field for. An action that
takes an entity and hands that same instance to InsertOnSubmit, Add or Attach therefore lets the
request decide EVERY column, including the ones no form ever shows - which is over-posting, and
the reason it keeps happening is that the code reads perfectly well:

    public ActionResult SubmitCreate(Clan clan, ...)
        db.Clans.InsertOnSubmit(clan);

ClansController did exactly that, and the extra column that mattered was ForumThreadID.
ForumController treats a thread a clan points at as that clan's own - "you cannot post in their
clan thread" to anybody else - so one additional field in one POST, from any account allowed to
make a clan, locked the whole site out of any thread it named. IsDeleted was settable the same
way.

**The rule is the site's own convention, not an invention.** Four other actions bind an entity -
LobbyNews, News, GameMode and DynamicConfig - and every one of them copies named fields onto a
row it loaded or newed up itself. Clan creation was the single place that did not, which is what
makes this checkable without noise: the fix is to do what the neighbours already do.

What is matched is narrow and literal: a parameter whose type is a class under ZkData/Ef/, passed
BY NAME to InsertOnSubmit, InsertAllOnSubmit, Add, AddObject or Attach in the same action, or by
the name of a local that is a plain alias of it (`var same = clan;`). Copying fields off it is
fine and is the point - `var fresh = new Clan { ... }` built from the bound one is exactly the
shape this wants, and is what the fix looks like.

Anything cleverer than a direct alias defeats it: a list, a ternary, a helper that returns the
same reference. That is the honest edge of reading C# with regular expressions, and the reason
the rule is phrased as a CONVENTION the other four actions already follow rather than as a proof. An action that must insert the bound instance can say so:

    [BindsWholeEntityByDesign("nothing on this type is privileged")]

**This is a static check and claims nothing about runtime.** Asserting it end to end would mean
driving /Clans/SubmitCreate, which requires a decodable image that the action writes into the
site's image directory - filesystem side effects this repository's checks deliberately avoid.
What holds the fix is this script plus the control that removes it.
"""
import re
import subprocess
import sys

SIGNATURE = re.compile(
    r"^[ \t]*public[ \t]+(?:async[ \t]+)?"
    r"(?:ActionResult|Task<ActionResult>|IActionResult|Task<IActionResult>)[ \t]+(\w+)[ \t]*\(([^)]*)\)",
    re.MULTILINE | re.DOTALL)
PARAM = re.compile(r"\b(\w+)\s+(\w+)\s*(?:,|$|=)")
ENTITY = re.compile(r"^\s*public\s+(?:partial\s+)?class\s+(\w+)", re.MULTILINE)
BY_DESIGN = re.compile(r'\[BindsWholeEntityByDesign\("([^"]*)"\)\]')


def entities():
    """Every class declared under ZkData/Ef/ - the EF model the site binds against."""
    listed = subprocess.run(["git", "ls-files", "ZkData/Ef/*.cs", "ZkData/Ef/**/*.cs"],
                            capture_output=True, text=True, check=True)
    found = set()
    for path in listed.stdout.splitlines():
        if not path.endswith(".cs"):
            continue
        with open(path, encoding="utf-8-sig") as handle:
            found.update(ENTITY.findall(handle.read()))
    return found


def controllers():
    listed = subprocess.run(["git", "ls-files",
                             "Zero-K.info/Controllers/*.cs", "ZeroKWeb.Host/Controllers/*.cs"],
                            capture_output=True, text=True, check=True)
    return [line for line in listed.stdout.splitlines() if line]


def body_of(text, start):
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
            pending.insert(0, stripped)
        else:
            break
    return found


def scan(paths, known):
    whole, by_design, bound = [], [], 0
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in SIGNATURE.finditer(text):
            parameters = [(t, n) for t, n in PARAM.findall(match.group(2)) if t in known]
            if not parameters:
                continue
            bound += 1
            body = body_of(text, match.end())
            if body is None:
                continue
            attributes = attributes_above(text, match.start())
            design = next((BY_DESIGN.search(a) for a in attributes if BY_DESIGN.search(a)), None)
            for kind, name in parameters:
                # The parameter, plus any local that is a plain alias of it. Without this,
                # `var same = clan; db.Clans.InsertOnSubmit(same);` reads as safe.
                names = {name}
                for alias in re.finditer(
                        r"\b(?:var|%s)\s+(\w+)\s*=\s*%s\s*;" % (re.escape(kind), re.escape(name)),
                        body):
                    names.add(alias.group(1))
                sink = re.search(
                    r"\b(?:InsertOnSubmit|InsertAllOnSubmit|Add|AddObject|Attach)\s*\(\s*(?:%s)\s*\)"
                    % "|".join(re.escape(n) for n in sorted(names)), body)
                if not sink:
                    continue
                line = text.count("\n", 0, match.start()) + 1
                where = (path, line, match.group(1), kind, sink.group(0))
                if design:
                    by_design.append(where + (design.group(1),))
                else:
                    whole.append(where)
    return whole, by_design, bound


# A check that looks at nothing passes. Two lists here, and the quieter failure is the second:
# with no entity types every action looks harmless, the output still names a number of
# controllers, and the check reports success having compared nothing.
def main():
    paths = controllers()
    if not paths:
        print("found no controllers - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    known = entities()
    if not known:
        print("found no entity types under ZkData/Ef - nothing would look bound", file=sys.stderr)
        return 2

    whole, by_design, bound = scan(paths, known)

    for path, line, name, kind, sink in whole:
        print("%s:%d: %s writes the request-bound %s whole (%s)" % (path, line, name, kind, sink))
    if whole:
        print("\n%d action(s) let the request set every column of an entity." % len(whole))
        print("Copy the fields the form offers onto a row you loaded or newed up, as the other")
        print('entity-bound actions do - or say why it is right with [BindsWholeEntityByDesign("...")].')
        return 1

    print("no action writes a request-bound entity whole"
          " - %d action(s) bind one of %d entity type(s) in %d controller(s)%s"
          % (bound, len(known), len(paths),
             ", %d by design" % len(by_design) if by_design else ""))
    for path, line, name, kind, sink, reason in by_design:
        print("  %s:%d  %s (%s) - %s" % (path, line, name, kind, reason))
    return 0


if __name__ == "__main__":
    sys.exit(main())
