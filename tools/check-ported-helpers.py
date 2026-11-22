#!/usr/bin/env python3
r"""The Html helpers that exist twice differ in the ways they are recorded to.

    ./tools/check-ported-helpers.py            fail if a difference changed
    ./tools/check-ported-helpers.py --update   re-record them

Zero-K.info/AppCode/HtmlHelperExtensions.cs is compiled by MVC 5 and
ZeroKWeb.Core/Mvc5Compat/HtmlHelperExtensions.Ported.cs by ASP.NET Core, and twenty-two
helpers appear in both: PrintAccount, PrintMap, AccountAvatar, BBCode and the rest - the
furniture of nearly every page. The signatures differ because MVC 5 has HtmlHelper and
ASP.NET Core has IHtmlHelper, which is why one file cannot simply be linked into the other
build the way HtmlHelperExtensions.Portable.cs is.

Two copies of the same markup drift, and these had:

    4d1be43d1 "Encode map and wiki names where they are rendered"

fixed PrintMap and PrintMediaWikiEdit in the MVC 5 copy. Neither fix reached the ported one,
so the .NET 9 site still interpolated a map's InternalName - which comes out of an uploaded
map archive - into an attribute and element content, and a wiki page title and editor name
from the recent-changes feed into an href and the front page. Both builds were green, every
check passed, and the port quietly had two vulnerabilities the site it replaces had fixed.

Comments are stripped before comparing: they do not change what is rendered, and a comment
edit churning the baseline would train people to re-record without reading. `this HtmlHelper`
and `WebContext()` are rewritten to their ASP.NET Core spellings, because those differ in
every helper and say nothing. Everything else - an expression body against a block body, a
namespace written out - is recorded as it is, so the baseline says what each pair's
difference IS.

A difference that changed is not necessarily wrong. Mirroring an edit into both copies
changes nothing here; changing one on purpose is a normal thing to do. It has to be looked
at, and then recorded with --update.
"""
# blinding-exempt: reads the two named HtmlHelperExtensions files directly rather than through
# git ls-files, and refuses outright when either parses to no helpers at all.
import difflib
import io
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
FRAMEWORK = ROOT / "Zero-K.info" / "AppCode" / "HtmlHelperExtensions.cs"
PORTED = ROOT / "ZeroKWeb.Core" / "Mvc5Compat" / "HtmlHelperExtensions.Ported.cs"
BASELINE = ROOT / "tools" / "ported-helpers.txt"

MEMBER = re.compile(r"^        public static [A-Za-z0-9_<>,\.\[\]\? ]+? "
                    r"([A-Za-z0-9_]+)\((this I?HtmlHelper[^)]*)\)", re.M)


def end_of_member(source, after):
    """
    From just past the signature to the end of the member: the matching close brace, or the
    semicolon of an expression body. Counted with the string literals skipped, because the
    markup these helpers build is full of braces - $"<span>{0}</span>" would otherwise close
    a body that is still open. Taking "up to the next member" instead swallowed whatever sat
    between them, which here is ProcessAtSignTags, and put 25 lines of it in the baseline.
    """
    index, depth, started = after, 0, False
    while index < len(source):
        char = source[index]
        if char in '"\'':
            verbatim = index > 0 and source[index - 1] == "@"
            index += 1
            while index < len(source):
                if not verbatim and source[index] == "\\":
                    index += 2
                    continue
                if source[index] == char:
                    if verbatim and index + 1 < len(source) and source[index + 1] == char:
                        index += 2
                        continue
                    break
                index += 1
        elif source.startswith("//", index):
            index = source.find("\n", index)
            if index < 0:
                return len(source)
        elif char == "{":
            depth, started = depth + 1, True
        elif char == "}":
            depth -= 1
            if started and depth == 0:
                return index + 1
        elif char == ";" and not started:
            return index + 1
        index += 1
    return len(source)


def members(path):
    """Keyed by name and parameter list, because several of these are overloaded."""
    source = io.open(path, encoding="utf-8-sig").read()
    found = {}
    for match in MEMBER.finditer(source):
        key = re.sub(r"\s+", " ", "%s(%s)" % (match.group(1), match.group(2)))
        key = key.replace("this HtmlHelper", "this IHtmlHelper")
        found[key] = source[match.start():end_of_member(source, match.end())]
    return found


def normalise(body):
    body = re.sub(r"//[^\n]*", "", body)
    body = body.replace("this HtmlHelper", "this IHtmlHelper").replace("WebContext()", "WebContext(helper)")
    return [line.strip() for line in body.splitlines() if line.strip()]


def report():
    framework, ported = members(FRAMEWORK), members(PORTED)
    shared = sorted(set(framework) & set(ported))

    lines = []
    for key in shared:
        diff = list(difflib.unified_diff(normalise(framework[key]), normalise(ported[key]),
                                         fromfile="MVC 5", tofile="ASP.NET Core", lineterm="", n=1))
        lines.append("### " + key)
        lines.extend(diff if diff else ["    (identical)"])
        lines.append("")

    for key in sorted(set(framework) - set(ported)):
        lines.append("### %s\n    only in HtmlHelperExtensions.cs (MVC 5)\n" % key)
    for key in sorted(set(ported) - set(framework)):
        lines.append("### %s\n    only in HtmlHelperExtensions.Ported.cs (.NET 9)\n" % key)

    return shared, framework, ported, "\n".join(lines) + "\n"


def main():
    shared, framework, ported, text = report()

    if not framework or not ported:
        print("read no helpers out of one of the two files - the check would pass by seeing nothing",
              file=sys.stderr)
        return 2

    if "--update" in sys.argv:
        io.open(BASELINE, "w", encoding="utf-8", newline="\n").write(text)
        print("recorded %d shared helper(s) in %s" % (len(shared), BASELINE.relative_to(ROOT)))
        return 0

    if not BASELINE.exists():
        print("missing %s - run with --update" % BASELINE.relative_to(ROOT))
        return 2

    if io.open(BASELINE, encoding="utf-8").read() == text:
        print("%d helper(s) exist in both copies and differ exactly as recorded "
              "(%d MVC 5 only, %d .NET 9 only)"
              % (len(shared), len(set(framework) - set(ported)), len(set(ported) - set(framework))))
        return 0

    print("a helper that exists in both copies changed in one of them:")
    print("")
    for line in difflib.unified_diff(io.open(BASELINE, encoding="utf-8").read().splitlines(),
                                     text.splitlines(), fromfile="recorded", tofile="now",
                                     lineterm="", n=2):
        print("  " + line)
    print("")
    print("MVC 5 serves HtmlHelperExtensions.cs and .NET 9 serves the .Ported.cs copy, so an edit")
    print("to one of them is an edit to half the site. Make it in both, then re-record with --update.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
