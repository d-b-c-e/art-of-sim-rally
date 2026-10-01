# Constant crash pulse — 2026-09-17 UTC

The owner preferred standalone method A (a finite constant-force pulse) over
the periodic crash effect, but found 40% insufficient. They requested a 50%
default, adjustment through 100%, and contributing the findings to the shared
toolkit so other games can use them. No public release was requested.

## Evidence and interpretation

The [RC4 investigation](2026-09-17-rc4-crash-feel.md) preserves the failed game
test and first standalone comparisons. The later log
`attended-effects-20260916-234046-57044.log` contains A40, B40, C40, A40. All four
returned S_OK and PLAYING. C retained PLAYING through three zero-steering
updates. DirectInput device/effect gain read back 10000; device state queries
returned E_FAIL and their flags are unusable. Focus loss restored original
autocenter successfully. These are API observations, not torque measurements,
Pit House settings, or proof of a universal periodic-driver defect.

Raw snapshot/hash: `results/attended-effects-40-percent-20260917/receipt.json`.
The observed preference is specific to this MOZA R12 and configuration. A and
B/C match nominal peak and duration, not impulse: A sustains the requested
magnitude while B/C fade. Higher strength does not guarantee unused headroom;
the wheel may saturate when steering and crash output mix.

## Standalone tester

Upstream `d6fac19380e208b06b01d15b4c9913c3ee57a9b3` changes the diagnostic to
default50 with manual levels 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100.
Both architecture builds and bounds/dispatch tests pass, including rejection
above 10000 native units. Duration remains 120 ms, with explicit clicks,
cooldown, focus stopping and game checks. The selector opens at 50.

The Desktop shortcut targets `results/attended-effects-d6fac19/attended_effects.exe`,
x64 SHA-256 `7D33FED6DE213D91724980ECEC128EF513009343A210031962570D1011FE80EE`.
The copied executable's read-only preflight passed. Prior copies are retained;
the old open tester was not terminated. Reopen the shortcut for the new levels.
The agent has not opened the GUI or applied force. Physical 50–100 results are
pending. Receipt: `results/attended-effects-d6fac19/consumer-receipt.json`.

## Game candidate

- Crash requests a positive-X 120 ms constant pulse through the shared toolkit;
  it does not repurpose, stop or retune ordinary steering.
- Crash strength defaults50 for new/missing settings, range0–100. Existing
  saved values and opt-outs remain. The feature stays experimental and off by
  default. Landing remains default5/range0–40 and its 25 Hz waveform is unchanged.
- Landing and crash have separate cached native handles, created only while
  idle. One logical impact owns output: strongest wins, crash wins ties, and
  the old handle must stop before replacement. No delayed suppressed events.
- Unsupported constant creation/play latches crash unavailable while preserving
  landing. Off/on while paused retries setup; failed stop releases both impact
  families. Device loss, focus loss, pause, restart and shutdown stop output.
- Default/migration, full-range proportional gain, malformed limits,
  cross-family replacement, partial capability failure, latency and lifecycle
  tests cover the consumer. The saved landing detector case must remain one
  event at row4230; steering arithmetic and motion telemetry do not change.

The cached runtime API creates an idle effect and sets its parameters before
starting. The original diagnostic creates A at its requested magnitude. The
command semantics match; equivalent physical feel still needs an attended
check of the exact packaged candidate alongside ordinary steering.

## Toolkit contribution and acceptance

The native implementation, optional managed API and ABI/lifecycle tests live
upstream, not in a consumer fork. Reusable findings include matched-peak versus
matched-impulse comparisons, the limits of Accepted/PLAYING/parameter echo,
independent steering/effect gains, nominal headroom, finite-driver duration and
one owner for overlapping impact cues. Other games need their own event signals
and attended tuning; this does not silently retune or migrate them.

Candidate packaging, exact toolkit pin, complete local gates and deployment
are recorded in [LOCAL-DEPLOYMENT](../LOCAL-DEPLOYMENT.md).
No test here constitutes physical acceptance or authorizes public publication.

The upstream contribution is pushed on `codex/finite-constant-bursts`, source
`358add53af220eb95bf606e240b230e23a6ea197`. Reusable findings live in
[knowledge/FFB-IMPACTS.md](https://github.com/d-b-c-e/dbce-wheel-mod-toolkit/blob/358add53af220eb95bf606e240b230e23a6ea197/knowledge/FFB-IMPACTS.md);
the optional API contract is `docs/CONSTANT-BURSTS.md` in that same revision.
Local package 0.15.0/native 0.8.0 has 46 exports; it is not an official release.
Upstream reports 121 managed assertions, 24 sync checks, 104+9+8+8 binding checks
per runtime and both architecture native fake-effect suites. These include
ordinary zero/nonzero steering updates preserving the constant effect, exact
finite-duration rejection, repeated restart and lifecycle release. Hardware
smoke harnesses were compiled only. The handoff receipt is
`E:/Source/dbce-wheel-mod-toolkit-constant/dist/LOCAL-HANDOFF.json`.

Toolkit ZIP SHA-256: `1676B31AFBB2273B23140D042BC311F1F523240E2661D2C61E7932F74DCFF6F6`.
Vendored native SHA-256: `A07DDF7E10ADBD016DB204324D5E035B951E21A7D8DD373405C91257EB7AD288`.
Managed FFB SHA-256: `6FC2BE9197D846A477C84E81D39DD420B324BD4B79B30CD54BB78567E270A72B`.

All 16 local gates passed for preliminary `0.2.6-rc.5`, source `c4d1a14`, including
both real corpus cases and 9,503 landing/crash assertions. That package was not
installed: the final documentation cross-check found old 5/40 crash instructions
in SETUP and the packaged README. These are corrected before building RC6;
the runtime and toolkit pin are unchanged. RC5 evidence is retained under
`results/rc-0.2.6-rc.5-66f35eacabf64e729d339647307700e0`.

All 16 local gates also passed for **0.2.6-rc.6**, clean source
`1cd7814ac20c647f176c57925875e6af47858e91`. Exact artifact installed at 05:05 UTC;
all six payloads and the second native copy match, with RC4 backup retained.
Settings, game assembly, UMM parameters and probe files remain byte-identical.
Owner crash stays on/19.52381 and landing on/20. The game remained closed;
no recording or physical output was started. Public stable remains 0.2.5.
The immutable artifact and gate/install receipt links are in LOCAL-DEPLOYMENT.
The earlier standalone focused-capture command used a nonexistent local path;
the RC gates use the real main-checkout corpus index and both pass. No capture
bytes were changed, and no failed command was counted as a test pass.
