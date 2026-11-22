#!/usr/bin/env python3
r"""Every view a controller asks for by name is there, spelled the way the disk spells it.

    ./tools/check-view-names.py

`return View("Ladders")` is a string. Nothing checks it at build time, on either stack, so a
view that does not exist under that name is a 500 the first time the action runs - and 35 of
this site's 120 views are rendered by no check here, which is exactly where a name like that
would sit unnoticed.

Case is the other half, and it is the half that only breaks off Windows.
LaddersController asked for `View("Ladders")` against a file named `ladders.cshtml`; NTFS does
not care and a case-sensitive disk does. It happened to work because ASP.NET Core looks compiled
views up without case - but the same spelling served from disk would not, and this repository has
already shipped two defects of exactly this shape, in `img/Avatars` and in the resources
directory. The file is `Ladders.cshtml` now and this keeps it that way.

Resolution follows Razor's own search order: Views/<Controller>/<Name>.cshtml, then
Views/Shared/<Name>.cshtml, and a `~/`-rooted path exactly as written. PortedViews/ counts as
present, because for .NET 9 it is what is compiled - see port-views.props.
"""
import io
import os
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

CLASS = re.compile(r'\bclass\s+(\w+?)Controller\b')
# View("Name"), PartialView("Name"), View("~/Views/X/Y.cshtml"), Html.Partial("~/...")
BY_NAME = re.compile(r'\b(?:Partial)?View\(\s*"([A-Za-z0-9_]+)"')
BY_PATH = re.compile(r'(?:\b(?:Partial)?View|Html\.(?:Partial|RenderPartial))\(\s*"(~/[^"]+?\.cshtml)"')


def main():
    listed = subprocess.run(["git", "-C", str(ROOT), "ls-files", "*.cshtml", "*.cs"],
                            capture_output=True, text=True, check=True).stdout.split()
    views = {f for f in listed if f.endswith(".cshtml")}
    sources = [f for f in listed if f.endswith(".cs") and "/Controllers/" in f]

    if not views or not sources:
        print("found no views or no controllers - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    def present(*candidates):
        # Exact, case-sensitive, in either tree: Zero-K.info/Views is what MVC 5 serves and
        # PortedViews is what .NET 9 compiles in its place.
        return any(c in views or c.replace("Zero-K.info/Views/", "PortedViews/") in views
                   for c in candidates)

    missing, checked = [], 0
    for name in sources:
        controller, text = None, io.open(ROOT / name, encoding="utf-8-sig", errors="replace").read()
        for number, line in enumerate(text.splitlines(), 1):
            found = CLASS.search(line)
            if found:
                controller = found.group(1)

            for path in BY_PATH.findall(line):
                checked += 1
                # "~/" is the WEB APP root, not the repository root - Zero-K.info/ for the site
                # and the host's own folder for the harness's controller. Resolving it against the
                # repository root instead made every one of these read as missing.
                rest = path[2:]
                if not present("Zero-K.info/" + rest, os.path.dirname(name) + "/../" + rest,
                               "ZeroKWeb.Host/" + rest):
                    missing.append((name, number, path, "no file at that path"))

            for view in BY_NAME.findall(line):
                if controller is None:
                    continue
                checked += 1
                where = ("Zero-K.info/Views/%s/%s.cshtml" % (controller, view),
                         "Zero-K.info/Views/Shared/%s.cshtml" % view)
                if present(*where):
                    continue
                # Separate the two failures: a name that is simply not there reads differently
                # from one that is there in another case, and only the second is a Linux problem.
                lower = {v.lower(): v for v in views}
                other = next((lower[w.lower()] for w in where if w.lower() in lower), None)
                missing.append((name, number, view,
                                "the file is %s" % other if other
                                else "no Views/%s/%s.cshtml and no Views/Shared/%s.cshtml"
                                     % (controller, view, view)))

    if missing:
        print("%d view name(s) a controller asks for that the disk does not match:" % len(missing))
        print("")
        for name, number, view, why in missing:
            print("  %s:%d  asks for \"%s\"" % (name, number, view))
            print("      %s" % why)
        print("")
        print("A view name is a string: neither build checks it, and the first request runs into")
        print("a 500. Where only the case differs it is worse - it works on Windows and not here.")
        return 1

    print("%d view name(s) asked for by %d controller(s), every one on disk as written"
          % (checked, len(sources)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
