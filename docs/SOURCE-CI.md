# Source-only CI

`Source checks` runs on pushes to `main`, pull requests targeting `main`, and
manual dispatch. One standard `windows-latest` job uses a 15-minute timeout;
newer runs cancel stale runs for the same ref. It grants only `contents: read`,
does not persist checkout credentials, and has no secrets, publishing, release,
deployment, scheduled runs, artifact uploads, or shared caches. Official checkout
and .NET setup actions are pinned to the resolved commit hashes.
An ephemeral SDK selector in `tools/ci/global.json` keeps the job on the installed
.NET 8 SDK even when the hosted image also includes newer SDKs. It is not committed
and does not change the game's production SDK policy.

Run the equivalent gate in PowerShell 7 on Windows with .NET 8 installed:

```powershell
./tools/ci/Test-Source.ps1
```

Logs and the result receipt are written under ignored `artifacts/source-ci`.
Choose a new `-OutputDirectory` to retain previous local evidence. No game folder
or other local data is accepted as a gate input. The NuGet configuration contains
only the public nuget.org feed.

## Checks and inputs

- Verify all five committed wheel toolkit file hashes without loading native code.
- Build with warnings treated as errors and run the existing UnifiedLifecycle,
  ForceLifecycle, GameBindings, GameState, Landing, Lifecycle, and Support suites.
  These use committed test doubles and the redistributable managed wheel toolkit;
  they do not reference Unity, UMM, game assemblies, or private recordings.
- Build the pure production triple geometry core and run the existing 335-assertion
  geometry/protocol suite through `tests/SourceCi.Geometry`. That CI-only harness
  links the unchanged protocol and test source with the public, locked Json.NET
  13.0.4 dependency. Production Json.NET/game project references are unchanged.
- Generate clearly marked synthetic legacy/unified hash inventories and inert text
  payloads, then run the existing full disposable installer gate, including actual
  Windows batch entry points, package allowlist/metadata checks, migration,
  interruption recovery, changed-file refusal, rollback preflight and settings
  preservation. Synthetic `.dll` and game/UMM filename markers contain no executable
  code. Fixtures are never distributed or installed into a real game.

## Exclusions and interpretation

This is a source and installer-policy gate. It excludes production wheel/triple
mod builds, game-dependent camera/input/settings/telemetry suites, Unity Mono
hooks, compiled shipping-assembly verification, release package generation, and
private capture/replay corpora. Those still require the separately authorized
local release gate and its proprietary read-only inputs. No new release automation
or release credentials are introduced.

The gate does not scan all tracked files for private assets or certify that future
commits contain no personal settings, recordings, proprietary assemblies, or secrets.
Its selected inputs avoid those dependencies; maintainers must still review the
tracked publication diff for private data before pushing.

The CI-only Json.NET version does not certify the game's supplied serializer ABI.
Synthetic hashes cannot establish shipping binary identity or UMM/runtime loading.
Neither passing CI nor configured geometry establishes physical FFB, rendered
views, seams, frame timing, tearing, loader-version qualification, or UAT. The
metadata-only legacy bridge remains a second visible UMM row, and unknown installed
triple 0.3.12 payloads remain refused by migration.

Expect roughly 3–6 minutes on a fresh standard Windows runner, depending on SDK
and public package downloads; local timing is recorded in the result receipt.
The current public repository's standard hosted runner minutes are free under
[GitHub's billing policy](https://docs.github.com/en/billing/concepts/product-billing/github-actions).
No Actions artifacts or caches are stored by this workflow. This estimate is not
a measured hosted run; the workflow must be reviewed and published before one occurs.
