#!/usr/bin/env python3
r"""The three _ViewImports.cshtml files are identical.

    ./tools/check-view-imports.py

ZeroKWeb.Core, ZeroKWeb.Host and ZeroKWeb.Render each hold their own
Views/_ViewImports.cshtml, and that file decides what namespaces EVERY view in the project can
see. MVC 5 does the same job with one Views/Web.config <pages><namespaces> list; ASP.NET Core
uses this file, and the port needs one per project.

They cannot be a single linked file. The Razor SDK discovers _ViewImports.cshtml by where the
file physically sits under the project, not by Link metadata, so a linked one is compiled and
then ignored as an import. That was tried: it cost every view its namespaces - 944 errors on a
clean build, and ZERO on a dirty one, because obj/ still held the previously generated views.
A check on a build that did not rebuild is not a check.

So they stay three files, and this requires them to be the same.

**Most drift would be caught by the compiler anyway, and not all of it.** The same views compile
in more than one of these projects, so an import that a view NEEDS, removed from one, breaks that
build. What the compiler cannot see is an import ADDED to one project only: every view still
compiles in both, and an Html helper can resolve to a different extension method in each. That is
not hypothetical - it is the shape of the bug that let two XSS fixes reach the MVC 5 copies of
HtmlHelperExtensions and not the ported ones, which is why those copies have a check of their own.
"""
import io
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
NAME = "Views/_ViewImports.cshtml"


def main():
    # Found through git rather than by a hardcoded list, so a project that is added, renamed or
    # removed changes what this checks. tools/check-the-checks.sh blinds git and requires a
    # failure; a check reading fixed paths would pass blinded and prove nothing.
    listed = subprocess.run(["git", "-C", str(ROOT), "ls-files", "*/" + NAME],
                            capture_output=True, text=True)
    if listed.returncode != 0:
        print("git ls-files failed, so this check cannot find its subjects: %s"
              % listed.stderr.strip(), file=sys.stderr)
        return 2

    files = [name for name in listed.stdout.split() if name]
    if len(files) < 2:
        print("found %d %s - there is nothing to compare, which is not what this check is for"
              % (len(files), NAME), file=sys.stderr)
        return 2

    contents = {}
    for name in files:
        contents[name] = io.open(ROOT / name, encoding="utf-8-sig", errors="replace").read()

    first = files[0]
    differing = [name for name in files[1:] if contents[name] != contents[first]]

    if not differing:
        print("%d copies of %s, all identical" % (len(files), NAME))
        return 0

    print("%d of %d copies of %s differ:" % (len(differing), len(files), NAME))
    print("")
    import difflib
    for name in differing:
        print("  %s" % name)
        for line in difflib.unified_diff(contents[first].splitlines(), contents[name].splitlines(),
                                         fromfile=first, tofile=name, lineterm="", n=1):
            print("    " + line)
        print("")
    print("These decide what every view in each project can see. An import in one and not another")
    print("lets the same view resolve a different helper in each, which compiles in both.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
