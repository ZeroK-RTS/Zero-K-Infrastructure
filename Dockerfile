# The ported Zero-K.info, on Linux, in a container.
#
#     ./tools/site-container.sh              build it, run it, check it answers
#     ./tools/site-container.sh --serve      leave it running on http://127.0.0.1:5200
#
# This is Phase 4 of the modernization plan - "Linux, containers, CI" - and it is worth being
# exact about what it proves and what it does not.
#
# PROVES: the ported site builds and serves real pages on Linux with no Windows anywhere. No
# IIS, no .NET Framework, no System.Drawing, no mono. Every controller and all 123 views are
# compiled into this image by the Razor SDK.
#
# DOES NOT PROVE: that it is ready to replace the live site. The entry point is ZeroKWeb.Host,
# which exists to serve pages for checking; the lobby server is not in it (see
# Zero-K.info/HOSTING.md), the two WCF endpoints have no home here, and the tripwires for
# unitsync and MonoTorrent throw rather than work. It needs a database - one this does not
# start, because db/docker-compose.yml already does.

# The asset build. Separate stage because it needs node and the runtime image needs neither node
# nor the 22 source files it reads - only the two it writes. See tools/build-assets.mjs.
FROM node:22-slim AS assets
WORKDIR /src
RUN apt-get update && apt-get install -y --no-install-recommends python3 && rm -rf /var/lib/apt/lists/*
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY tools/ ./tools/
COPY Zero-K.info/Scripts/ ./Zero-K.info/Scripts/
COPY Zero-K.info/Styles/ ./Zero-K.info/Styles/
COPY Zero-K.info/App_Start/ ./Zero-K.info/App_Start/
COPY ZeroKWeb.Core/Mvc5Compat/BundlingCompat.cs ./ZeroKWeb.Core/Mvc5Compat/
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# The whole tree, because ZeroKWeb.Host links sources out of Zero-K.info, ZeroKWeb.Core,
# ZkData, ZkData.Core and PlasmaShared rather than referencing built assemblies. .dockerignore
# keeps the build output and the 53 MB of images out of this layer.
COPY . .

# The same file run-host.sh generates, generated the same way - see tools/view-set.sh.
RUN ./tools/view-set.sh ZeroKWeb.Host/view-set.props

RUN dotnet publish ZeroKWeb.Host/ZeroKWeb.Host.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /publish ./bin

# The site's static content. ONLY these three directories: the Host's web root is the site
# SOURCE tree, and the rest of it is .cs files and Web.config. They land under a directory
# called Zero-K.info because that is what Program.FindSiteRoot looks for above the binary -
# the layout reads img/ through Server.MapPath, so an empty web root throws rather than
# rendering an unstyled page.
# The built bundles, beside the binary - which is where Program.cs looks for them, and why the
# container serves one script tag where the build tree serves eleven.
COPY --from=assets /src/build/assets/bundles ./bin/bundles

COPY Zero-K.info/img     ./Zero-K.info/img
COPY Zero-K.info/Scripts ./Zero-K.info/Scripts
COPY Zero-K.info/Styles  ./Zero-K.info/Styles

# 0.0.0.0, or nothing outside the container can connect. The checks still use 127.0.0.1:5199
# when this runs without --serve, which is why the variable is only read in serving mode.
ENV ZK_HOST_URLS=http://0.0.0.0:5199
EXPOSE 5199

ENTRYPOINT ["dotnet", "bin/ZeroKWeb.Host.dll"]
CMD ["--serve"]
