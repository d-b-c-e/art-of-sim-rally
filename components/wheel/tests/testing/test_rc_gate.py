import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("rc_gate", Path(__file__).resolve().parents[2] / "tools/testing/rc_gate.py")
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)


class ReleaseGateTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.artifact = self.root / "candidate.zip"
        self.artifact.write_bytes(b"fixture")
        self.report = self.root / "automated.json"
        self.data = {"schema": 1, "status": "passed", "release": "0.2.3-rc.99",
                     "artifact": str(self.artifact), "artifactSha256": gate.digest(self.artifact),
                     "checks": [{"name": n, "status": "passed", "assertions": 1} for n in gate.AUTOMATED]}
        self.report.write_text(json.dumps(self.data))
        self.manual = self.root / "manual.json"
        gate.initialize(self.report, self.manual)

    def sign(self):
        data = gate.read(self.manual)
        data.update(tester="fixture", rig="synthetic")
        evidence = self.root / "notes.txt"
        evidence.write_text("Synthetic test; no game drive")
        for case in data["cases"]:
            case.update(status="passed", notes="fixture", evidence=[{"path": str(evidence), "sha256": gate.digest(evidence)}])
        self.manual.write_text(json.dumps(data))

    def test_pending_never_passes(self):
        with self.assertRaises(ValueError): gate.check(self.manual)

    def test_completed_evidence_passes(self):
        self.sign()
        self.assertIn("sign-off", gate.check(self.manual))

    def test_changed_candidate_fails(self):
        self.sign(); self.artifact.write_bytes(b"different build")
        with self.assertRaises(ValueError): gate.check(self.manual)

    def test_missing_and_duplicate_cases_fail(self):
        self.sign(); data = gate.read(self.manual)
        data["cases"][-1] = data["cases"][0]
        self.manual.write_text(json.dumps(data))
        with self.assertRaises(ValueError): gate.check(self.manual)

    def test_changed_evidence_fails(self):
        self.sign(); (self.root / "notes.txt").write_text("changed")
        with self.assertRaises(ValueError): gate.check(self.manual)

    def test_zero_tests_fail(self):
        self.data["checks"][0]["assertions"] = 0
        self.report.write_text(json.dumps(self.data))
        with self.assertRaises(ValueError): gate.verify_automated(self.report)
