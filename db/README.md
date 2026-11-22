# Local database

A runnable SQL Server for development, and the groundwork for automated tests that need
real data rather than fixtures.

## Why a container

The application's Local mode is configured for SQL Server **LocalDB**
(`(LocalDb)\MSSQLLocalDB`), which only exists on Windows. This container is the
cross-platform stand-in: the same engine over TCP, so one connection string works from
Windows, Linux and CI.

## Start it

    docker compose -f db/docker-compose.yml up -d

If a harness ever fails with a SQL Server connection error, run

    ./db/require-db.sh

It says which of the four things is actually wrong - no container, a stopped one, one that is
still starting, or a missing database - and how to fix that one. `tools/render-view.sh`,
`tools/run-host.sh`, `db/compare-ratings.sh` and `db/efcore-schema.sh` call it themselves, so
they now stop with that message instead of a stack trace. They skip it when
`ZK_CONNECTION_STRING` is set, since the database is then not theirs to check.
    ./db/wait-for-db.sh

Listening on `127.0.0.1:14330` - deliberately not 1433, so it cannot collide with a real
SQL Server on the machine. The SA password is a local development secret and is committed
on purpose; the container holds no production data and is bound to loopback.

## Give the application the database

The connection string is hard-coded per mode in `Shared/PlasmaShared/GlobalConst.cs`.
`ZK_CONNECTION_STRING` overrides it, and is read after the mode is applied:

    export ZK_CONNECTION_STRING="$(./db/connection-string.sh)"

## Create the schema

    ./db/dbsetup.sh latest

That builds and runs `db/DbSetup`, a small EF6 migration runner, under mono in Docker. It
applies all 117 migrations and seeds forum categories, a resource row and `LocalSeed`,
giving a working empty database with **no personal data in it at all**.

An earlier version of this file said schema creation needed Windows, because EF6
migrations run on .NET Framework. That turns out to be wrong: mono runs them fine against
SQL Server 2022, in both directions. Verified - `./db/dbsetup.sh status` reports
`applied: 117, pending: 0`.

    ./db/dbsetup.sh status     # what is applied, what is pending
    ./db/dbsetup.sh list       # the most recent migrations
    ./db/dbsetup.sh to <id>    # migrate up OR down to a given migration

## Load real data

See `db/dumps/README.md` for where dumps go, then:

    ./db/load-bcp.sh --all

**The dump is behind this repository's migrations, and the load will fail if you skip
this.** The `.bcp` files are SQL Server native format, which is positional and
type-exact, so a table whose columns have changed since the dump was taken will not load.
`AddPwAttackCharges` added two columns to `Accounts` and `AddStructureBlockFlags` added
two to `StructureTypes`, both in April 2026, and the live database does not have them.

So the sequence is: roll the schema back to the dump's level, load, then migrate forward.

    ./db/dbsetup.sh to 202404060935038_AddPopularMapsFraction
    ./db/load-bcp.sh --all
    ./db/dbsetup.sh latest

Migrating forward afterwards fills the new columns from their defaults. Loading roughly
2 GB takes about three and a half minutes here, and gives a 6 GB database:

| Table | Rows |
|---|---|
| SpringBattlePlayers | 8,646,551 |
| AccountBattleAwards | 7,718,096 |
| SpringBattleBots | 3,245,290 |
| SpringBattles | 2,250,838 |
| Accounts | 307,045 |
| AccountRatings | 58,373 |
| ResourceContentFiles | 51,448 |
| Resources | 45,991 |

## Stop it, and start over

    docker compose -f db/docker-compose.yml down        # keeps the data
    docker compose -f db/docker-compose.yml down -v     # deletes it

## The schema snapshot

`db/schema/schema.txt` records the schema the EF6 migrations produce - 91 tables, 746
columns, 267 indexes, 163 foreign keys - in a sorted, normalised form that diffs cleanly.

    ./db/dump-schema.py            # regenerate it
    ./db/dump-schema.py --check    # compare a live database against it

It is deliberately not SQL Server's own scripting output, which carries generated
constraint names and storage settings that change between runs and versions. It records
what a model has to get right and nothing else.

Two uses. It is the contract the EF Core port must reproduce
(`ZkData/EFCORE-MIGRATION.md`), and it turns schema drift into a diff on a pull request -
CI checks it on every one. The live database being behind this repository is what made the
BCP dump refuse to load, and nothing in the repository recorded that; this is the fix for
that class of surprise.

If a migration changes the schema on purpose, regenerate and commit the snapshot in the
same pull request.

## The EF Core port, measured against it

    ./db/efcore-schema.sh --check    # does the EF Core model build this same schema?
    ZK_CONNECTION_STRING="$(DB_NAME=zk_test ./db/connection-string.sh)" \
        ./tools/dotnet.sh run --project ZkData.Core -- read   # can it read this data?

    ZK_CONNECTION_STRING="$(DB_NAME=zk_test ./db/connection-string.sh)" \
        ./tools/dotnet.sh run --project ZkData.Core -- write  # and write to it?

Three questions, and none implies the next - a mapping can name the wrong
column consistently, which builds a schema that matches nothing and reads nothing, but
diffs clean against itself. The first command builds a database from the EF Core model into
`zk_efcore` and diffs it against the snapshot; the remaining difference is committed as
`db/schema/efcore-gap.txt`, so it fails when the gap changes in either direction. The second
selects from every mapped table and runs the queries production issues. The third inserts,
updates and deletes inside a transaction it always rolls back, checking that EF6's save
hooks and annotation validation - neither of which EF Core does by itself - still happen;
the fixture is byte-identical afterwards. CI runs all three.

There is no .NET SDK on the build machines or on most laptops here, so `tools/dotnet.sh`
runs it from a container with the repository mounted.

A fourth question sits on top of those three, and it is the one the plan names as the real
risk of an EF6 → EF Core port:

    ./db/compare-ratings.sh    # do both stacks compute the same ratings?

It runs the Whole History Rating pipeline twice against the same fixture — once on .NET
Framework over EF6, once on .NET 9 over EF Core — and diffs the result. Both runs use the
same pipeline source, linked rather than copied, so the only difference between them is the
data layer. WHR is iterative and order-sensitive: a LINQ translation that quietly started
returning rows in a different order shows up here as a rating that moved, and nowhere else.
CI runs it on every pull request.

## The committed test fixture

`db/fixture/fixture.sql` is a small, anonymised slice of the real database that **is**
committed - unlike anything in `db/dumps/`. About 60 KB, loads in a second:

    DB_NAME=zk_test ./db/load-fixture.sh

30 accounts, 67 resources, 150 battles, 375 player rows, 57 ratings, and - from
`db/fixture/forum-seed.sql`, which is hand-written rather than generated - two forum
categories. The selection is the
30 most active accounts since 2024 and the battles in which every non-spectating player is
one of them, which gives a densely connected set - isolated players never converge, so a
scattered sample would be useless for rating tests. The result has a real skill spread:
one account with 85 battles and 59 wins, another with 29 and 4.

**Why a second, hand-written file.** `make-fixture.py` slices the five tables the rating
pipeline needs, and the forum is not one of them - so a database built from nothing had no
categories at all, while the site's front page is a forum page. That went unnoticed for as
long as the host harness only ran on laptops whose test database had collected categories
from earlier work; its **first run in CI**, against a database built from scratch, threw
`Sequence contains no elements` on the first query it made. There is nothing real to
anonymise in two category names, so the seed is written out rather than generated.

**What was removed.** Every identifier is renumbered from 1, so nothing points back at a
real account, battle or map. Names become `player01`..`player30`, maps `test_map_N`,
battles `Test battle N`. E-mail addresses, password hashes, Steam IDs and names, countries,
avatars, aliases, special notes, public keys, lobby versions, purchased DLC, replay file
names and map author names are dropped entirely, as are the clan and faction links. Login
timestamps are flattened to fixed dates.

**What was kept**, because it is the shape the rating code reads: who played whom, when,
for how long, who won, team numbers, Elo changes, and map proportions. Battle start times
are real, because WHR indexes ratings by day and the spacing matters.

Regenerate it from a loaded database with `./db/make-fixture.py`. The anonymisation is
done in SQL inside that script, in one place, so it can be read and audited.

## The database tests

`Tests.Database` exercises the Whole History Rating pipeline end to end against the
fixture - the real `RatingSystems.Init()` entry point the website calls at startup, not
just the arithmetic.

    docker compose -f db/docker-compose.yml up -d && ./db/wait-for-db.sh
    ZK_CONNECTION_STRING="$(DB_NAME=zk_test ./db/connection-string.sh)" ./db/dbsetup.sh latest
    DB_NAME=zk_test ./db/load-fixture.sh
    ./db/run-db-tests.sh

Nine tests, and the pipeline itself takes under a second on the fixture. Pass a substring
to run a subset: `./db/run-db-tests.sh Predicted`.

CI runs exactly these four commands - `.github/workflows/test_database.yml` - on every
pull request, and it is a required check for merging into `master`. It is heavier than the
portable suite, about two minutes on a cold runner, because it pulls SQL Server and mono
and builds the ZkData chain. It runs unconditionally rather than only on relevant paths:
a required check that gets skipped never reports, and the pull request then waits for it
forever.

It targets **net48** because `WholeHistoryRating`, `SpringBattle` and `ZkDataContext` are
Entity Framework 6, which does not run on .NET 9 - the blocker in
`ZkData/EFCORE-MIGRATION.md`. When that lifts, this project can move.

`OutputType` is `Exe` because mono has no working vstest runner, so `Program.cs`
discovers and runs the `[TestMethod]`s itself. The same methods are found normally by
Visual Studio and by the Windows CI runner; there is one set of tests either way.

**Two things worth knowing before reading the tests.** First, the harness deletes
`AccountRatings` before starting: those rows are the live pipeline's stored output,
carried along in the fixture, and leaving them would let `GetPlayerRating` answer from the
database cache without ever running the computation. Second, the fixture's battles are
historical, so no player is inside `GlobalConst.LadderActivityDays` and nothing is
*ranked* - rating and ranking are separate, and a test states that rather than working
around it.

The assertions are properties, not pinned numbers: ordering, convergence, bounds and rank
correlation. WHR's exact output depends on iteration count and ordering, so pinning Elo
would break on any tuning.

## Towards more automated tests

`Tests.Portable` deliberately has no database: it links pure source files and runs on
.NET 9 in about 150 ms, and that should stay true. That is why the database tests are a
separate project - a developer without Docker can still run the fast suite.

When that project is written, the shape that fits what is here: bring the container up in
CI as a service, build the schema with `db/dbsetup.sh latest`, and let each test run in a
transaction that is rolled back. No Windows runner is needed for the schema - `DbSetup`
runs under mono - though CI would have to build it, which takes a few minutes the fast
suite does not.

For tests that need data rather than an empty schema, a small redacted fixture is a better
fit than this 6 GB dump.
