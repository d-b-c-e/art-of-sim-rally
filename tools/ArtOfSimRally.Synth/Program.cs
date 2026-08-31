using System;
using System.Threading;
using ArtOfSimRally.Telemetry;

// Synthetic telemetry emitter: proves the READ side (SimHub, a dashboard, the
// probe in harness/) against a known-good pattern, with no game running.
//
// If a dashboard mirrors this pattern, the packet layout and the consumer are
// both fine, and any remaining weirdness is in what the mod feeds the encoder.
// If it does not, the problem is upstream of the game entirely.
//
//   dotnet run --project tools/ArtOfSimRally.Synth              # port 8000
//   dotnet run --project tools/ArtOfSimRally.Synth -- 5300 10   # port, seconds
//
// Same idea as cruisn-collection's forza_synth.py, reusing this repo's real
// encoder so the synth cannot drift from what the mod actually sends.

int port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : TelemetrySender.DefaultPort;
double seconds = args.Length > 1 && double.TryParse(args[1], out var s) ? s : 0; // 0 = forever

const float MaxRpm = 7500f;
const float IdleRpm = 900f;
// Speed at which each gear tops out, in mph, so the gear steps track the ramp.
float[] gearTopMph = { 30f, 55f, 80f, 105f, 130f };

using var sender = new TelemetrySender("127.0.0.1", port);
Console.WriteLine($"emitting synthetic rally telemetry to udp://127.0.0.1:{port} at 60 Hz");
Console.WriteLine("pattern: speed 0->130->0 mph triangle (~20 s), 5 gears, RPM sawtooth");
Console.WriteLine("Ctrl+C to stop\n");

var start = DateTime.UtcNow;
uint ms = 0;
var cancelled = false;
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled = true; };

while (!cancelled)
{
    var elapsed = (DateTime.UtcNow - start).TotalSeconds;
    if (seconds > 0 && elapsed >= seconds) break;

    // Triangle wave 0..1..0 over 20 s.
    var phase = (elapsed % 20.0) / 10.0;
    var tri = phase <= 1.0 ? phase : 2.0 - phase;
    var mph = (float)(130.0 * tri);

    // Pick the gear whose band contains the current speed, then sweep RPM
    // across that band so the tachometer sawtooths the way a real one does.
    byte gear = 1;
    float low = 0f, high = gearTopMph[^1];
    for (int i = 0; i < gearTopMph.Length; i++)
    {
        if (mph <= gearTopMph[i] || i == gearTopMph.Length - 1)
        {
            gear = (byte)(i + 1);
            high = gearTopMph[i];
            break;
        }
        low = gearTopMph[i];
    }
    var frac = high <= low ? 0f : Math.Min(1f, Math.Max(0f, (mph - low) / (high - low)));
    var rpm = IdleRpm + (MaxRpm - 600f - IdleRpm) * frac;

    var speedMs = mph * 0.44704f;
    // Steer oscillates so a wheel-position readout visibly moves; slip follows it
    // loosely so per-wheel arrays are non-zero and their ordering is checkable.
    var steer = (float)Math.Sin(elapsed * 1.5);

    ms += 17;

    var frame = new TelemetryFrame
    {
        IsRaceOn         = true,
        TimestampMs      = ms,
        EngineMaxRpm     = MaxRpm,
        EngineIdleRpm    = IdleRpm,
        CurrentEngineRpm = rpm,
        VelocityZ        = speedMs,
        Speed            = speedMs,
        Power            = rpm * 20f,
        Torque           = 300f * frac,
        Gear             = gear,
        Accel            = TelemetryFrame.ToPedal(frac),
        Brake            = TelemetryFrame.ToPedal(phase > 1.0 ? 0.4f : 0f),
        Steer            = TelemetryFrame.ToSteer(steer),
        CurrentRaceTime  = (float)elapsed,
        CurrentLap       = (float)elapsed,
        LapNumber        = 1,
        RacePosition     = 1,
        DrivetrainType   = 2, // AWD, the rally default
        NumCylinders     = 4,
        TireSlipAngle    = new WheelValues(steer * 0.3f, steer * 0.3f, steer * 0.1f, steer * 0.1f),
        TireCombinedSlip = WheelValues.Uniform(Math.Abs(steer) * 0.5f),
        SurfaceRumble    = WheelValues.Uniform(0.2f),
        TireTemp         = new WheelValues(80f, 82f, 78f, 79f),
    };

    sender.Send(frame);

    Console.Write($"\r  sending: {mph,6:F1} mph  rpm {rpm,5:F0}  gear {gear}  " +
                  $"steer {frame.Steer,4}   sent {sender.PacketsSent}  fail {sender.SendFailures}   ");

    Thread.Sleep(16);
}

Console.WriteLine();
Console.WriteLine($"stopped. packets sent: {sender.PacketsSent}, failures: {sender.SendFailures}");
if (sender.SendFailures > 0) Console.WriteLine($"last error: {sender.LastError}");
