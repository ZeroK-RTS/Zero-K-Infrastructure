#!/usr/bin/env python3
"""The two copies of the bundle list must agree.

    ./tools/check-bundles.py

Zero-K.info/App_Start/BundleConfig.cs is what the live site serves. It is written against
System.Web.Optimization's BundleCollection, which does not exist on .NET 9, so the port cannot
link it and ZeroKWeb.Core/Mvc5Compat/BundlingCompat.cs carries a second copy of the same list.

BundlingCompat's own comment said "the check in ZeroKWeb.Host compares the two". It did not:
nothing anywhere compared them, and the only two files mentioning ~/bundles/main were the two
lists. This is that check, written.

It matters because drift is silent in both directions. A script added to the site and not to the
port makes the ported pages quietly miss it; one removed from the site and left in the port makes
the port render a tag for a file that is gone - and the host harness asserts only that the bundle
renders SOMETHING, so neither shows up as a failure.
"""
# blinding-exempt: parses BundleConfig.cs and the port's bundle list directly rather than
# through git ls-files, and already refuses an empty parse - "could not find any bundles in
# %s - has its shape changed?", exit 2.
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SITE = ROOT / "Zero-K.info" / "App_Start" / "BundleConfig.cs"
PORT = ROOT / "ZeroKWeb.Core" / "Mvc5Compat" / "BundlingCompat.cs"


def read(path):
    return path.read_text(encoding="utf-8-sig")


def strip_comments(text):
    """Both files explain themselves in comments that mention script paths."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return re.sub(r"//[^\n]*", "", text)


def site_bundles(text):
    """bundles.Add(new ScriptBundle("~/bundles/main").Include("~/a.js", "~/b.js"));"""
    found = {}
    for match in re.finditer(
        r'new\s+\w*Bundle\(\s*"([^"]+)"\s*\)\s*\.Include\((.*?)\)\s*\)\s*;', text, flags=re.S
    ):
        found[match.group(1)] = re.findall(r'"([^"]+)"', match.group(2))
    return found


def initialiser_body(text, start):
    """The text between a { and its matching }, ignoring braces inside string literals.

    Written as a scan rather than a regex because one of the entries is
    "~/Scripts/jquery-{version}.js" - the placeholder the real bundler expands - and a
    non-greedy regex stops at the brace inside it. The first version of this check did exactly
    that and reported every file in the bundle as missing, which is how the scan came to exist.
    """
    depth = 0
    in_string = False
    index = start
    while index < len(text):
        char = text[index]
        if in_string:
            if char == "\\":
                index += 2
                continue
            if char == '"':
                in_string = False
        elif char == '"':
            in_string = True
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start + 1:index]
        index += 1
    return ""


def port_bundles(text):
    """["~/bundles/main"] = new[] { "~/a.js", "~/b.js", },"""
    found = {}
    for match in re.finditer(r'\["([^"]+)"\]\s*=\s*new\[\]\s*\{', text, flags=re.S):
        body = initialiser_body(text, match.end() - 1)
        found[match.group(1)] = re.findall(r'"([^"]+)"', body)
    return found


def main():
    site = site_bundles(strip_comments(read(SITE)))
    port = port_bundles(strip_comments(read(PORT)))

    # The build step needs the same list this check reads. Printing it here rather than parsing
    # the C# a second time in JavaScript keeps one parser: a second one would drift from this,
    # and drift between the list and the thing that builds it is exactly what this file is for.
    if "--print" in sys.argv:
        import json
        print(json.dumps(port, indent=2))
        return 0

    if not site:
        print("could not find any bundles in %s - has its shape changed?" % SITE.name, file=sys.stderr)
        return 2
    if not port:
        print("could not find any bundles in %s - has its shape changed?" % PORT.name, file=sys.stderr)
        return 2

    failures = 0
    for name in sorted(set(site) | set(port)):
        if name not in site:
            print("   FAIL  %s is in the port but not in BundleConfig.cs" % name)
            failures += 1
            continue
        if name not in port:
            print("   FAIL  %s is in BundleConfig.cs but not in the port" % name)
            failures += 1
            continue
        if site[name] != port[name]:
            print("   FAIL  %s differs" % name)
            for only in [f for f in site[name] if f not in port[name]]:
                print("           site has, port does not:  %s" % only)
            for only in [f for f in port[name] if f not in site[name]]:
                print("           port has, site does not:  %s" % only)
            if sorted(site[name]) == sorted(port[name]):
                print("           same files, different order - the order is what the page emits")
            failures += 1
            continue
        print("   ok    %s (%d files)" % (name, len(site[name])))

    print()
    print("bundle lists agree" if failures == 0 else "%d bundle(s) out of step" % failures)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
