# Offline effects study

```powershell
dotnet run --project tools/testing/EffectsLab/EffectsLab.csproj -c Release
```

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
