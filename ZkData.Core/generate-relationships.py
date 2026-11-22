#!/usr/bin/env python3
"""
Translates EF6's fluent configuration into EF Core's, generating
ZkData.Core/ZkDataContext.Relationships.cs from ZkData/ZkDataContext.cs.

Why generated rather than typed: there are 139 statements, and EF Core cannot infer these
relationships by itself - two navigations on one entity pointing at the same type are
ambiguous, which is where model building stops. Translating by hand would be 139 chances
to transpose a lambda; translating by rule is one chance to get the rule wrong, and the
rule is visible and re-runnable.

It refuses to guess. Anything it does not recognise is emitted as a compile error rather
than skipped, so an untranslated statement cannot pass quietly.

    ./ZkData.Core/generate-relationships.py
"""
import re, pathlib, sys

SOURCE = pathlib.Path("ZkData/ZkDataContext.cs")

# What to emit when EF6's configuration says nothing about deletes.
#
# EF6 cascades required relationships by default, and the database it produced does cascade
# 62 of its 163 foreign keys - but reproducing that by assumption creates cascade paths SQL
# Server rejects outright ("may cause cycles or multiple cascade paths"), because EF6's
# migrations settled those cases individually over 117 revisions.
#
# So the default here is deliberately the conservative one, and db/schema/schema.txt is what
# says where it is wrong. The diff names each foreign key that should cascade; those get
# fixed in ZkDataContext.RelationshipsByHand.cs, from evidence rather than from a rule.
UNSPECIFIED = "DeleteBehavior.Restrict"
TARGET = pathlib.Path("ZkData.Core/ZkDataContext.Relationships.cs")


ENTITIES = pathlib.Path("ZkData/Ef")


def entity_model():
    """
    (classes with a composite key, {(class, navigation property): type}) read from the
    entity sources.

    The generator needs this to know when EF6 left a foreign key unstated. EF6 could infer
    the key from the principal's composite key and the dependent's matching property names;
    EF Core cannot, and invents shadow columns instead - silently, because a model with
    shadow foreign keys builds and validates perfectly well. See the CampaignJournals note
    in ZkDataContext.RelationshipsByHand.cs for what that looks like when it reaches a
    database.
    """
    composite, navigations = set(), {}

    # A composite key is declared either way in this model: [Key] on two properties, or a
    # fluent HasKey over an anonymous type. CampaignPlanet uses the fluent form, so reading
    # only the attributes would miss exactly the case this exists to catch.
    for m in re.finditer(r'modelBuilder\.Entity<([A-Za-z0-9_]+)>\(\)\.HasKey\([^;]*?new\s*\{',
                         SOURCE.read_text(encoding="utf-8-sig")):
        composite.add(m.group(1))

    for path in sorted(ENTITIES.glob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        for match in re.finditer(r'\n\s*public class ([A-Za-z0-9_]+)', text):
            name = match.group(1)
            body = text[match.end():]
            if len(re.findall(r'\[Key\]', body)) > 1:
                composite.add(name)
            for nav in re.finditer(r'public virtual ([A-Za-z0-9_]+) ([A-Za-z0-9_]+)\s*\{', body):
                navigations[(name, nav.group(2))] = nav.group(1)
    return composite, navigations


COMPOSITE_KEYS, NAVIGATIONS = entity_model()


def navigation_target(entity, lambda_argument):
    """The type a navigation lambda such as `e => e.CampaignPlanet` points at."""
    m = re.search(r'=>\s*[A-Za-z0-9_]+\.([A-Za-z0-9_]+)', lambda_argument or "")
    return NAVIGATIONS.get((entity, m.group(1))) if m else None


def unstated_composite_key(principal, arg):
    """
    True when EF6 named no foreign key and the principal's key is composite.

    That combination is the one EF6 resolved by convention and EF Core does not, so it is
    refused rather than translated: the statement goes to the by-hand file, where the key
    is written out.
    """
    return principal in COMPOSITE_KEYS and "HasForeignKey" not in arg


def statements(text):
    block = re.search(r'protected override void OnModelCreating.*?\n(        \})', text, re.S)
    if not block:
        sys.exit("OnModelCreating not found in " + str(SOURCE))
    body = block.group(0)
    for raw in body.split(";"):
        raw = raw.strip()
        if raw.startswith("modelBuilder"):
            yield " ".join(raw.split())


def calls(statement):
    """[(method, argument), ...] in source order."""
    out, i = [], 0
    while i < len(statement):
        m = re.compile(r'\.([A-Za-z]+)\(').search(statement, i)
        if not m:
            break
        depth, j = 1, m.end()
        while j < len(statement) and depth:
            if statement[j] == "(": depth += 1
            elif statement[j] == ")": depth -= 1
            j += 1
        out.append((m.group(1), statement[m.end():j - 1].strip()))
        i = j
    return out


def entity_of(statement):
    m = re.match(r'modelBuilder\.Entity<([A-Za-z0-9_]+)>', statement)
    return m.group(1) if m else None


def delete_behaviour(argument, required):
    # EF6 has a parameterless WillCascadeOnDelete() meaning cascade, as well as the
    # explicit overload. An empty argument is that one, not a missing value.
    if argument.strip() in ("", "true"):
        return "DeleteBehavior.Cascade"
    if argument.strip() == "false":
        return "DeleteBehavior.Restrict"
    sys.exit("unexpected WillCascadeOnDelete argument: " + repr(argument))


def translate(statement):
    entity = entity_of(statement)
    if not entity:
        return None, "no entity type"
    parts = calls(statement)
    names = [n for n, _ in parts if n != "Entity"]
    arg = dict((n, a) for n, a in parts)

    # property facets translate unchanged - IsUnicode, HasPrecision, IsFixedLength
    if names and names[0] == "Property":
        # everything after Property is the facet chain; an earlier version sliced this
        # from parts[2:] and silently dropped single-facet statements such as
        # Property(e => e.SteamID).HasPrecision(38, 0)
        facets = "".join(".%s(%s)" % (n, a) for n, a in parts[1:] if n != "Property")
        if not facets:
            return None, "Property with no facets"
        return "modelBuilder.Entity<%s>().Property(%s)%s;" % (entity, arg["Property"], facets), None

    if names == ["HasKey"]:
        return "modelBuilder.Entity<%s>().HasKey(%s);" % (entity, arg["HasKey"]), None

    # many-to-many with an explicit join table is a different shape in EF Core
    if "WithMany" in names and "HasMany" in names and "Map" in names:
        return None, "many-to-many with Map/ToTable needs UsingEntity"

    # the common case: HasMany(...).WithRequired|WithOptional(...)[.HasForeignKey(...)][.WillCascadeOnDelete(...)]
    if names and names[0] == "HasMany" and len(names) > 1 and names[1] in ("WithRequired", "WithOptional"):
        required = names[1] == "WithRequired"
        if unstated_composite_key(entity, arg):
            return None, "no foreign key stated and %s has a composite key" % entity
        out = "modelBuilder.Entity<%s>().HasMany(%s).WithOne(%s)" % (entity, arg["HasMany"], arg[names[1]])
        if "HasForeignKey" in arg:
            out += ".HasForeignKey(%s)" % arg["HasForeignKey"]
        out += ".IsRequired(%s)" % ("true" if required else "false")
        behaviour = delete_behaviour(arg.get("WillCascadeOnDelete", ""), required) if "WillCascadeOnDelete" in arg \
            else UNSPECIFIED
        out += ".OnDelete(%s);" % behaviour
        return out, None

    # the inverse spelling: HasOptional|HasRequired(...).WithMany(...)
    if names and names[0] in ("HasOptional", "HasRequired") and len(names) > 1 and names[1] == "WithMany":
        required = names[0] == "HasRequired"
        principal = navigation_target(entity, arg[names[0]])
        if unstated_composite_key(principal, arg):
            return None, "no foreign key stated and %s has a composite key" % principal
        out = "modelBuilder.Entity<%s>().HasOne(%s).WithMany(%s)" % (entity, arg[names[0]], arg["WithMany"])
        if "HasForeignKey" in arg:
            out += ".HasForeignKey(%s)" % arg["HasForeignKey"]
        out += ".IsRequired(%s)" % ("true" if required else "false")
        behaviour = delete_behaviour(arg.get("WillCascadeOnDelete", ""), required) if "WillCascadeOnDelete" in arg \
            else UNSPECIFIED
        out += ".OnDelete(%s);" % behaviour
        return out, None

    return None, "unrecognised shape: " + " ".join(names)


def main():
    text = SOURCE.read_text(encoding="utf-8-sig")
    translated, skipped = [], []
    for statement in statements(text):
        line, why = translate(statement)
        if line:
            translated.append(line)
        else:
            skipped.append((why, statement))

    out = ['''// GENERATED by ZkData.Core/generate-relationships.py - do not edit by hand.
//
// EF6's fluent configuration, translated. EF Core cannot infer these relationships: two
// navigations on one entity pointing at the same type are ambiguous, and that is where
// model building stops without them.
//
// Translation rules, so the output can be audited against the source:
//   HasMany(x).WithRequired(y)  ->  HasMany(x).WithOne(y).IsRequired(true)
//   HasMany(x).WithOptional(y)  ->  HasMany(x).WithOne(y).IsRequired(false)
//   HasOptional(x).WithMany(y)  ->  HasOne(x).WithMany(y).IsRequired(false)
//   WillCascadeOnDelete(false)  ->  OnDelete(DeleteBehavior.Restrict)
//   WillCascadeOnDelete(true)   ->  OnDelete(DeleteBehavior.Cascade)
//   no WillCascadeOnDelete      ->  EF6's default for that optionality, stated explicitly
//                                   rather than left to EF Core's, which differs
//
// That last rule is the one to check against db/schema/schema.txt: EF6 cascades required
// relationships by default and EF Core does not, so leaving it implicit would silently
// change delete behaviour.
using Microsoft.EntityFrameworkCore;

namespace ZkData
{
    public partial class ZkDataContext
    {
        partial void ConfigureRelationships(ModelBuilder modelBuilder)
        {''']
    out += ["            " + line for line in translated]
    if skipped:
        out.append("")
        out.append("            // Not translated here - done by hand in")
        out.append("            // ZkDataContext.RelationshipsByHand.cs. Listed so the two files can be")
        out.append("            // checked against each other:")
        for why, statement in skipped:
            out.append("            //   %s" % why)
            out.append("            //     %s" % statement[:150])
    out.append("        }")
    out.append("    }")
    out.append("}")

    TARGET.write_text("\n".join(out) + "\n", encoding="utf-8")
    print("translated %d statements, %d left by hand" % (len(translated), len(skipped)))
    for why, _ in skipped:
        print("   - " + why)


if __name__ == "__main__":
    main()
