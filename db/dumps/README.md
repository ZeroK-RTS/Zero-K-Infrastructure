# Put database dumps here

**Nothing in this directory is committed.** `db/.gitignore` excludes it, and it must stay
that way: a dump of the live database contains real accounts - password hashes, e-mail
addresses, IP addresses, private messages - and this repository is public.

Accepted:

- **`.bcp`** - SQL Server native-format table exports, one file per table. This is what the
  shared Drive folder contains; its own `README.txt` sits beside them. Load with
  `./db/load-bcp.sh`, and read the schema-rollback note in `db/README.md` first.
- **`.bak`** (native backup) or **`.sql`** (script dump), optionally `.gz`/`.zip`. Load
  with `./db/restore.sh`.

Then:

    docker compose -f db/docker-compose.yml up -d
    ./db/wait-for-db.sh
    ./db/dbsetup.sh latest      # schema
    ./db/load-bcp.sh --all      # data

## Getting a dump

The shared folder is at
<https://drive.google.com/drive/folders/18hokFRvN3ylp16tcocv76ilwwqsCr8Ar>. Download it
with a browser - on the it-designers network the command line cannot reach it, the web
filter returns `403 Web Filter Violation` for `drive.google.com`.

## Before sharing a dump onward

A dump taken from live is personal data. If one has to be passed around, strip it first.
The columns that matter, by table:

| Table | Columns |
|---|---|
| `Accounts` | `Password`, `PasswordBcrypt`, `Email`, `Country`, `SteamID`, `InstallID` |
| `AccountIPs` | the whole table |
| `AccountUserIDs` | the whole table |
| `LobbyChatHistory` | private message text |
| `AbuseReports`, `Punishments` | free text about named people |

`ZkData/DbCloner.cs` already knows how to copy a database table by table in
foreign-key order - it is what the live-to-test clone uses, and it is the natural place
to build a redacting export on rather than writing one from scratch.
