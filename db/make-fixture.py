#!/usr/bin/env python3
"""
Builds db/fixture/fixture.sql - a small, anonymised, committable slice of the database
for automated tests.

Run it against a loaded local database (see db/README.md):

    ./db/make-fixture.py

Selection: the 30 most active accounts since 2024, and 150 battles in which every
non-spectating player is one of them. That gives a densely connected set, which is what
rating tests need - isolated players never converge.

Anonymisation happens in SQL, below, so it can be read and audited in one place. Every
identifier is renumbered from 1, so nothing in the fixture points back at a real account,
battle or map.
"""
import json, subprocess, sys, datetime, pathlib

PASS = "ZkLocal!Dev2026"
DB = "zero-k_local"
OUT = pathlib.Path(__file__).parent / "fixture" / "fixture.sql"


def query(sql):
    """Runs a query that ends in FOR JSON and returns the parsed rows."""
    # -y 0 stops sqlcmd truncating the JSON at 256 characters. It cannot be combined
    # with -h -1, so the header and rule lines are filtered out here instead.
    cmd = ["docker", "exec", "zk-db", "/opt/mssql-tools18/bin/sqlcmd",
           "-S", "localhost", "-U", "sa", "-P", PASS, "-C", "-I", "-d", DB,
           "-y", "0", "-Q", "SET NOCOUNT ON; " + sql]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        sys.exit("sqlcmd failed:\n" + res.stdout + res.stderr)
    body = "".join(
        line.rstrip("\n") for line in res.stdout.splitlines()
        if line.strip() and not line.startswith("JSON_") and not set(line.strip()) <= {"-"})
    return json.loads(body) if body else []


def literal(v):
    if v is None:
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float)):
        return repr(v)
    return "N'" + str(v).replace("'", "''") + "'"


def insert_block(table, rows, identity):
    if not rows:
        return "-- %s: no rows\n" % table
    # FOR JSON PATH omits NULL values, so rows do not all carry the same keys. Take the
    # union in first-seen order: using only rows[0]'s keys would silently drop a column
    # that happens to be NULL in the first row but set in a later one.
    cols = []
    for r in rows:
        for c in r:
            if c not in cols:
                cols.append(c)
    lines = []
    if identity:
        lines.append("SET IDENTITY_INSERT [dbo].[%s] ON;" % table)
    # batched: SQL Server caps a VALUES list at 1000 rows
    for i in range(0, len(rows), 500):
        chunk = rows[i:i + 500]
        lines.append("INSERT INTO [dbo].[%s] (%s) VALUES" %
                     (table, ", ".join("[%s]" % c for c in cols)))
        lines.append(",\n".join(
            "  (" + ", ".join(literal(r.get(c)) for c in cols) + ")" for r in chunk) + ";")
    if identity:
        lines.append("SET IDENTITY_INSERT [dbo].[%s] OFF;" % table)
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- selection
# Rebuilt every run so the fixture is reproducible from the same source database.
SELECT = """
IF OBJECT_ID('fx_acc') IS NOT NULL DROP TABLE fx_acc;
IF OBJECT_ID('fx_bat') IS NOT NULL DROP TABLE fx_bat;
IF OBJECT_ID('fx_res') IS NOT NULL DROP TABLE fx_res;

SELECT TOP 30 p.AccountID, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, p.AccountID) AS NewID
INTO fx_acc
FROM SpringBattlePlayers p JOIN SpringBattles b ON b.SpringBattleID = p.SpringBattleID
WHERE b.StartTime >= '2024-01-01' AND p.IsSpectator = 0
GROUP BY p.AccountID ORDER BY COUNT(*) DESC, p.AccountID;

SELECT TOP 150 b.SpringBattleID, ROW_NUMBER() OVER (ORDER BY b.SpringBattleID) AS NewID
INTO fx_bat
FROM SpringBattles b
WHERE b.StartTime >= '2024-01-01' AND b.IsMission = 0 AND b.HasBots = 0
  AND b.PlayerCount BETWEEN 2 AND 8 AND b.Duration > 60
  AND NOT EXISTS (SELECT 1 FROM SpringBattlePlayers p WHERE p.SpringBattleID = b.SpringBattleID
                    AND p.IsSpectator = 0 AND p.AccountID NOT IN (SELECT AccountID FROM fx_acc))
  AND EXISTS (SELECT 1 FROM SpringBattlePlayers p WHERE p.SpringBattleID = b.SpringBattleID AND p.IsSpectator = 0)
ORDER BY b.SpringBattleID;

SELECT r.ResourceID, ROW_NUMBER() OVER (ORDER BY r.ResourceID) AS NewID
INTO fx_res
FROM Resources r
WHERE r.ResourceID IN (SELECT b.MapResourceID FROM SpringBattles b JOIN fx_bat f ON f.SpringBattleID = b.SpringBattleID WHERE b.MapResourceID IS NOT NULL
                       UNION
                       SELECT b.ModResourceID FROM SpringBattles b JOIN fx_bat f ON f.SpringBattleID = b.SpringBattleID WHERE b.ModResourceID IS NOT NULL);
SELECT 1 AS ok FOR JSON PATH;
"""

# ---------------------------------------------------------------- extraction
# Personal data is replaced here, not in post-processing, so what is kept is explicit.
ACCOUNTS = """
SELECT a2.NewID AS AccountID,
       'player' + RIGHT('00' + CAST(a2.NewID AS varchar), 2) AS Name,
       NULL AS Email, NULL AS Aliases, NULL AS Country, NULL AS Avatar,
       NULL AS SteamID, NULL AS SteamName, NULL AS PasswordBcrypt,
       NULL AS SpecialNote, NULL AS LastPubKey, NULL AS LobbyVersion, NULL AS PurchasedDlc,
       NULL AS ClanID, NULL AS FactionID, NULL AS LobbyID,
       CAST('2024-01-01T00:00:00' AS datetime) AS FirstLogin,
       CAST('2025-01-01T00:00:00' AS datetime) AS LastLogin,
       CAST('2025-01-01T00:00:00' AS datetime) AS LastLogout,
       CAST('2025-01-01T00:00:00' AS datetime) AS LastChatRead,
       a.IsBot, a.IsDeleted, a.Level, a.Xp, a.Rank, a.Cpu, a.MissionRunCount,
       a.CanPlayMultiplayer, a.DevLevel, a.AdminLevel, a.HideCountry, a.HasKudos,
       a.IsTourneyController, a.HasVpnException, a.ForumTotalUpvotes, a.ForumTotalDownvotes,
       a.VotesAvailable, a.PwAttackCharges, a.PwLastChargeChange,
       a.PwDropshipsProduced, a.PwDropshipsUsed, a.PwBombersProduced, a.PwBombersUsed,
       a.PwMetalProduced, a.PwMetalUsed, a.PwWarpProduced, a.PwWarpUsed, a.PwAttackPoints
FROM Accounts a JOIN fx_acc a2 ON a2.AccountID = a.AccountID
ORDER BY a2.NewID FOR JSON PATH;
"""

RESOURCES = """
SELECT r2.NewID AS ResourceID,
       'test_map_' + CAST(r2.NewID AS varchar) AS InternalName,
       NULL AS AuthorName, NULL AS TaggedByAccountID, NULL AS ForumThreadID,
       NULL AS MissionID, NULL AS RatingPollID, NULL AS MapPlanetWarsIcon,
       NULL AS MapSpringieCommands, NULL AS RapidTag, NULL AS MapTags,
       r.TypeID, r.MapWidth, r.MapHeight, r.MapSizeSquared, r.MapSizeRatio,
       r.MapIsAssymetrical, r.MapHills, r.MapWaterLevel, r.MapIs1v1, r.MapIsTeams,
       r.MapIsFfa, r.MapIsChickens, r.MapIsSpecial, r.MapFFAMaxTeams,
       r.MapRatingCount, r.MapRatingSum, r.MapSupportLevel,
       0 AS DownloadCount, 0 AS NoLinkDownloadCount,
       CAST('2024-01-01T00:00:00' AS datetime) AS LastChange
FROM Resources r JOIN fx_res r2 ON r2.ResourceID = r.ResourceID
ORDER BY r2.NewID FOR JSON PATH;
"""

BATTLES = """
SELECT b2.NewID AS SpringBattleID,
       'test-battle-' + CAST(b2.NewID AS varchar) AS EngineGameID,
       'Test battle ' + CAST(b2.NewID AS varchar) AS Title,
       NULL AS ReplayFileName, NULL AS ForumThreadID,
       h.NewID AS HostAccountID,
       m.NewID AS MapResourceID, g.NewID AS ModResourceID,
       b.StartTime, b.Duration, b.PlayerCount, b.HasBots, b.IsMission,
       b.EngineVersion, b.IsEloProcessed, b.WinnerTeamXpChange, b.LoserTeamXpChange,
       b.Mode, b.IsMatchMaker, b.ApplicableRatings, b.Rank
FROM SpringBattles b
JOIN fx_bat b2 ON b2.SpringBattleID = b.SpringBattleID
LEFT JOIN fx_acc h ON h.AccountID = b.HostAccountID
LEFT JOIN fx_res m ON m.ResourceID = b.MapResourceID
LEFT JOIN fx_res g ON g.ResourceID = b.ModResourceID
ORDER BY b2.NewID FOR JSON PATH;
"""

PLAYERS = """
SELECT b2.NewID AS SpringBattleID, a2.NewID AS AccountID,
       p.IsSpectator, p.IsInVictoryTeam, p.LoseTime, p.AllyNumber,
       p.EloChange, p.XpChange, p.Influence
FROM SpringBattlePlayers p
JOIN fx_bat b2 ON b2.SpringBattleID = p.SpringBattleID
JOIN fx_acc a2 ON a2.AccountID = p.AccountID
ORDER BY b2.NewID, a2.NewID FOR JSON PATH;
"""

RATINGS = """
SELECT a2.NewID AS AccountID, r.RatingCategory, r.Percentile, r.RealElo, r.Elo,
       r.EloStdev, r.IsRanked, r.LadderElo
FROM AccountRatings r JOIN fx_acc a2 ON a2.AccountID = r.AccountID
ORDER BY a2.NewID, r.RatingCategory FOR JSON PATH;
"""


def main():
    query(SELECT)
    accounts = query(ACCOUNTS)
    resources = query(RESOURCES)
    battles = query(BATTLES)
    players = query(PLAYERS)
    ratings = query(RATINGS)

    parts = ["""-- Zero-K test fixture. GENERATED - do not edit by hand; run db/make-fixture.py.
--
-- A small, anonymised slice of the real database, safe to commit and small enough to
-- load in a second. Every identifier is renumbered from 1, so nothing here points back
-- at a real account, battle or map. Names are player01..player%02d, e-mail addresses,
-- password hashes, Steam identities, countries, avatars, aliases, notes, public keys,
-- battle titles, replay file names and map names are all replaced or dropped.
--
-- What is kept is the shape the rating code cares about: who played whom, when, for how
-- long, who won, and the map proportions. %d accounts, %d battles, %d player rows.
--
-- Load into a database that already has the schema (db/dbsetup.sh latest):
--     ./db/load-fixture.sh
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DELETE FROM [dbo].[AccountRatings];
DELETE FROM [dbo].[SpringBattlePlayers];
DELETE FROM [dbo].[SpringBattles];
DELETE FROM [dbo].[Resources];
DELETE FROM [dbo].[Accounts];
""" % (len(accounts), len(accounts), len(battles), len(players))]

    parts.append(insert_block("Accounts", accounts, identity=True))
    parts.append(insert_block("Resources", resources, identity=True))
    parts.append(insert_block("SpringBattles", battles, identity=True))
    parts.append(insert_block("SpringBattlePlayers", players, identity=False))
    parts.append(insert_block("AccountRatings", ratings, identity=False))

    OUT.parent.mkdir(exist_ok=True)
    OUT.write_text("\n".join(parts), encoding="utf-8")
    print("wrote %s (%.1f KB)" % (OUT, OUT.stat().st_size / 1024))
    print("  accounts=%d resources=%d battles=%d players=%d ratings=%d"
          % (len(accounts), len(resources), len(battles), len(players), len(ratings)))


if __name__ == "__main__":
    main()
