#!/usr/bin/env python3
"""A player name reaches the database only through Account.SetName, which validates it.

    ./tools/check-name-invariant.py

Several places render a player name into HTML without encoding it. That is not an oversight and
they say so - HtmlHelperExtensions, in both copies:

    Compare PrintAccount, which does not encode and does not need to: Account.IsValidLobbyName
    is enforced server-side at both registration and rename, so a player name cannot carry markup.

The reasoning is sound and the premise was held up by nothing. `Account.SetName` assigned whatever
it was given; the charset was checked by each caller remembering to check it. Two callers did. A
third that forgot would have turned a stored name into stored script on every page that prints
one - the user list, every forum post, every battle, every PlanetWars event - and nothing in the
repository would have said a word.

So the validation moved into SetName, and this script asserts the two halves of that:

  1. SetName still calls IsValidLobbyName. Without this the script would keep passing while the
     thing it protects had been deleted - and it is the likelier edit of the two, because a
     validation that never fires looks like dead code.
  2. No `new Account { Name = ... }` anywhere in the deployed code sets a name from anything but
     a constant. An object initializer walks straight past SetName, which is exactly how the
     second, unchecked door existed in LoginChecker until it was taken out.

Literal and GlobalConst names are allowed: ZkData/Migrations/Configuration.cs seeds "test" and
the Nightwatch bot that way, and a constant in the source is not a name a request can choose.

**Scope is the deployed code** - the site, its data layer, the port's compat layer and the lobby
server. Fixer/, Tests*/ and tools/ write names too and are not reachable by a request; naming that
boundary is better than a glob that quietly stops matching.
"""
import re
import subprocess
import sys

DEPLOYED = ["Zero-K.info/", "ZeroKWeb.Core/", "ZeroKWeb.Host/",
            "ZkData/", "ZkData.Core/", "ZkLobbyServer/"]
ACCOUNT = "ZkData/Ef/Account.cs"

SETNAME = re.compile(r"public\s+void\s+SetName\s*\(\s*string\s+(\w+)\s*\)")
VALIDATES = re.compile(r"IsValidLobbyName\s*\(")
# `new Account { ... Name = <value> ... }` and `new Account() { ... }`. Non-greedy to the first
# closing brace: an initializer that nests one would be read short, which hides a fault rather
# than inventing one.
INITIALIZER = re.compile(r"new\s+Account\s*(?:\(\s*\))?\s*\{(.*?)\}", re.DOTALL)
NAME_FIELD = re.compile(r"(?<![\w.])Name\s*=\s*([^,}\n]+)")
CONSTANT = re.compile(r'^(?:"[^"]*"|GlobalConst\.\w+|nameof\([^)]*\))$')


def sources():
    listed = subprocess.run(["git", "ls-files"] + DEPLOYED,
                            capture_output=True, text=True, check=True)
    return [line for line in listed.stdout.splitlines() if line.endswith(".cs")]


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


def door_is_locked():
    """(ok, why) - whether Account.SetName still validates what it is given."""
    try:
        with open(ACCOUNT, encoding="utf-8-sig") as handle:
            text = handle.read()
    except OSError:
        return None, "%s is not there to read" % ACCOUNT

    match = SETNAME.search(text)
    if not match:
        return None, "no SetName(string) in %s - the shape this checks is gone" % ACCOUNT

    body = body_of(text, match.end())
    if body is None:
        return None, "SetName's body does not close"
    if not VALIDATES.search(body):
        return False, "SetName no longer calls IsValidLobbyName, so nothing validates a name"
    return True, ""


def windows(paths):
    """`new Account { Name = <not a constant> }` - initializers that walk past SetName."""
    found = []
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in INITIALIZER.finditer(text):
            for field in NAME_FIELD.finditer(match.group(1)):
                value = field.group(1).strip().rstrip(",").strip()
                if CONSTANT.match(value):
                    continue
                found.append((path, text.count("\n", 0, match.start()) + 1, value))
    return found


# A check that looks at nothing passes. Here that would be the quiet way to lose the whole thing:
# with no sources there are no initializers to object to, and the locked-door assertion is the
# half that still has something to say - which is why it is asserted first and separately.
def main():
    paths = sources()
    if not paths:
        print("found no deployed sources - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    locked, why = door_is_locked()
    if locked is None:
        print(why, file=sys.stderr)
        return 2
    if not locked:
        print("%s: %s" % (ACCOUNT, why))
        print("\nEvery place that prints a player name unencoded relies on this. Put the")
        print("IsValidLobbyName check back, or encode at each of those places instead.")
        return 1

    open_windows = windows(paths)
    for path, line, value in open_windows:
        print("%s:%d: new Account sets Name = %s, which never reaches SetName" % (path, line, value))
    if open_windows:
        print("\n%d initializer(s) set a player name without validating it." % len(open_windows))
        print("Construct the account and call SetName, which is the one door that checks the")
        print("charset - or use a constant, which a request cannot choose.")
        return 1

    print("a player name reaches the database only through Account.SetName, which validates it"
          " (%d deployed source(s) read)" % len(paths))
    return 0


if __name__ == "__main__":
    sys.exit(main())
