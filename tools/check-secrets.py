#!/usr/bin/env python3
r"""No new secret is compiled into this repository.

    ./tools/check-secrets.py            fail on a secret-shaped literal that is not accounted for
    ./tools/check-secrets.py --update   re-record the accounted-for ones

Shared/PlasmaShared/SECRETS.md says where secrets go: the database, in MiscVar, read through
ZkData/Secrets.cs, so a value can change without a deploy and never enters source control. That
rule was written down and nothing held it.

It has already been broken once and survived four years. GlobalConst.CrashReportGithubToken is a
live GitHub token, committed upstream in 2021, **assembled from two string literals rather than
written as one** - which is what a secret scanner looks for, and is why nothing ever flagged it.
So this check looks for the concatenation too: a secret-shaped NAME assigned one or more adjacent
literals, joined before judging the length. Spelling a token in four pieces defeats GitHub's
scanning and does not defeat this.

**What it deliberately does not do is fail on that token.** Deleting the line would be worse than
leaving it - the value stays in git history, both this repository and upstream are public, and
every clone already has it; removing it from the source removes the only thing prompting anyone
to deal with it. SECRETS.md argues that at length. It is recorded here as outstanding, printed on
every run, and the only thing that closes it is revoking the token at GitHub, which is upstream's
to do.

Accepted findings are recorded by a hash of the line rather than the line itself, so the baseline
does not become a second published copy of the credential. Editing an accepted line changes the
hash and brings it back for review, which is the right default for a line that holds a secret.

publicKeyToken is excluded by name. It is an assembly strong-name identity, public by definition,
and it appears in every app.config in the repository - roughly a hundred lines that are not
secrets and never will be.
"""
import hashlib
import io
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
BASELINE = ROOT / "tools" / "secrets-baseline.txt"

EXTENSIONS = (".cs", ".sh", ".yml", ".yaml", ".config", ".json", ".ps1", ".mjs", ".js", ".py")
SKIP = ("packages/", "PlanetWars.old/", "tools/secrets-baseline.txt", "tools/check-secrets.py")

# A vendor's own prefix is a secret whatever it is called.
PREFIX = re.compile(r'"(ghp_|gho_|ghu_|ghs_|ghr_|github_pat_|xox[baprs]-|sk-|AKIA|AIza)[A-Za-z0-9_\-]{6,}')

# ...and a secret-shaped name assigned literals, however many pieces they are spelled in.
NAMED = re.compile(r'\b(\w*(?:token|secret|password|passwd|apikey|api_key|accesskey)\w*)\s*=\s*'
                   r'("(?:[^"\\]|\\.)*"(?:\s*\+\s*"(?:[^"\\]|\\.)*")*)', re.I)

LITERAL = re.compile(r'"((?:[^"\\]|\\.)*)"')
PUBLIC_KEY_TOKEN = re.compile(r'publicKeyToken\s*=', re.I)


def fingerprint(path, line):
    return hashlib.sha256(("%s\0%s" % (path, line.strip())).encode("utf-8")).hexdigest()[:16]


def findings():
    listed = subprocess.run(["git", "-C", str(ROOT), "ls-files"],
                            capture_output=True, text=True, check=True).stdout
    files = [f for f in listed.split()
             if f.endswith(EXTENSIONS) and not f.startswith(SKIP)]

    found = []
    for name in files:
        try:
            text = io.open(ROOT / name, encoding="utf-8-sig", errors="replace").read()
        except OSError:
            continue
        for number, line in enumerate(text.splitlines(), 1):
            if PUBLIC_KEY_TOKEN.search(line):
                continue

            why = None
            if PREFIX.search(line):
                why = "a vendor's own token prefix"
            else:
                match = NAMED.search(line)
                if match:
                    # Joined first: "9a81" + "5450a" + "0058" is one value, not three short ones.
                    value = "".join(LITERAL.findall(match.group(2)))
                    if len(value) >= 12 or (len(value) >= 8 and not re.fullmatch(r"[\w .,/:%\-]*", value)):
                        why = "%s assigned a literal of %d characters" % (match.group(1), len(value))
            if why:
                found.append((name, number, fingerprint(name, line), why))
    return files, found


def accepted():
    if not BASELINE.exists():
        return {}
    out = {}
    for line in io.open(BASELINE, encoding="utf-8").read().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        digest, _, reason = line.partition(" ")
        out[digest] = reason.strip()
    return out


def main():
    files, found = findings()

    if not files:
        print("found no tracked files to read - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    if "--update" in sys.argv:
        existing = accepted()
        lines = ["# Secret-shaped literals that are accounted for. One per line: a hash of the line,",
                 "# then why it is not a leak. The hash rather than the line itself, so this file does",
                 "# not become a second copy of anything. Editing an accepted line changes its hash and",
                 "# brings it back for review. See Shared/PlasmaShared/SECRETS.md.",
                 ""]
        for name, number, digest, why in found:
            lines.append("%s  %s" % (digest, existing.get(digest, "%s:%d - %s - REASON NEEDED" % (name, number, why))))
        io.open(BASELINE, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
        print("recorded %d finding(s) in %s" % (len(found), BASELINE.relative_to(ROOT)))
        return 0

    known = accepted()
    new = [f for f in found if f[2] not in known]

    # Counted and named in one line rather than printed in full on every run. These were printed
    # as "STILL OPEN" until 2026-10-02, when the exposure they describe was assessed with the
    # repository's owner and kept deliberately - see SECRETS.md. Repeating a closed decision as an
    # alarm on every build is how people learn to read past this check's output.
    decided = [known[f[2]] for f in found if f[2] in known and known[f[2]].startswith("ACCEPTED")]

    if new:
        print("%d secret-shaped literal(s) nothing accounts for:" % len(new))
        print("")
        for name, number, digest, why in new:
            print("  %s:%d" % (name, number))
            print("      %s" % why)
        print("")
        print("Secrets belong in MiscVar, read through ZkData/Secrets.cs - see")
        print("Shared/PlasmaShared/SECRETS.md. If this one is not a secret, record it with --update")
        print("and write down why.")
        return 1

    print("%d secret-shaped literal(s), all accounted for, in %d tracked file(s)"
          % (len(found), len(files)))
    if decided:
        print("   %d of them are a known exposure, assessed and accepted - tools/secrets-baseline.txt"
              % len(decided))
    return 0


if __name__ == "__main__":
    sys.exit(main())
