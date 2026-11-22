#!/usr/bin/env python3
"""An action that changes the LOBBY SERVER must not be reachable by GET.

    ./tools/check-lobby-writes.py

The third side of the CSRF triangle. tools/check-antiforgery.py reads forms, tools/
check-get-writes.py reads database calls - and between them sits a whole class of state change
that neither can see, because it happens in the other process. check-get-writes.py says so in its
own docstring and has said so since it was written: "this does NOT see an action whose only effect
is on the lobby server". Tourney's JoinBattle and RemoveBattle were found by reading. So was
Planetwars/MatchMakerJoin. Reading is not a check.

What makes this checkable rather than a guess is that the surface is an INTERFACE.
ZkLobbyServer/ILobbyServerApi.cs is the website's entire lobby-server vocabulary - 45 members,
enforced by the port's build, which has no reference to that project. So every member can be
classified once, beside itself:

    [ChangesLobbyState("posts a message")]
    Task GhostSay(Say say, int? battleID = null);

    [ReadsLobbyState]
    bool IsLobbyConnected(string user);

**Both attributes exist so that neither is a default.** A member carrying neither fails this
check until somebody decides which it is, which is the only way a member added next year gets
looked at. Classifying by section comment would not do: `// ---- commands ----` is about the
shape of the call, and OnNewsChanged, AddClanChannel, LogIpFailure and RedeemSessionToken all
change something from under other headings. RedeemSessionToken is the one to remember - it reads
like a query and is single-use, so redeeming invalidates.

**[HttpPost] is half the rule here too.** A cross-site page cannot send a GET-shaped change, but
it can auto-submit a form, so a POSTing action that changes the lobby must also validate the
token - or carry [NoAntiForgeryTokenByDesign("...")], which GithubController.Hook does: GitHub
signs the body with a shared secret and Hook verifies the HMAC before reading it.

An action is reachable by GET unless it carries [HttpPost]. One that calls a changing member must
say which it is, on the method:

    [ChangesLobbyOnGetByDesign("the game client follows this link itself")]
    [ChangesLobbyOnGetNotYetFixed("the chat panel posts through Ajax.BeginForm")]

The second kind is counted and printed every run. A list that shrinks, not a decision.

Calls are matched on the RECEIVER, not on the name alone: `Global.LobbyApi.GhostSay(...)` and
`api?.GhostSay(...)` count, `something_else.GhostSay(...)` does not. check-get-writes.py matches
bare names and pays one false follow for it; here the receiver is always the lobby API, so there
is no reason to be looser than the code is.
"""
import re
import subprocess
import sys

SIGNATURE = re.compile(
    r"^[ \t]*public[ \t]+(?:async[ \t]+)?"
    r"(?:ActionResult|Task<ActionResult>|IActionResult|Task<IActionResult>)[ \t]+(\w+)[ \t]*\(",
    re.MULTILINE)

# A member declaration in the interface, with the attribute that classifies it on the line above.
# The attribute and the member are read together on purpose: a classification that could drift
# away from what it classifies is the thing this is trying to avoid.
CHANGES = re.compile(
    r'\[ChangesLobbyState\("([^"]*)"\)\][ \t]*\r?\n[ \t]*[^\n]*?\b(\w+)[ \t]*[\(\{;]')
READS = re.compile(
    r'\[ReadsLobbyState\][ \t]*\r?\n[ \t]*[^\n]*?\b(\w+)[ \t]*[\(\{;]')
# Every member, classified or not, so an unclassified one can be named.
MEMBER = re.compile(
    r"^        (?!//|/\*|\*|\[)(?!public |internal |protected |private |class |}|{)"
    r"[^\n]*?\b(\w+)[ \t]*[\(\{;]", re.MULTILINE)

HTTP_POST = re.compile(r"\[\s*HttpPost\s*[\]\(]")
TOKEN = re.compile(r"\[\s*ValidateAntiForgeryToken\s*\]")
NO_TOKEN = re.compile(r'\[NoAntiForgeryTokenByDesign\(')
BY_DESIGN = re.compile(r'\[ChangesLobbyOnGetByDesign\("([^"]*)"\)\]')
NOT_YET = re.compile(r'\[ChangesLobbyOnGetNotYetFixed\("([^"]*)"\)\]')

INTERFACE = "ZkLobbyServer/ILobbyServerApi.cs"


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


def classify():
    """(changing, reading, unclassified) member names, read off the interface itself."""
    with open(INTERFACE, encoding="utf-8-sig") as handle:
        text = handle.read()
    # Only the interface body: the attribute CLASSES are declared below it in the same file, and
    # their own members are not lobby API members.
    start = text.find("public interface ILobbyServerApi")
    end = text.find("\n    }", start)
    body = text[start:end] if start >= 0 and end > start else ""

    changing = {name: what for what, name in CHANGES.findall(body)}
    reading = set(READS.findall(body))
    everything = set(MEMBER.findall(body))
    unclassified = everything - set(changing) - reading
    return changing, reading, unclassified


def scan(paths, changing):
    """Actions a GET can reach that call a changing member."""
    # The receiver is the lobby API, spelled Global.LobbyApi or a local holding it. Matching the
    # member name alone would catch GetTourneyBattles on anything at all.
    call = re.compile(r"\bLobbyApi\s*\??\s*\.\s*(\w+)\s*\(")
    plain, by_design, not_yet, untokened = [], [], [], []
    for path in paths:
        with open(path, encoding="utf-8-sig") as handle:
            text = handle.read()
        for match in SIGNATURE.finditer(text):
            body = body_of(text, match.end())
            if body is None:
                continue
            hit = sorted({c for c in call.findall(body)} & set(changing))
            if not hit:
                continue
            attributes = attributes_above(text, match.start())
            line = text.count("\n", 0, match.start()) + 1
            if any(HTTP_POST.search(a) for a in attributes):
                if not any(TOKEN.search(a) for a in attributes) \
                        and not any(NO_TOKEN.search(a) for a in attributes):
                    untokened.append((path, line, match.group(1), hit))
                continue
            where = (path, line, match.group(1), hit)
            design = next((BY_DESIGN.search(a) for a in attributes if BY_DESIGN.search(a)), None)
            pending = next((NOT_YET.search(a) for a in attributes if NOT_YET.search(a)), None)
            if design:
                by_design.append(where + (design.group(1),))
            elif pending:
                not_yet.append(where + (pending.group(1),))
            else:
                plain.append(where)
    return plain, by_design, not_yet, untokened


# A check that looks at nothing passes. Three ways this one could: no controllers, no interface
# members, or an interface whose members are all suddenly unclassified because the attribute got
# renamed. The third would otherwise read as "nothing changes the lobby", which is the most
# comfortable wrong answer available.
def main():
    paths = controllers()
    if not paths:
        print("found no controllers - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    changing, reading, unclassified = classify()
    if not changing and not reading:
        print("found no classified members in %s - the check would pass by seeing nothing"
              % INTERFACE, file=sys.stderr)
        return 2

    if unclassified:
        print("every member of ILobbyServerApi must say whether it changes anything:")
        for name in sorted(unclassified):
            print("  %s" % name)
        print('\nAdd [ChangesLobbyState("<what it changes>")] or [ReadsLobbyState] above it.')
        return 1

    plain, by_design, not_yet, untokened = scan(paths, changing)

    for path, line, name, hit in plain:
        print("%s:%d: %s changes the lobby server (%s) and a GET can reach it"
              % (path, line, name, ", ".join(hit)))
    if plain:
        print("\n%d action(s) change lobby state on a GET with nothing said about it."
              % len(plain))
        print("Add [HttpPost] and [ValidateAntiForgeryToken], and make the view use Html.PostLink")
        print('- or say why it is right, with [ChangesLobbyOnGetByDesign("...")] on the method.')
        return 1

    for path, line, name, hit in untokened:
        print("%s:%d: %s changes the lobby (%s) and takes POST, but never validates the token"
              % (path, line, name, ", ".join(hit)))
    if untokened:
        print("\n%d POSTing action(s) change the lobby with no anti-forgery token validated."
              % len(untokened))
        print("Add [ValidateAntiForgeryToken] - or, if the caller is not a browser and cannot")
        print('send one, say so with [NoAntiForgeryTokenByDesign("...")] on the method.')
        return 1

    print("no action changes the lobby on a GET without saying why"
          " - %d member(s) change something, %d only answer (%d by design, %d not yet fixed)"
          % (len(changing), len(reading), len(by_design), len(not_yet)))
    if not_yet:
        print("\nstill reachable by GET, and should not be:")
        for path, line, name, hit, reason in not_yet:
            print("  %s:%d  %s (%s) - %s" % (path, line, name, ", ".join(hit), reason))
    return 0


if __name__ == "__main__":
    sys.exit(main())
