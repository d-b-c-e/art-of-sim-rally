# Offline effects study

```powershell
dotnet run --project tools/testing/EffectsLab/EffectsLab.csproj -c Release
```

For a real schema-3 drive, use `--capture <capture-directory> <new-output-directory>`.
This first runs strict replay, then compares bounded toolkit envelopes with the
recorded steering at detected landings. Steering and event gains are independent;
both hypothetical torque directions are evaluated and clipping is reported.
Study gains/amplitudes are not wheel settings or calibrated impact measurements.
Output goes only to CSV/JSON. The first real study found the 5 ms pulse peak
undersampled by the game's roughly 60 Hz force updates; see
[the report](../../../docs/reviews/2026-09-11-bug-follow-up.md).

Calls only the pinned toolkit's managed `ImpactMixer` and `SignalSample`. No game,
Unity, native loader, wheel/device output or production dependency. A fresh output
directory retains the synthetic CSV, library hashes and JSON result. An existing
explicit output directory is refused.

This evaluates consumer design questions: independent steering/effect gains,
headroom attenuation, burst bounds, expired/stale/invalid samples and resets.
The toolkit owns the envelope implementation and its source tests. This program
does not implement a new force curve or collision detector.

See [the signal audit](../../../docs/research/2026-09-08-wheel-signals.md) for results
and limits. Test headroom values are not wheel tuning recommendations. The CSV
is synthetic and is not a real driving corpus for the release gate.
