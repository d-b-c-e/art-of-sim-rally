"""Schema-4 collision protocol fixtures, never hardware/drive evidence."""
import hashlib
import subprocess
import unittest
import xml.etree.ElementTree as ET

import test_signals
from test_replay import ROOT


class CollisionTests(unittest.TestCase):
    setUp = test_signals.SignalTests.setUp
    write_base = test_signals.SignalTests.write_base
    run_replay = test_signals.SignalTests.run_replay
    samples = test_signals.SignalTests.samples
    corpus = test_signals.SignalTests.corpus
    HEADER = test_signals.SignalTests.HEADER
    COLLISION_HEADER = "event,time_s,physics_time_s,epoch,last_force_row,body_id,other_id,other_layer,road,crowd,contacts,examined,selected,rvx_mps,rvy_mps,rvz_mps,ix_ns,iy_ns,iz_ns,mass_kg,px_m,py_m,pz_m,qx,qy,qz,qw,vx_mps,vy_mps,vz_mps,nx,ny,nz,cpx_m,cpy_m,cpz_m"

    def write(self):
        test_signals.SignalTests.write(self)
        if not hasattr(self, "collisions"): return
        self.receipt.set("schema", "4")
        path = self.capture / "collisions.csv"
        path.write_text(self.COLLISION_HEADER + "\n" + "".join(",".join(map(str, r)) + "\n" for r in self.collisions))
        element = self.receipt.find("collisions")
        if element is None: element = ET.SubElement(self.receipt, "collisions")
        element.set("count", str(len(self.collisions)))
        element.text = hashlib.sha256(path.read_bytes()).hexdigest().upper()
        ET.ElementTree(self.receipt).write(self.capture / "manifest.xml", encoding="utf-8")

    def fixture(self):
        self.samples([15, 15])
        self.collisions = [[0, 1.02, 1.02, 0, 1, -123456789, 123456789, 8, 0, 1, 1, 1, 0,
                            0, 0, -10, 0, 0, 100, 100, 0, 0, 1, 0, 0, 0, 1,
                            0, 0, 0, 0, 0, -1, 0, 0, 2]]
        self.write()

    def result(self): return self.run_replay()["detail"]["collisions"]

    def test_units_identity_and_projection(self):
        self.fixture()
        event = self.result()["events"][0]
        self.assertEqual(event["normalSpeedMps"], 10)
        self.assertEqual(event["impulsePerMassMps"], 1)
        self.assertEqual(event["bodyId"], -123456789)
        self.assertEqual(event["otherId"], 123456789)
        self.assertEqual(event["lastForceRow"], 1)
        self.assertTrue(event["crowd"])
        self.assertFalse(event["road"])
        # High tangential speed is distinct from a direct closing speed.
        self.collisions[0][13:16] = [40, 0, -1]; self.write()
        event = self.result()["events"][0]
        self.assertGreater(event["relativeSpeedMps"], 40)
        self.assertEqual(event["normalSpeedMps"], 1)

    def test_empty_missing_and_partial_contact_are_explicit(self):
        self.fixture(); self.collisions = []; self.write()
        self.assertEqual(self.result()["rows"], 0)
        self.fixture(); self.collisions[0][10:13] = [0, 0, -1]
        self.collisions[0][30:] = [0] * 6; self.write()
        self.assertIsNone(self.result()["events"][0]["normalSpeedMps"])
        self.fixture(); self.collisions[0][10:13] = [12, 8, 7]; self.write()
        self.assertEqual(self.result()["contactLimitedEvents"], 1)

    def test_invalid_content_rejected_even_with_updated_hash(self):
        for column, value, reason in ((0, 3, "identity"), (1, -1, "time"), (3, .5, "integer"),
                                      (4, 2, "preceding"), (7, 32, "layer"), (8, 2, "flags"),
                                      (11, 2, "counts"), (12, 2, "selection"), (13, "NaN", "nonfinite"),
                                      (19, 0, "mass"), (26, 0, "quaternion"), (32, 0, "normal")):
            with self.subTest(column=column):
                self.fixture(); self.collisions[0][column] = value; self.write()
                self.assertIn(reason, self.run_replay(expected=1))

    def test_file_receipt_and_count_are_required(self):
        for variant in ("file", "receipt", "hash", "count", "overflow"):
            with self.subTest(variant=variant):
                self.fixture()
                if variant == "file": (self.capture / "collisions.csv").unlink()
                elif variant == "receipt": self.receipt.remove(self.receipt.find("collisions"))
                elif variant == "hash": self.receipt.find("collisions").text = "invalid"
                else: self.receipt.find("collisions").set("count", "2" if variant == "count" else "4097")
                ET.ElementTree(self.receipt).write(self.capture / "manifest.xml", encoding="utf-8")
                self.assertIn("collisions:", self.run_replay(expected=1))

    def test_bounded_report(self):
        self.fixture()
        row = self.collisions[0]
        self.collisions = [[i] + row[1:] for i in range(140)]; self.write()
        result = self.result()
        self.assertEqual(result["rows"], 140)
        self.assertEqual(len(result["events"]), 128)
        self.assertEqual(result["omittedEvents"], 12)

    def test_schema_four_promotion_preserves_collision_bytes(self):
        self.fixture(); self.receipt.set("origin", "game"); self.write()
        corpus = self.root / "promoted"
        result = subprocess.run(["pwsh", "-NoProfile", "-File", str(ROOT / "tools/testing/Add-RegressionCapture.ps1"),
                                 "-Capture", str(self.capture), "-Corpus", str(corpus), "-Name", "synthetic-protocol"],
                                capture_output=True, text=True, timeout=45)
        self.assertEqual(result.returncode, 0, result.stderr)
        for name in ("signals.csv", "collisions.csv"):
            self.assertEqual((corpus / "cases/synthetic-protocol" / name).read_bytes(), (self.capture / name).read_bytes())
        self.assertEqual(self.run_replay("--corpus", corpus / "index.json")["detail"]["caseCount"], 1)
        # Exercise replacement of an existing corpus index, not only creation.
        first_manifest = (corpus / "cases/synthetic-protocol/manifest.xml").read_bytes()
        second = subprocess.run(["pwsh", "-NoProfile", "-File", str(ROOT / "tools/testing/Add-RegressionCapture.ps1"),
                                 "-Capture", str(self.capture), "-Corpus", str(corpus), "-Name", "second-synthetic-protocol"],
                                capture_output=True, text=True, timeout=45)
        self.assertEqual(second.returncode, 0, second.stderr)
        self.assertEqual((corpus / "cases/synthetic-protocol/manifest.xml").read_bytes(), first_manifest)
        self.assertEqual(self.run_replay("--corpus", corpus / "index.json")["detail"]["caseCount"], 2)

    def test_legacy_capture_does_not_claim_collision_observations(self):
        self.samples([15, 15])
        self.assertFalse(self.run_replay()["detail"]["collisions"]["available"])


if __name__ == "__main__": unittest.main()
