#!/usr/bin/env bash
# Runs the .NET 9 SDK from a container, because nothing here installs it on the host.
#
#     ./tools/dotnet.sh build ZkData.Core/ZkData.Core.csproj
#     ZK_CONNECTION_STRING=... ./tools/dotnet.sh run --project ZkData.Core -- read
#
# The workload notice is silenced because it goes to STDOUT, and some of these commands
# have stdout that is data - `-- rate` prints a table meant to be diffed.
#
# --network host so the harness can reach the SQL Server on 127.0.0.1:14330 that
# db/docker-compose.yml starts; the NuGet cache is kept outside the repo so restores do
# not repeat and do not land in git status.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Written out rather than inlined: an earlier one-liner used ${VAR:-:ro}, which expands to the
# variable's VALUE when it is set, so the mount became /mnt/extra1 and the engine vanished.
MOUNT_SUFFIX=":ro"
[ -n "${ZK_DOTNET_MOUNT_RW:-}" ] && MOUNT_SUFFIX=""
CACHE="${ZK_NUGET_CACHE:-$HOME/.nuget/packages}"
mkdir -p "$CACHE"

# ZK_DOTNET_NAME names the container so a caller that starts a long-running server can stop it
# again: killing the `docker run` client does not reliably stop what it started, and a stray
# container keeps holding the port that the next run needs.
# ZK_DOTNET_MOUNT mounts one extra host directory read-only at /mnt/extra, for things that must
# NOT live in the repository - the Spring engine above all. It is ~42MB, it is not ours, and this
# checkout is synced by Synology Drive, so an engine dropped in the tree would be replicated and
# would keep reappearing after deletion.
exec docker run --rm -i ${ZK_DOTNET_NAME:+--name "$ZK_DOTNET_NAME"} \
    ${ZK_DOTNET_MOUNT:+-v "$ZK_DOTNET_MOUNT:/mnt/extra$MOUNT_SUFFIX"} \
    ${ZK_DOTNET_MOUNT:+-e ZK_UNITSYNC_DIR=/mnt/extra} \
    ${ZK_DOTNET_MOUNT:+-e SPRING_DATADIR=/mnt/extra} \
    --network host \
    -u "$(id -u):$(id -g)" \
    -v "$REPO:/repo" \
    -v "$CACHE:/nuget" \
    -e HOME=/tmp \
    -e NUGET_PACKAGES=/nuget \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    -e DOTNET_NOLOGO=1 \
    -e DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 \
    -e ZK_CONNECTION_STRING="${ZK_CONNECTION_STRING:-}" \
    -e ZK_BATTLE_CONTEXT_OUT="${ZK_BATTLE_CONTEXT_OUT:-}" \
    -e ZK_BATTLE_CONTEXT_IN="${ZK_BATTLE_CONTEXT_IN:-}" \
    -e ZK_RENDERED_VIEWS="${ZK_RENDERED_VIEWS:-}" \
    -e ZK_REQUIRE_FULL_RUN="${ZK_REQUIRE_FULL_RUN:-}" \
    -e ZK_SITE_URL="${ZK_SITE_URL:-}" \
    -e ZK_HARNESS_ENDPOINTS="${ZK_HARNESS_ENDPOINTS:-}" \
    -w /repo \
    mcr.microsoft.com/dotnet/sdk:9.0 \
    dotnet "$@"
