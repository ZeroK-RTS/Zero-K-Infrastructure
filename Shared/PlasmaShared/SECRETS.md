# Secrets in this repository

## How it is supposed to work, and mostly does

Secrets live in the database, in `MiscVar`, and are read through `ZkData/Secrets.cs`:

```csharp
public string GetNightwatchPassword()     => MiscVar.GetValue("NightwatchPassword");
public string GetSteamWebApiKey()         => MiscVar.GetValue("SteamWebApiKey");
public string GetGithubHookKey()          => MiscVar.GetValue("GithubHookKey");
public string GetSteamBuildPassword()     => MiscVar.GetValue("SteamBuildPassword");
public string GetGlacierSecretKey()       => MiscVar.GetValue("GlacierSecretKey");
public string GetNightwatchDiscordToken() => MiscVar.GetValue("NightwatchDiscordToken");
```

Nothing there is in source control, and a value can be changed without a deploy. **New secrets go
here.** Note `GetGithubHookKey` in particular: a GitHub credential, handled correctly. The repo
already knows how to do this.

## The one exception: `GlobalConst.CrashReportGithubToken`

`Shared/PlasmaShared/GlobalConst.cs` declares a live GitHub token as a compiled-in constant. It has
one consumer, `ChobbyLauncher/CrashReportHelper.cs`, which files crash reports with it.

It is assembled from two string literals rather than written as one, which is what a secret scanner
looks for. That is why it went four years without anybody noticing — not why it is still here. It is
still here because it was looked at in October 2026 and deliberately kept; see the decision below.

### Do not "fix" this by deleting the line

This is the trap, and it is worth being explicit because the change looks like a fix and is worse
than leaving it alone:

- The value stays in **git history**. `git log -p` on one file recovers it.
- Both this fork and upstream `ZeroK-RTS/Zero-K-Infrastructure` are **public**. Every fork and every
  clone carries its own copy, and rewriting history here would reach none of them.
- So you would end up with a token that is still valid, still published, and no longer visible to
  anyone reading the code — which removes the only thing currently prompting anyone to deal with it.

**Revoking the token at GitHub is the only thing that would close this.** Nothing done in this
repository substitutes for that — which is exactly why the decision below is to leave it rather than
to perform a change that would look like closing it.

### It cannot be closed from this fork

## Decided: kept, knowingly (2026-10-02)

**This was assessed with the repository's owner and the token is staying as it is.** The reasoning,
recorded so that it can be re-examined rather than re-litigated:

- The token can file issues in **one isolated repository** and reach nothing else. The exposure is
  therefore **issue spam in that repository**, not access to code, releases or any other account.
- It is compiled into a client that ships to players, so it is public whatever the source does.
  Rotating and re-embedding would restart that clock rather than stop it - see "Why `MiscVar` is
  not available here" below - and the alternatives that would actually close it cost a new endpoint
  and a client release for a risk whose worst case is spam.

**What would change the assessment**, and is worth checking if either happens: the target
repository ceasing to be isolated, or the token's scopes widening beyond creating issues there.

The rest of this section is kept because it is the analysis behind that decision, and because the
same questions arise for the next secret somebody is tempted to compile in.

The token belongs to a GitHub account this repository does not control. Rotation is upstream's to
do — `ZeroK-RTS/Zero-K-Infrastructure` — and this fork can only carry the note.

If it is ever rotated, the order that avoids a window where crash reporting is broken:

1. **Issue a replacement**, scoped as narrowly as filing crash reports needs. If the current one is a
   classic PAT with broad scopes, make the replacement fine-grained and limited to the one repository.
2. **Decide where the new value lives, and do not assume it is `MiscVar`.** It cannot be, for this
   consumer — measured, see "Why `MiscVar` is not available here" below. The client should probably
   not hold a token at all.
3. **Ship it**, so the consumer uses the new value.
4. **Revoke the old token.** This is the step that matters; everything before it is preparation.

Check the old token's *last used* date before revoking — if it has been used from somewhere
unexpected, that is worth knowing. Revoking first and accepting broken crash reports in the meantime
is also a defensible choice; it is a trade, not a mistake.

### Why `MiscVar` is not available here

This section used to say the fix was "small and mechanical - move that consumer onto the
`Secrets`/`MiscVar` pattern". **That is not possible, and the reason is worth stating precisely**
(measured 2026-10-02):

- `ChobbyLauncher` builds `Zero-K.exe`, a `WinExe`. It is the launcher that ships to players.
- It references `PlasmaDownloader` and `PlasmaShared` and **not `ZkData`**, which is where both
  `MiscVar` and `Secrets` live.
- `MiscVar.GetValue` opens a `ZkDataContext` - a database connection. A player's machine has none
  and must never be given one.
- Nothing server-side files GitHub issues today: the only `GitHubClient` in the repository is in
  that one client file.

So the pattern every other secret here uses is simply not reachable from the one consumer that
needs this one.

**The harder point, which rotation alone does not touch: a credential shipped inside a desktop
application is public.** It is on every player's disk. Splitting it across two string literals
delayed a scanner, not a reader. Issuing a new token and compiling it in the same way restarts the
clock rather than stopping it - the replacement is extractable the day it ships.

That makes this a design decision rather than a code move, and there are three honest answers:

| | | |
|---|---|---|
| **Proxy it** | The client POSTs the crash report to the site; the site files the issue with a token that lives in `MiscVar` and never leaves the server. | The only option where the token stops being public. Costs a new endpoint, a decision about abuse of it, and a client release. |
| **Re-embed, scoped hard** | A fine-grained PAT that can create issues in the one crash-report repository and do nothing else. | Trivial, matches what is there now, and still public - but the worst case becomes issue spam in one repository rather than whatever the current scopes allow. |
| **Stop filing from the client** | Crash reports go to the site and a human or a job files what is worth filing. | No token on any player's machine at all. Changes the workflow, not just the plumbing. |

The first and the third are real fixes; the second is a stopgap that resets the clock, which may
still be the right call if the scopes are narrow enough and a release is expensive.

### What was done here instead

- `tools/check-secrets.py` (2026-10-02) fails when a **new** token-shaped literal appears, and joins
  adjacent string literals before judging them, so the concatenation that hid this one does not hide
  the next. It does **not** fail on this token: it reports it as outstanding on every run, because
  deleting the line would hide the problem rather than fix it.
- The consumer has deliberately **not** been moved. There is nowhere correct to move it to until the
  decision above is made, and moving it would make the problem look solved while the published value
  stayed live.

## A note on handling

While writing this, the token's value was deliberately not read, printed or moved; two attempts to
inspect it were refused, which was correct. Documenting an exposure does not require reproducing it,
and this file contains no part of the value.

## See also

- `ZkData/Secrets.cs` — the pattern to follow.
- `Zero-K.info/HOSTING.md` — the two `Web.config` input-filtering settings, and what is known about
  whether they can be restored.
