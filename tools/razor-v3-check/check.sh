#!/usr/bin/env bash
# Asks MVC 5's own Razor parser whether it accepts a view.
#
#     ./tools/razor-v3-check/check.sh Zero-K.info/Views/Lobby/LobbyChatMessages.cshtml
#     ./tools/razor-v3-check/check.sh --all      every view the MVC 5 site serves
#
# The point is view EDITS. Until now a change to a .cshtml could be compiled on .NET 9 and only
# assumed to be valid on MVC 5, because mono ships no aspnet_compiler.exe. That is still true of
# compilation; this answers the narrower question of whether Razor v3 parses the syntax, which is
# what separates @helper (v3 only) from a construct both stacks accept.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
CACHE="${NUGET_CACHE:-$HOME/.cache/zk-mono-nuget}"
RAZOR="$CACHE/packages/microsoft.aspnet.razor/3.2.3/lib/net45"
[ -f "$RAZOR/System.Web.Razor.dll" ] || { echo "run tools/build-website.sh first to restore Razor 3" >&2; exit 2; }

# --all is what CI runs. A view edit made for the .NET 9 port can be compiled there and only
# ASSUMED to be valid on MVC 5, and MVC 5 is the build still serving the site - so an edit that
# ASP.NET Core accepts and Razor v3 does not would break production and pass every check here.
if [ "${1:-}" = "--all" ]; then
    # App_Code included when it exists: GridHelpers.cshtml lived there until it became partial
    # views, and a repository that still has one should still have it checked.
    set -- $(find Zero-K.info/Views Zero-K.info/App_Code -name '*.cshtml' 2>/dev/null | sort)
fi

docker run --rm -v "$PWD":/src -v "$CACHE":/cache -w /src mono:6.12 bash -c '
  set -e
  R=/cache/packages/microsoft.aspnet.razor/3.2.3/lib/net45
  W=/cache/packages/microsoft.aspnet.webpages/3.2.3/lib/net45
  mkdir -p /tmp/asm && cp $R/*.dll $W/*.dll /tmp/asm/
  mcs -nologo -out:/tmp/parse.exe -r:System.Web.dll -r:$R/System.Web.Razor.dll \
      -r:$W/System.Web.WebPages.Razor.dll -r:$W/System.Web.WebPages.dll \
      tools/razor-v3-check/ParseV3.cs 2>&1
  MONO_PATH=/tmp/asm mono /tmp/parse.exe '"$*"' 2>&1
'
