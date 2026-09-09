"""Synthetic motion observations; no game capture or physical-force validation."""
import math
import subprocess
import xml.etree.ElementTree as ET

import test_replay


# Reuse fixture helpers without inheriting its tests a second time.
import unittest
class SignalTests(unittest.TestCase):
    setUp = test_replay.ReplayTests.setUp
    write_base = test_replay.ReplayTests.write
    run_replay = test_replay.ReplayTests.run_replay
    corpus = test_replay.ReplayTests.corpus

    HEADER = "force_row,time_s,epoch,valid,physics_time_s,contact_mask,px_m,py_m,pz_m,vx_mps,vy_mps,vz_mps,qx,qy,qz,qw,local_vx_mps,local_vy_mps,local_vz_mps,compression_fl_m,compression_fr_m,compression_rl_m,compression_rr_m,travel_fl_m,travel_fr_m,travel_rl_m,travel_rr_m"

    def write(self):
        self.write_base()
        if not hasattr(self, "signals"): return
        import hashlib
        path = self.capture / "signals.csv"
        path.write_text(self.HEADER + "\n" + "\n".join(",".join(map(str, row)) for row in self.signals) + "\n", encoding="utf-8")
        element = self.receipt.find("signals")
        if element is None: element = ET.SubElement(self.receipt, "signals")
        element.set("count", str(len(self.signals)))
        element.text = hashlib.sha256(path.read_bytes()).hexdigest().upper()
        ET.ElementTree(self.receipt).write(self.capture / "manifest.xml", encoding="utf-8")

    def samples(self, masks, vy=None):
        self.receipt.set("schema", "3")
        self.forces = self.forces[:1]
        self.signals = []
        for i, mask in enumerate(masks):
            time = 1 + i * .02
            velocity_y = 0 if vy is None else vy[i]
            self.forces.append(f"{time},0,8,8.5,40,11500,0.3,0,0.5,0,0,0,0")
            self.signals.append([i, time, 0, 1, time, mask, 0, 0, i * .2,
                                 0, velocity_y, 10, 0, 0, 0, 1, 0, velocity_y, 10,
                                 .1, .1, .1, .1, .2, .2, .2, .2])
        self.write()

    def result(self): return self.run_replay()["detail"]["signals"]

    def test_landing_reports_observed_force_and_motion(self):
        self.samples([15] + [0] * 5 + [3] * 4, [0] + [-2] * 5 + [0] * 4)
        report = self.result()
        self.assertEqual(report["landingCandidates"], 1)
        event = report["events"][0]
        self.assertEqual(event["forceRow"], 6)
        self.assertAlmostEqual(event["precedingSeconds"], .1, places=5)
        self.assertAlmostEqual(event["localAccelerationMps2"][1], 100, places=2)
        self.assertEqual(event["normalizedCompression"], [.5] * 4)
        self.assertEqual(event["force"], event["previousForce"])
        self.assertEqual(report["unavailableRows"], 0)

    def test_short_airborne_jitter_does_not_report_landing(self):
        self.samples([15, 0, 15, 15, 15, 15])
        self.assertEqual(self.result()["landingCandidates"], 0)

    def test_brief_contact_is_not_stable_landing(self):
        self.samples([0] * 5 + [1, 0, 0])
        self.assertEqual(self.result()["landingCandidates"], 0)

    def test_rotating_vehicle_does_not_invent_acceleration(self):
        self.samples([15, 15])
        # Constant world velocity; 90 degrees around local X rotates the local
        # velocity into Y, but differentiating WORLD velocity remains zero.
        self.signals[1][12:16] = [math.sqrt(.5), 0, 0, math.sqrt(.5)]
        self.signals[1][16:19] = [0, 10, 0]; self.write()
        self.assertEqual(self.result()["maxAbsLocalVerticalAccelerationMps2"], 0)

    def test_discontinuities_reset_event_and_acceleration_history(self):
        for variant in ("epoch", "gap", "rollback", "duplicate", "teleport", "missing"):
            with self.subTest(variant=variant):
                self.samples([0] * 5 + [15] * 4)
                if variant == "epoch":
                    for i in range(5, 9):
                        self.signals[i][2] = 1
                        self.forces[i + 1] = self.forces[i + 1].rsplit(",", 1)[0] + ",1"
                elif variant == "gap":
                    for i in range(5, 9): self.signals[i][4] += 1
                elif variant == "rollback":
                    for i in range(5, 9): self.signals[i][4] -= 1
                elif variant == "duplicate": self.signals[5][4] = self.signals[4][4]
                elif variant == "teleport":
                    for i in range(5, 9): self.signals[i][6] = 1000
                else: self.signals[5][3:] = [0] * 24
                self.write()
                self.assertEqual(self.result()["landingCandidates"], 0)

    def test_slide_recovery_and_grounded_compression_summary(self):
        self.samples([15] * 4)
        cells = self.forces[1].split(","); cells[2] = "24"; self.forces[1] = ",".join(cells)
        self.signals[1][19:23] = [.12] * 4; self.write()
        report = self.result()
        self.assertEqual(report["slideRecoveryCandidates"], 1)
        self.assertGreater(report["groundedCompressionDeltaRms"], 0)
        self.assertEqual(report["groundedWheelPairs"], 12)

    def test_missing_context_is_explicit_and_not_a_measured_zero(self):
        self.samples([15, 15])
        for row in self.signals: row[3:] = [0] * 24
        self.write(); report = self.result()
        self.assertEqual(report["unavailableRows"], 2)
        self.assertEqual(report["accelerationRows"], 0)
        self.assertIsNone(report["maxAbsLocalVerticalAccelerationMps2"])
        self.assertIsNone(report["groundedCompressionDeltaRms"])

    def test_corruption_alignment_and_invalid_values_rejected(self):
        for column, value, message in ((0, 99, "alignment"), (1, 55, "alignment"), (2, 1, "alignment"),
                                       (3, 2, "availability"), (4, -1, "clock"), (5, 16, "mask"), (5, .5, "mask"),
                                       (6, "NaN", "nonfinite"), (15, 0, "quaternion"), (18, 99, "projection")):
            with self.subTest(column=column, value=value):
                self.samples([15, 15]); self.signals[1][column] = value; self.write()
                self.assertIn(message, self.run_replay(expected=1))

    def test_missing_extra_rows_and_wrong_hash_rejected(self):
        for variant in ("missing", "extra", "hash"):
            with self.subTest(variant=variant):
                self.samples([15, 15])
                if variant == "missing": self.signals.pop(); self.write()
                elif variant == "extra": self.signals.append(self.signals[-1]); self.write()
                else:
                    with (self.capture / "signals.csv").open("a") as f: f.write("tampered\n")
                self.assertIn("signals:", self.run_replay(expected=1))

    def test_event_report_is_bounded(self):
        self.samples(([0] * 5 + [15] * 4) * 140)
        report = self.result()
        self.assertEqual(report["landingCandidates"], 140)
        self.assertEqual(len(report["events"]), 128)
        self.assertEqual(report["omittedEvents"], 12)

    def test_schema_three_promotion_preserves_signal_bytes(self):
        from test_replay import ROOT
        self.samples([15, 15]); self.receipt.set("origin", "game"); self.write()
        # Fake game origin exercises only the protocol, never a real corpus.
        corpus = self.root / "promoted"
        result = subprocess.run(["pwsh", "-NoProfile", "-File", str(ROOT / "tools/testing/Add-RegressionCapture.ps1"),
                                 "-Capture", str(self.capture), "-Corpus", str(corpus), "-Name", "synthetic-protocol"],
                                capture_output=True, text=True, timeout=45)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((corpus / "cases/synthetic-protocol/signals.csv").read_bytes(), (self.capture / "signals.csv").read_bytes())
        self.assertEqual(self.run_replay("--corpus", corpus / "index.json")["detail"]["caseCount"], 1)


if __name__ == "__main__": unittest.main()
