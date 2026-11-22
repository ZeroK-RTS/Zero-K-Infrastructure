#!/usr/bin/env python3
"""
Writes db/schema/schema.txt - a deterministic description of the schema the EF6
migrations produce.

Two jobs:

  * It is the contract the EF Core port has to reproduce. EF Core cannot read EF6's
    __MigrationHistory, so the port baselines the current schema instead
    (ZkData/EFCORE-MIGRATION.md); this file is what "the current schema" means, in a form
    that can be diffed rather than described.

  * It makes drift visible. The live database turned out to be behind this repository's
    migrations, which is why the BCP dump would not load until the schema was rolled back
    - and nothing in the repository said so. A committed snapshot turns that class of
    surprise into a diff on a pull request.

Usage:
    ./db/dump-schema.py            # write the snapshot
    ./db/dump-schema.py --check    # compare against the committed one, non-zero if different

Deliberately not raw SQL Server scripting output: that carries generated constraint names,
fill factors and other noise that changes between runs and versions. This is sorted,
normalised, and only records what a model has to get right.
"""
import json, os, subprocess, sys, pathlib, difflib

PASS = os.environ.get("MSSQL_SA_PASSWORD", "ZkLocal!Dev2026")
DB = (sys.argv[sys.argv.index("--db") + 1] if "--db" in sys.argv
      else os.environ.get("DB_NAME", "zk_test"))
OUT = pathlib.Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv \
    else pathlib.Path(__file__).parent / "schema" / "schema.txt"


def query(sql):
    cmd = ["docker", "exec", "zk-db", "/opt/mssql-tools18/bin/sqlcmd",
           "-S", "localhost", "-U", "sa", "-P", PASS, "-C", "-I", "-d", DB,
           "-y", "0", "-Q", "SET NOCOUNT ON; " + sql]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        sys.exit("sqlcmd failed:\n" + res.stdout + res.stderr)
    body = "".join(l.rstrip("\n") for l in res.stdout.splitlines()
                   if l.strip() and not l.startswith("JSON_") and not set(l.strip()) <= {"-"})
    return json.loads(body) if body else []


COLUMNS = """
SELECT t.name AS [table], c.name AS [column], ty.name AS [type],
       c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity,
       d.definition AS [default]
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints d ON d.object_id = c.default_object_id
ORDER BY t.name, c.name FOR JSON PATH;
"""

INDEXES = """
SELECT t.name AS [table], i.name AS [index], i.type_desc, i.is_unique, i.is_primary_key,
       i.has_filter, i.filter_definition,
       (SELECT STRING_AGG(CAST(col.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END AS nvarchar(max)), ', ')
               WITHIN GROUP (ORDER BY ic.key_ordinal)
        FROM sys.index_columns ic JOIN sys.columns col
             ON col.object_id = ic.object_id AND col.column_id = ic.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) AS [columns]
FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.type > 0
ORDER BY t.name, i.name FOR JSON PATH;
"""

FOREIGN_KEYS = """
SELECT OBJECT_NAME(fk.parent_object_id) AS [table],
       OBJECT_NAME(fk.referenced_object_id) AS [references],
       fk.delete_referential_action_desc AS [on_delete],
       fk.update_referential_action_desc AS [on_update],
       (SELECT STRING_AGG(CAST(pc.name AS nvarchar(max)), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
        FROM sys.foreign_key_columns fkc JOIN sys.columns pc
             ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
        WHERE fkc.constraint_object_id = fk.object_id) AS [columns]
FROM sys.foreign_keys fk
ORDER BY OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id),
         (SELECT STRING_AGG(CAST(pc.name AS nvarchar(max)), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
          FROM sys.foreign_key_columns fkc JOIN sys.columns pc
               ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
          WHERE fkc.constraint_object_id = fk.object_id)
FOR JSON PATH;
"""

# A database built straight from a model has no EF6 migration history, so this is
# optional rather than required - which is what makes the tool usable on both sides of
# the port comparison.
MIGRATIONS = """
IF OBJECT_ID('__MigrationHistory') IS NOT NULL
    SELECT MigrationId FROM __MigrationHistory ORDER BY MigrationId FOR JSON PATH;
ELSE
    SELECT TOP 0 '' AS MigrationId FOR JSON PATH;
"""


def width(col):
    """Type suffix that matters to a model: length, or precision/scale for decimals."""
    t, ml, p, s = col["type"], col.get("max_length"), col.get("precision"), col.get("scale")
    if t in ("varchar", "char", "varbinary", "binary"):
        return "(max)" if ml == -1 else "(%d)" % ml
    if t in ("nvarchar", "nchar"):
        return "(max)" if ml == -1 else "(%d)" % (ml // 2)
    if t in ("decimal", "numeric"):
        return "(%d,%d)" % (p, s)
    return ""


def render():
    cols, idx, fks, migs = query(COLUMNS), query(INDEXES), query(FOREIGN_KEYS), query(MIGRATIONS)

    out = [
        "# Schema produced by the EF6 migrations. GENERATED - run db/dump-schema.py.",
        "#",
        "# The contract the EF Core port has to reproduce, and the thing that makes schema",
        "# drift show up as a diff. See ZkData/EFCORE-MIGRATION.md.",
        "",
        "migrations applied: %d, through %s" % (len(migs), migs[-1]["MigrationId"] if migs else "(none - built from a model)"),
        "tables: %d, columns: %d, indexes: %d, foreign keys: %d" % (
            len({c["table"] for c in cols}), len(cols), len(idx), len(fks)),
        "",
    ]

    by_table = {}
    for c in cols:
        by_table.setdefault(c["table"], []).append(c)

    for table in sorted(by_table):
        out.append("TABLE " + table)
        for c in by_table[table]:
            bits = [c["type"] + width(c)]
            bits.append("NULL" if c.get("is_nullable") else "NOT NULL")
            if c.get("is_identity"):
                bits.append("IDENTITY")
            if c.get("default"):
                bits.append("DEFAULT " + c["default"])
            out.append("  %-34s %s" % (c["column"], " ".join(bits)))

        # ordered by the columns, not by the index name: names are generated and differ
        # between EF6 and EF Core, and sorting by them made identical schemas diff purely
        # because two tables listed the same indexes in a different order
        for i in sorted([x for x in idx if x["table"] == table],
                        key=lambda x: (x.get("columns") or "", bool(x.get("is_primary_key")))):
            kind = "PRIMARY KEY" if i.get("is_primary_key") else ("UNIQUE INDEX" if i.get("is_unique") else "INDEX")
            line = "  %s (%s)" % (kind, i.get("columns") or "")
            if i.get("has_filter"):
                line += " WHERE " + i["filter_definition"]
            if i.get("type_desc") != "NONCLUSTERED":
                line += "  [" + i["type_desc"].lower() + "]"
            out.append(line)

        for f in sorted([x for x in fks if x["table"] == table],
                        key=lambda x: (x["references"], x.get("columns") or "")):
            line = "  FOREIGN KEY (%s) -> %s" % (f.get("columns") or "", f["references"])
            if f.get("on_delete") != "NO_ACTION":
                line += " ON DELETE " + f["on_delete"]
            if f.get("on_update") != "NO_ACTION":
                line += " ON UPDATE " + f["on_update"]
            out.append(line)

        out.append("")

    return "\n".join(out)


def main():
    text = render()
    if "--check" in sys.argv:
        if not OUT.exists():
            sys.exit("No committed snapshot at %s - run db/dump-schema.py first." % OUT)
        committed = OUT.read_text(encoding="utf-8")
        if committed == text:
            print("schema matches the committed snapshot")
            return
        diff = list(difflib.unified_diff(committed.splitlines(), text.splitlines(),
                                         "committed", "database", lineterm="", n=2))
        print("\n".join(diff[:120]))
        if len(diff) > 120:
            print("... %d more diff lines" % (len(diff) - 120))
        sys.exit("\nThe database schema does not match db/schema/schema.txt.\n"
                 "If a migration changed it deliberately, run db/dump-schema.py and commit the result.")
    OUT.parent.mkdir(exist_ok=True)
    OUT.write_text(text, encoding="utf-8")
    print("wrote %s (%.1f KB)" % (OUT, OUT.stat().st_size / 1024))


if __name__ == "__main__":
    main()
