"""Standalone Forza-packet probe: proves the EMIT side independently of SimHub.

Listens on the telemetry port and live-prints what art-of-sim-rally is actually
sending, so the emitter and the consumer can be tested separately:

  1. CLOSE SimHub (it holds the port).
  2. Run:  python harness/forza_probe.py        (default port 8000)
  3. Launch art of rally, start a stage, drive.
     Speed should rise AND fall with the car; "drops seen" counts every time
     speed decreased, so a non-zero count proves it is not stuck ramping.
  4. Ctrl+C for a summary. Then close this, reopen SimHub, and compare.

If this probe shows correct values but SimHub's dash does not, the problem is
the SimHub game profile (packet layout), not the emitter.

Byte 323 is unused by every real Forza title, so the mod stamps 'R' there.
That is how the "source" column tells our packets apart from anything else
already emitting on this port.

Pattern borrowed from the sibling cruisn-collection project, whose equivalent
harness was validated live against SimHub's Forza Horizon profile.
"""
import os
import socket
import struct
import sys
import time

PACKET_SIZE = 324
OFF_IS_RACE_ON = 0
OFF_ENGINE = 8          # maxRpm, idleRpm, currentRpm
OFF_SPEED = 256         # metres/second
OFF_ACCEL = 315
OFF_BRAKE = 316
OFF_GEAR = 319
OFF_STEER = 320
OFF_SENTINEL = 323
SENTINEL = 0x52         # 'R' - written by ArtOfSimRally.Telemetry.ForzaPacket

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
MPS_TO_MPH = 1.0 / 0.44704

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind(("0.0.0.0", PORT))
sock.settimeout(0.5)

results = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "results")
os.makedirs(results, exist_ok=True)
log_path = os.path.join(results, "forza_probe_log.csv")
logf = open(log_path, "w")
logf.write("t,src,race_on,mph,rpm,maxrpm,gear,steer,accel,brake\n")

print(f"listening on UDP {PORT} (Forza Data Out)... Ctrl+C to stop")
print(f"logging every packet to {log_path}")

n = 0
last_mph = 0.0
max_mph = 0.0
max_rpm = 0.0
drops = 0
sizes = set()
t0 = time.time()

try:
    while True:
        try:
            data, _ = sock.recvfrom(2048)
        except socket.timeout:
            continue

        n += 1
        sizes.add(len(data))
        if len(data) != PACKET_SIZE:
            # Wrong size means a different Forza title's layout, or a truncated
            # send. Counting it in `sizes` is more useful than parsing garbage.
            continue

        race_on, = struct.unpack_from("<i", data, OFF_IS_RACE_ON)
        maxr, idler, cur = struct.unpack_from("<fff", data, OFF_ENGINE)
        spd_ms, = struct.unpack_from("<f", data, OFF_SPEED)
        gear = data[OFF_GEAR]
        steer = struct.unpack_from("<b", data, OFF_STEER)[0]
        accel = data[OFF_ACCEL]
        brake = data[OFF_BRAKE]
        src = "mod" if data[OFF_SENTINEL] == SENTINEL else "other"

        mph = spd_ms * MPS_TO_MPH
        logf.write(f"{time.time() - t0:.2f},{src},{race_on},{mph:.1f},{cur:.0f},"
                   f"{maxr:.0f},{gear},{steer},{accel},{brake}\n")

        if mph < last_mph - 0.5:
            drops += 1
        last_mph = mph
        max_mph = max(max_mph, mph)
        max_rpm = max(max_rpm, cur)

        sys.stdout.write(
            f"\r  [{src}] race={race_on} {mph:6.1f} mph  rpm {cur:5.0f}/{maxr:5.0f}"
            f"  gear {gear}  steer {steer:4d}  thr {accel:3d}  brk {brake:3d}   ")
        sys.stdout.flush()

except KeyboardInterrupt:
    pass
finally:
    logf.close()
    print("\n")
    print(f"  packets      : {n}")
    print(f"  packet sizes : {sorted(sizes) or 'none'}")
    print(f"  max speed    : {max_mph:.1f} mph")
    print(f"  max rpm      : {max_rpm:.0f}")
    print(f"  drops seen   : {drops}   (0 with a non-trivial run means speed never fell)")
    print(f"  log          : {log_path}")
    if n and PACKET_SIZE not in sizes:
        print("\n  WARNING: nothing arrived at the expected 324-byte size.")
