#!/usr/bin/env python3
r"""Global.asax and the .NET 9 host declare the same routes.

    ./tools/check-routes.py

Zero-K.info/Global.asax.cs:RegisterRoutes is the MVC 5 route table. ZeroKWeb.Host/Program.cs
has to be the same table in ASP.NET Core's spelling, and for a long time it was not: it had
only the default route, and all seven of the custom ones were missing. Nothing noticed,
because the actions they point at take their parameters by NAME and the query-string form
reaches them anyway. /Static?name=UnitGuide works with no StaticFile route at all. So the
harness asked the site for ?name=, got 200, and the broken route sat under a green check.

Two tables drift. This compares them as tables rather than waiting for a request to notice:

    routes.MapRoute("ReplayFile", "Replays/{name}",        <- Global.asax.cs
        new { controller = "Replays", action = "Download", name = UrlParameter.Optional });
    app.MapControllerRoute("ReplayFile", "Replays/{name?}",   <- Program.cs
        new { controller = "Replays", action = "Download" });

Same name, same template, same controller and action. The spellings differ mechanically and
in known ways, and both sides are normalised before comparing:

  * MVC 5 makes a segment optional with `name = UrlParameter.Optional` in the defaults;
    ASP.NET Core writes it into the template as `{name?}`.
  * MVC 5 gives a segment a default in the defaults object, where having one also makes it
    optional; ASP.NET Core writes that into the template too, as `{controller=Home}`. Both
    become `{controller?}` plus a default of Home.
  * Route NAMES are compared without case, because neither framework's URL generation cares:
    Global.asax says "Default" and the ASP.NET Core template everyone copies says "default".

ORDER is compared too, because these routes are matched in order and the default one has to
stay last - a route after it is a route that never matches.

It also checks the OTHER route table, which is the reason this script grew a second half.
Application_Start configures Web API separately - GlobalConfiguration.Configure(
WebApiConfig.Register) - and on .NET Framework that is an independent pipeline with its own
controllers. Comparing MapRoute tables says nothing about it, and the port had no /api surface
at all for the whole of its life: WhrController serves POST /api/whr/battles, nothing in this
repository calls it, and the consumer is outside the repo.

So: every class deriving from ApiController has to be linked into the port. There is no route
table to compare, because ASP.NET Core needs no registration for these - MapControllerRoute
already builds the endpoint data source and that includes attribute-routed actions - which is
exactly why the failure is silent. The class simply has to exist in the build.

What this does NOT cover: routes.IgnoreRoute. MVC 5 uses it to hand `img/`, `Resources/`,
`autoregistrator/maps/` and robots.txt to the IIS static handler instead of to MVC. The port
has no equivalent line because it needs none - UseStaticFiles runs before routing, so those
paths never reach the route table in the first place. Different mechanism, same effect, and
not something a table comparison can say anything useful about.
"""
import io
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
LEGACY = "Zero-K.info/Global.asax.cs"
PORT = "ZeroKWeb.Host/Program.cs"

# routes.MapRoute("Name", "template", new { ... });  - the defaults run to the closing "});"
MVC = re.compile(r'routes\.MapRoute\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*(?:,\s*new\s*\{(.*?)\})?\s*\)\s*;',
                 re.DOTALL)
# app.MapControllerRoute("Name", "template"[, new { ... }]);
CORE = re.compile(r'app\.MapControllerRoute\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*(?:,\s*new\s*\{(.*?)\})?\s*\)\s*;',
                  re.DOTALL)
PAIR = re.compile(r'(\w+)\s*=\s*([^,}]+)')


def read(name):
    """The file with its comments taken out.

    Not an ornament. The first version matched the raw text, so

        // app.MapControllerRoute("StaticFile", "Static/{name?}", ...);

    counted as a registered route and the check stayed green with the route switched off -
    which is how the control that commented all seven out was passed rather than caught. A
    commented route is a route that does not run, and this has to read it that way.

    String-literal aware, because a route template is a string and the C# in these files is
    full of URLs: "p/zero-k/wiki/{node?}" has no // in it, but nothing says the next one
    added will not.
    """
    text = io.open(ROOT / name, encoding="utf-8-sig", errors="replace").read()

    out, index, length = [], 0, len(text)
    while index < length:
        char = text[index]
        if char == '@' and text[index:index + 2] == '@"':
            end = index + 2
            while end < length:
                if text[end] == '"':
                    if text[end:end + 2] == '""':
                        end += 2
                        continue
                    end += 1
                    break
                end += 1
            out.append(text[index:end])
            index = end
        elif char in '"\'':
            end = index + 1
            while end < length:
                if text[end] == '\\':
                    end += 2
                    continue
                if text[end] == char:
                    end += 1
                    break
                if text[end] == '\n':
                    break
                end += 1
            out.append(text[index:end])
            index = end
        elif text[index:index + 2] == '//':
            end = text.find('\n', index)
            index = length if end < 0 else end
        elif text[index:index + 2] == '/*':
            end = text.find('*/', index + 2)
            index = length if end < 0 else end + 2
        else:
            out.append(char)
            index += 1
    return "".join(out)


def defaults(text):
    """The anonymous object's name = value pairs, with the quotes and whitespace taken off."""
    found = {}
    for key, value in PAIR.findall(text or ""):
        found[key] = value.strip().strip('"')
    return found


def normalise(name, template, raw):
    """One route as a comparable tuple, in ASP.NET Core's spelling.

    MVC 5 says `{name}` plus `name = UrlParameter.Optional`; Core says `{name?}` and nothing in
    the defaults. Core also writes a default INTO the template - `{controller=Home}` - so the
    default route's template is reduced the same way from both sides.
    """
    values = defaults(raw)

    optional = {key for key, value in values.items() if value.endswith("UrlParameter.Optional")}
    for key in optional:
        del values[key]

    segments = []
    for segment in template.split("/"):
        match = re.fullmatch(r"\{(\w+)(?:=([^}]*))?\??\}", segment)
        if not match:
            segments.append(segment)
            continue
        key, inline = match.group(1), match.group(2)
        if inline is not None:
            values.setdefault(key, inline)
        # Optional three ways, and they mean the same thing: said so in the template, named in
        # UrlParameter.Optional, or given a default - a segment with a default can be left out.
        segments.append("{%s?}" % key if (segment.endswith("?}") or key in optional
                                          or key in values) else segment)

    # An empty-string default is how MVC 5's "Root" route spells "no id"; Core has no such
    # route because its default template makes the same segments optional.
    values = {k: v for k, v in values.items() if v != ""}
    return name.lower(), "/".join(segments), tuple(sorted(values.items()))


# class Foo : ApiController   /   class Foo: ApiController
API = re.compile(r"class\s+(\w+)\s*:\s*ApiController\b")
PORT_SOURCES = "port-sources.props"

_api_files = []


def api_count():
    return len(_api_files)


def api_controllers_not_linked():
    """Every ApiController in the repository, and whether port-sources.props compiles it."""
    listed = subprocess.run(["git", "-C", str(ROOT), "ls-files", "*.cs"],
                            capture_output=True, text=True)
    if listed.returncode != 0:
        return []

    # XML comments, not C# ones: read() takes out // and /* */, and an entry commented out of
    # an msbuild file is <!-- ... -->. Left in, a commented-out Compile Include would read as
    # linked - the same mistake the route half of this script made and was caught making.
    linked = re.sub(r"<!--.*?-->", "", read(PORT_SOURCES), flags=re.DOTALL)
    missing = []
    del _api_files[:]
    for name in listed.stdout.split():
        if "/obj/" in name or "/bin/" in name:
            continue
        try:
            text = read(name)
        except OSError:
            continue
        if not API.search(text):
            continue
        _api_files.append(name)
        # The props file spells paths with backslashes and a leading ..\.
        if name.replace("/", "\\") not in linked:
            missing.append(name)
    return missing


def show(route):
    name, template, values = route
    return '%-13s %-34s %s' % (name, template or '""',
                               ", ".join("%s=%s" % pair for pair in values) or "-")


def main():
    # These scripts find their subjects through git, so that a file which has been moved or
    # renamed stops the check instead of quietly removing its own subject. tools/check-the-
    # checks.sh blinds git and requires every check to fail; one that reads a hardcoded path
    # passes blinded and proves nothing.
    listing = subprocess.run(["git", "-C", str(ROOT), "ls-files", LEGACY, PORT],
                             capture_output=True, text=True)
    if listing.returncode != 0:
        # Reported rather than raised: check-the-checks.sh runs every check with git blinded
        # and requires a failure, and a traceback does not count as one.
        print("git ls-files failed, so this check cannot find its subjects: %s"
              % listing.stderr.strip(), file=sys.stderr)
        return 2
    tracked = listing.stdout.split()
    missing = [name for name in (LEGACY, PORT) if name not in tracked]
    if missing:
        print("not in the repository: %s" % ", ".join(missing), file=sys.stderr)
        return 2

    legacy_text, port_text = read(LEGACY), read(PORT)

    # Only the routes RegisterRoutes declares. Reading the whole file would pick up a MapRoute
    # written in a comment or an area registration somewhere else in it.
    start = legacy_text.index("public static void RegisterRoutes")
    end = legacy_text.index("protected void Application_End", start)
    legacy = [normalise(*m) for m in MVC.findall(legacy_text[start:end])]
    port = [normalise(*m) for m in CORE.findall(port_text)]

    if not legacy or not port:
        print("found %d route(s) in %s and %d in %s - one of the patterns stopped matching"
              % (len(legacy), LEGACY, len(port), PORT), file=sys.stderr)
        return 2

    # MVC 5's "Root" route maps "" to Home/Index. Core's default template does that already,
    # with {controller=Home}, so there is nothing for it to correspond to.
    legacy = [r for r in legacy if r[1] != ""]

    if legacy == port:
        unlinked = api_controllers_not_linked()
        if unlinked:
            print("%d Web API controller(s) the live site serves and the port does not compile:"
                  % len(unlinked))
            print("")
            for name in unlinked:
                print("  " + name)
            print("")
            print("Web API is a second pipeline on .NET Framework, so these are URLs with no entry")
            print("in the route table above. Link the file in port-sources.props; nothing else is")
            print("needed, because attribute routing is already on.")
            return 1

        print("%d route(s), declared the same way in both route tables, and %d Web API "
              "controller(s) linked into the port" % (len(port), api_count()))
        return 0

    print("the two route tables do not agree:")
    print("")
    print("  %-30s %s" % (LEGACY, PORT))
    for index in range(max(len(legacy), len(port))):
        left = show(legacy[index]) if index < len(legacy) else "-"
        right = show(port[index]) if index < len(port) else "-"
        print("  %s  %s" % ("ok " if left == right else "->", left))
        if left != right:
            print("      %s" % right)
    print("")
    print("A route only in Global.asax is a URL the live site serves and the port answers 404")
    print("for - and the query-string form of the same request still works, so nothing else")
    print("here will tell you. Add it to %s, in the same position." % PORT)
    return 1


if __name__ == "__main__":
    sys.exit(main())
