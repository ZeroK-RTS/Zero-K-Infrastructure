#!/usr/bin/env bash
# Captures the HTML MVC 5 emits for the site's Ajax call shapes, under mono, against the real
# System.Web.Mvc 5.2.3 that tools/build-website.sh already restores.
#
#     ./tools/ajax-ground-truth/capture.sh            print it
#     ./tools/ajax-ground-truth/capture.sh --update   record it in expected.txt
#
# The recorded file is what ZeroKWeb.Render checks the ported AjaxHelper against, so this is
# ground truth rather than transcription. Regenerate it only when MVC 5 itself changes.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
CACHE="${NUGET_CACHE:-$HOME/.cache/zk-mono-nuget}"
MVC="$CACHE/packages/microsoft.aspnet.mvc/5.2.3/lib/net45"
[ -f "$MVC/System.Web.Mvc.dll" ] || { echo "run tools/build-website.sh first to restore MVC 5" >&2; exit 2; }

if ! OUT=$(docker run --rm -v "$PWD":/src -v "$CACHE":/cache -w /src mono:6.12 bash -c '
  set -e
  MVC=/cache/packages/microsoft.aspnet.mvc/5.2.3/lib/net45
  WEBPAGES=/cache/packages/microsoft.aspnet.webpages/3.2.3/lib/net45
  # MVC 5 resolves System.Web.WebPages at runtime, from RouteCollectionExtensions.MapRoute
  # onwards, so both directories have to be on MONO_PATH or the capture dies before it prints.
  mkdir -p /tmp/asm && cp $MVC/*.dll $WEBPAGES/*.dll /tmp/asm/

  # PostLinkExtensions, with the one call to Html.AntiForgeryToken replaced by a literal.
  #
  # Not a convenience. Anti-forgery in MVC 5 reads web.config through
  # System.Web.Configuration, and mono outside a hosted application cannot supply that - it dies
  # in WebConfigurationHost.InitForConfiguration. The token is also RANDOM per request, so it
  # could never have been byte-compared in any case.
  #
  # What this captures is the form and button structure, which is the part a port gets wrong by
  # transcribing. That the token is PRESENT is asserted separately, in ZeroKWeb.Render: a port
  # dropping it would turn every one of these links into a CSRF hole and still match here.
  #
  # The substitution is string.Empty rather than a placeholder so that no quoting is needed:
  # the whole block lives inside a single-quoted bash -c, which is also why these comments
  # carry no apostrophes.
  sed "s/html.AntiForgeryToken().ToHtmlString()/string.Empty/" \
      Zero-K.info/AppCode/PostLinkExtensions.cs > /tmp/PostLinkExtensions.cs
  mcs -nologo -out:/tmp/capture.exe \
      -r:System.Web.dll -r:System.Web.Routing.dll -r:System.Core.dll \
      -r:System.ComponentModel.DataAnnotations.dll \
      -r:$MVC/System.Web.Mvc.dll -r:$WEBPAGES/System.Web.WebPages.dll \
      tools/ajax-ground-truth/Capture.cs /tmp/PostLinkExtensions.cs 2>&1
  MONO_PATH=/tmp/asm mono /tmp/capture.exe 2>&1
'); then
    echo "capture failed:" >&2
    printf '%s\n' "$OUT" >&2
    exit 1
fi
if [ "${1:-}" = "--update" ]; then
    printf '%s\n' "$OUT" > tools/ajax-ground-truth/expected.txt
    echo "recorded tools/ajax-ground-truth/expected.txt"
fi
printf '%s\n' "$OUT"
