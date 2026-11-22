#!/usr/bin/env python3
"""The JSON endpoints that replaced the two WCF services must still serve their whole contract.

    ./tools/check-service-contracts.py

`ContentService.svc` and `MissionService.svc` are WCF and have no future on .NET 9. Their
replacements are two ordinary POST endpoints - /ContentService and /MissionService - that take a
request class and dispatch on its type:

    dynamic typed = request;
    return await Process(typed);

**`dynamic` is why this script exists.** The compiler cannot tell you that a request class has no
Process overload, that an overload returns the wrong response, or that an interface member stopped
being reachable. Any of those is a RuntimeBinderException or a wrong message on the wire, and the
callers are shipped game clients - ZKL, Chobby, the autoregistrator, the mission editor - which
cannot be redeployed with the server. A silent gap here is a client that breaks in the field.

Three things are asserted, all of them the sort a build would catch if the dispatch were static:

  1. Every `class X : ApiRequest<Y>` has a `Process(X)` overload in one of the implementations.
  2. Every member of IMissionService is called by MissionServiceImplementation. The six operations
     collapse into four request classes - DeleteMission and UndeleteMission share one, GetMission
     and GetMissionByID share another - so matching names one to one would be wrong. What is
     checked is that the implementation still REACHES each member.
  3. A `Process(X)` returning `Task<Z>` has Z equal to the Y that X declares. A handler returning
     the wrong response compiles and serializes happily, and the client reads a message it was not
     expecting.

None of the three has anything to report today, which is the point of writing it down now: the
replacement is complete, and this is what keeps it complete. The plan document listed "a
replacement for two public service contracts" as remaining design work long after it had been
built; a check says so in a way a document cannot.
"""
import io
import re
import subprocess
import sys

REQUEST = re.compile(r"public\s+class\s+(\w+)\s*:\s*ApiRequest<(\w+)>")
# The access modifier is OPTIONAL, and that is not a detail. ContentServiceImplementation writes
# its handlers as `async Task<X> Process(Y request)` with no modifier at all, so a pattern that
# required one matched none of the thirteen - and the return-type rule, which only looks at what
# this matches, then reported "no mismatches" having compared nothing. It passed by seeing
# nothing, inside the script whose whole subject is that.
HANDLER = re.compile(
    r"(?:(?:private|public|internal|protected)\s+)?(?:static\s+)?(?:async\s+)?"
    r"Task<(\w+)>\s+Process\s*\(\s*(\w+)\s+\w+\s*\)")
MEMBER = re.compile(r"^\s*(?!//)[\w<>\[\], ]+?\s+(\w+)\s*\(", re.MULTILINE)

IMPLEMENTATIONS = [
    "Zero-K.info/AppCode/ContentServiceImplementation.cs",
    "Zero-K.info/AppCode/MissionServiceImplementation.cs",
]
MISSION_IFACE_NAME = "IMissionService.cs"
MISSION_IMPL = "Zero-K.info/AppCode/MissionServiceImplementation.cs"


def tracked():
    listed = subprocess.run(["git", "ls-files"], capture_output=True, text=True, check=True)
    return [line for line in listed.stdout.splitlines() if line.endswith(".cs")]


def read(path):
    """Lenient on purpose: a few tracked files are not UTF-8, and skipping one would be a file
    that is not checked - which is the whole failure mode this repository keeps finding. The
    patterns below only ever match ASCII identifiers, so a replaced byte costs nothing."""
    with open(path, "rb") as handle:
        return handle.read().decode("utf-8-sig", errors="replace")


def requests(paths):
    """{request class: declared response class}, wherever they are declared."""
    found = {}
    for path in paths:
        try:
            text = read(path)
        except OSError:
            continue
        for match in REQUEST.finditer(text):
            found[match.group(1)] = match.group(2)
    return found


def handlers():
    """{request class: (response class, file)} from the implementations."""
    found = {}
    for path in IMPLEMENTATIONS:
        try:
            text = read(path)
        except OSError:
            continue
        for match in HANDLER.finditer(text):
            found[match.group(2)] = (match.group(1), path)
    return found


def mission_members(paths):
    """IMissionService's operations, and which the implementation reaches."""
    iface = next((p for p in paths if p.endswith(MISSION_IFACE_NAME)), None)
    if iface is None:
        return None, None
    body = read(iface)
    # Inside the interface only - the file also declares nothing else today, but saying so in
    # code is cheaper than relying on it.
    start = body.find("interface IMissionService")
    end = body.find("\n\t}", start) if start >= 0 else -1
    if end < 0:
        end = body.find("\n    }", start) if start >= 0 else -1
    members = set(MEMBER.findall(body[start:end] if start >= 0 and end > start else ""))
    impl = read(MISSION_IMPL)
    reached = {m for m in members if re.search(r"\bservice\.%s\s*\(" % re.escape(m), impl)}
    return members, reached


# A check that looks at nothing passes. Three lists here and each is guarded, because the quiet
# failure is not "no requests" but "no implementations" - every request would then look unhandled,
# which fails loudly, while an empty REQUEST list would pass in silence.
def main():
    paths = tracked()
    if not paths:
        print("found no tracked C# files - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    declared = requests(paths)
    if not declared:
        print("found no ApiRequest<> classes - the contract this checks is gone", file=sys.stderr)
        return 2

    served = handlers()
    if not served:
        print("found no Process overloads in the implementations - nothing serves the contract",
              file=sys.stderr)
        return 2

    failures = 0

    unhandled = sorted(set(declared) - set(served))
    for name in unhandled:
        print("%s has no Process overload - posting one is a RuntimeBinderException" % name)
    failures += len(unhandled)

    for name, response in sorted(declared.items()):
        if name not in served:
            continue
        returned, where = served[name]
        if returned != response:
            print("%s: Process(%s) returns %s, but %s declares ApiRequest<%s>"
                  % (where, name, returned, name, response))
            failures += 1

    members, reached = mission_members(paths)
    if members is None:
        print("found no %s - the mission contract this checks is gone" % MISSION_IFACE_NAME,
              file=sys.stderr)
        return 2
    if not members:
        print("read no members out of IMissionService - the shape this checks has changed",
              file=sys.stderr)
        return 2
    for name in sorted(members - reached):
        print("IMissionService.%s is not reached by %s" % (name, MISSION_IMPL))
        failures += 1

    if failures:
        print("\n%d gap(s) between a service contract and the endpoint that replaced it." % failures)
        print("The dispatch is dynamic, so none of these is a build error - the clients are")
        print("shipped game clients, and they find out in the field.")
        return 1

    print("both JSON endpoints serve their whole contract"
          " - %d request class(es), %d IMissionService member(s), responses matching"
          % (len(declared), len(members)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
