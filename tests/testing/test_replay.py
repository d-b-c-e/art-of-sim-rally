"""Adversarial protocol fixtures. These are NOT recorded game sessions."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
RUNNER = ROOT / "tools/testing/Replay/bin/Release/net8.0/Replay.dll"


class ReplayTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.capture = self.root / "case"
        self.capture.mkdir()
        self.frames = ["frame,time_s,delta_s,driving,direct_input,steer,throttle,brake,clutch,handbrake",
                       "100,1,0.016,1,0,0,0,0,0,0", "101,2,0.016,1,0,0,0,0,0,0"]
        self.forces = ["time_s,fy_n,slip_deg,ideal_deg,speed_kmh,reference_n,gain,invert,smoothing,previous,output,device,epoch",
                       "1,0,8,8.5,40,11500,0.3,0,0.5,0,0,0,0",
                       "2,0,8,8.5,40,11500,0.3,0,0.5,0,0,0,0"]
        self.receipt = ET.Element("capture", schema="2", complete="true", origin="synthetic")
        ET.SubElement(self.receipt, "modSha256").text = "protocol-fixture"
        self.write()

    def write(self):
        for kind, rows in (("frames", self.frames), ("forces", self.forces)):
            path = self.capture / (kind + ".csv")
            path.write_text("\n".join(rows) + "\n", encoding="utf-8")
            element = self.receipt.find(kind)
            if element is None: element = ET.SubElement(self.receipt, kind)
            element.set("count", str(len(rows) - 1))
            element.text = hashlib.sha256(path.read_bytes()).hexdigest().upper()
        ET.ElementTree(self.receipt).write(self.capture / "manifest.xml", encoding="utf-8")

    def run_replay(self, flag="--replay", path=None, expected=0):
        self.assertTrue(RUNNER.is_file(), "Build tools/testing/Replay before running these tests")
        result = subprocess.run(["dotnet", str(RUNNER), flag, str(path or self.capture)], capture_output=True, text=True, timeout=20)
        if expected == 0:
            self.assertEqual(result.returncode, 0, result.stderr)
            return json.loads(result.stdout)
        self.assertNotEqual(result.returncode, 0, result.stdout)
        return result.stderr

    def corpus(self, items=None):
        manifest = self.capture / "manifest.xml"
        case = {"id": "protocol-fixture", "path": "case", "manifestSha256": hashlib.sha256(manifest.read_bytes()).hexdigest().upper()}
        index = self.root / "index.json"
        index.write_text(json.dumps({"schema": 1, "cases": [case] if items is None else items}), encoding="utf-8")
        return index

    def test_valid_synthetic_replay(self):
        result = self.run_replay()["detail"]
        self.assertEqual(result["forceRows"], 2)
        self.assertEqual(result["deviceMismatches"], 0)
        self.assertTrue(result["stateful"])

    def test_missing_frame_rejected(self):
        self.frames[2] = self.frames[2].replace("101,", "102,", 1); self.write()
        self.assertIn("missing/nonmonotonic frames", self.run_replay(expected=1))

    def test_fractional_frame_rejected(self):
        self.frames[1] = self.frames[1].replace("100,", "100.5,", 1); self.write()
        self.assertIn("invalid frame number", self.run_replay(expected=1))

    def test_nonfinite_force_rejected(self):
        self.forces[1] = self.forces[1].replace("1,0,8,", "1,NaN,8,", 1); self.write()
        self.assertIn("invalid force row", self.run_replay(expected=1))

    def test_no_drive_rejected(self):
        self.frames = [self.frames[0]] + [row.replace(",0.016,1,", ",0.016,0,") for row in self.frames[1:]]; self.write()
        self.assertIn("no drive", self.run_replay(expected=1))

    def test_empty_force_rejected(self):
        self.forces = self.forces[:1]; self.write()
        self.assertIn("count mismatch/empty", self.run_replay(expected=1))

    def test_wrong_device_force_rejected(self):
        cells = self.forces[1].split(","); cells[11] = "1"; self.forces[1] = ",".join(cells); self.write()
        self.assertIn("device magnitude mismatch", self.run_replay(expected=1))

    def test_invalid_epoch_rejected(self):
        cells = self.forces[1].split(","); cells[12] = "-1"; self.forces[1] = ",".join(cells); self.write()
        self.assertIn("invalid reset epoch", self.run_replay(expected=1))

    def test_empty_corpus_rejected(self):
        self.assertIn("corpus is empty", self.run_replay("--corpus", self.corpus([]), expected=1))

    def test_synthetic_cannot_enter_recorded_corpus(self):
        self.assertIn("not a recorded game session", self.run_replay("--corpus", self.corpus(), expected=1))

    def test_changed_corpus_receipt_rejected(self):
        index = self.corpus(); self.receipt.set("note", "changed"); self.write()
        self.assertIn("Corpus receipt changed", self.run_replay("--corpus", index, expected=1))

    def test_corpus_protocol_and_duplicate_detection(self):
        # Deliberately exercise the game-origin protocol with an artificial
        # fixture. This temporary file is never promoted as real drive evidence.
        self.receipt.set("origin", "game"); self.write()
        index = self.corpus()
        self.assertEqual(self.run_replay("--corpus", index)["detail"]["caseCount"], 1)
        data = json.loads(index.read_text()); data["cases"] *= 2; index.write_text(json.dumps(data))
        self.assertIn("duplicate corpus case", self.run_replay("--corpus", index, expected=1))

    def test_promotion_preserves_and_rejects_overwrite(self):
        # Only protocol fixtures; using a temporary fake game receipt here tests
        # copying/indexing, not the quality or authenticity of a recorded drive.
        script = ROOT / "tools/testing/Add-RegressionCapture.ps1"
        corpus = self.root / "corpus"
        command = ["pwsh", "-NoProfile", "-File", str(script), "-Capture", str(self.capture), "-Corpus", str(corpus), "-Name", "protocol-fixture"]
        result = subprocess.run(command, capture_output=True, text=True, timeout=30)
        self.assertNotEqual(result.returncode, 0, "synthetic capture was promoted")
        self.receipt.set("origin", "game"); self.write()
        result = subprocess.run(command, capture_output=True, text=True, timeout=45)
        self.assertEqual(result.returncode, 0, result.stderr)
        before = (corpus / "index.json").read_bytes()
        self.assertEqual(self.run_replay("--corpus", corpus / "index.json")["detail"]["caseCount"], 1)
        result = subprocess.run(command, capture_output=True, text=True, timeout=45)
        self.assertNotEqual(result.returncode, 0, "existing case was overwritten")
        self.assertEqual((corpus / "index.json").read_bytes(), before)


if __name__ == "__main__": unittest.main()
