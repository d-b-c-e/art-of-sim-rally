# Toolkit adoption follow-up

The user authorized completing the remaining review fixes and adopting shared
logic before the next release candidate. Public mod release remains 0.2.2;
working source prepares 0.2.3. No mod candidate is deployed or published here.

Toolkit v0.12.0 is released at commit
`6f9c662e330af1ab790ca291a799265e0a50ef8a`. Its official ZIP SHA-256 is
`0D870225CCFF53B8E7BAD1D5BEAA196EF29BFCF486D0F19BA6B790A8E5EE24E6`.
All five native/managed vendor files were compared directly with that archive.
The shared changes were coordinated with the dbce-wheel-mod-toolkit task and
shipped there; native lifecycle and force implementation stay upstream.

- KI-9: transactional Sync replaces the old partial-copy/version behavior.
- Native binding: the consumer removes all toolkit DllImports and uses the shared
  exact-module wrapper for FFB, shifter and direct reads. DLL alias remains
  UnityForceFeedback.dll; no second WheelFfb.dll is installed.
- Forces: local ForceCurve is a forwarding adapter to AxleForceCurve@1. The old
  generic SimLite profile remains different, so it is not silently substituted.
  The original Unity formula is frozen in the regression project.
- Device identity: explicit new wheel choices save GUIDs; missing/invalid saved
  identities fail closed. Legacy settings still load. Input readers are refreshed
  after FFB switching, and full shutdown releases independent input resources.
- Additional boundary fixes: invalid/nonfinite output is zeroed before integer
  conversion; disabling FFB zeroes it immediately; enabling after a disabled
  launch initializes it. Malformed bindings are rejected before array indexing.
- Capture schema 2 identifies the shared force library and records reset epochs.
  Replay runs separate original/toolkit histories and compares exact device
  integers. Schema 1 remains readable with its narrower per-row scope.

Later in the same session, the user clarified that recorder/playback must remain
development-only. Capture was removed from the production assembly and panel,
moved to a separate removable UMM probe, and controlled through external commands.
Replay now builds without game assemblies and supports a reusable recorded corpus.
Packaging inspects the production metadata and rejects recorder types/dependencies.
Additional review fixed telemetry recovery after a failed destination (KI-11),
with production-code loopback tests. See PRE-RELEASE-TESTING for current coverage.

Offline consumer coverage includes 125,000 generated steps (changing signals,
settings, clipping and resets), the unchanged 703-row reference, picker identity,
settings persistence, camera ownership/lifecycle, capture reset continuity,
negative replay/evidence cases and package/installer checks. Production builds
require zero warnings. The identified RC runner binds its report to one ZIP.

Upstream validation: 121 managed tests (80 FFB, 41 telemetry), 40 fake-native
assertions on each of x64 CoreCLR and x86 .NET Framework, 24 filesystem transaction
assertions, both native architectures and Windows/Linux CI. Native component 0.5
adds strict selection, retains read-slot GUIDs and recovers after panic. The x86
zero-force smoke passed on MOZA R12, including strict missing-GUID rejection and
panic/reinitialisation. The old smoke's ungated nonzero damper probe was corrected
before execution. No force-feel conclusion follows from zero-output checks.

Attended Unity/Mono loading, actual FFB delivery/feel, cameras, stage-start stutter,
telemetry consumers, persistence and recorder performance remain pending. Use
[TEST-DRIVE.md](../TEST-DRIVE.md) and the generated manual checklist for the exact
candidate. A matching force replay is not a deterministic game replay or measured
physical torque. Fanatec, PS5/T300 and RWD-snapback reports remain unverified.
