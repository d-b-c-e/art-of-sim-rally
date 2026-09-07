"""Local release evidence gate. Does not install, launch, publish or inject input."""
import argparse
import hashlib
import json
from pathlib import Path

AUTOMATED = {"build", "telemetry-loopback", "regression", "lifecycle", "dev-recorder", "replay", "replay-rejection", "release-gate", "vector", "native-exports", "package", "no-recorder", "dev-installer", "installer", "installer-rejection", "source-stability"}
CASES = {
    "camera": "Cycle all stock views before/after bonnet+bumper; finish cinematic; replay; pause; restart; disable/re-enable mod. Repeat with PS5 controller attached and absent when available; inspect ChangeCamera binding.",
    "stutter": "Capture cold stage first 15s, same-stage restart, and a different stage. Compare warm/cold runs and a mod-disabled run if stutter persists. Note any 100ms+ hitches and rig settings.",
    "ffb-lifecycle": "At a comfortable strength, verify centering, pause/resume, finish, alt-tab/reacquire, disable mod, quit. Force must release; no unexpected snap. Keep a hand at the rig.",
    "input-persistence": "Learn steering and all bound pedal ranges, use clutch/handbrake/shifter, pause, quit, relaunch. Verify bindings/ranges and Strength/Smoothing persist, menus work. Select the wheel explicitly, restart, and verify its saved GUID still identifies it; a missing saved wheel must not choose another.",
    "telemetry": "SimHub from start line through driving, pause, finish, replay and quit. Verify live/park transitions with your usual consumers.",
    "support-identity": "Generate support file. Match build identity, managed hash and observed native hash against this exact candidate; check toolkit pin and native component against build.json/manifest. A plugin path alone is not stale.",
    "umm-upgrade": "With game closed, install the packaged zip using your normal UMM route; verify it loads exactly once and keeps settings. Record route and installed build identity.",
}

def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))

def require(condition, message):
    if not condition:
        raise ValueError(message)

def verify_automated(path):
    report = read(path)
    require(report.get("schema") == 1 and report.get("status") == "passed", "Automated gate has not passed")
    checks = report.get("checks", [])
    require(len(checks) == len(AUTOMATED) and {c["name"] for c in checks} == AUTOMATED, "Missing/duplicate automated checks")
    require(all(c.get("status") == "passed" and c.get("assertions", 0) > 0 for c in checks), "Empty or failed automated check")
    require(digest(report["artifact"]) == report["artifactSha256"], "Candidate archive changed")
    return report

def initialize(report_path, output):
    report = verify_automated(report_path)
    document = {"schema": 1, "automatedReport": str(Path(report_path).resolve()),
                "automatedSha256": digest(report_path), "release": report["release"],
                "artifactSha256": report["artifactSha256"], "tester": "", "rig": "",
                "cases": [{"id": key, "steps": value, "status": "pending", "notes": "", "evidence": []}
                          for key, value in CASES.items()]}
    with Path(output).open("x", encoding="utf-8") as stream:
        json.dump(document, stream, indent=2)
    return "Attended checklist created; release remains blocked until it passes."

def check(path):
    manual = read(path)
    require(manual.get("schema") == 1, "Unknown manual schema")
    report = verify_automated(manual["automatedReport"])
    require(digest(manual["automatedReport"]) == manual["automatedSha256"], "Automated report changed since checklist creation")
    require(manual["release"] == report["release"] and manual["artifactSha256"] == report["artifactSha256"], "Evidence belongs to a different candidate")
    require(manual.get("tester", "").strip() and manual.get("rig", "").strip(), "Tester/rig not recorded")
    cases = manual.get("cases", [])
    require(len(cases) == len(CASES) and {c["id"] for c in cases} == set(CASES), "Missing/duplicate attended cases")
    for case in cases:
        require(case.get("status") == "passed", f'{case["id"]}: {case.get("status", "missing")}')
        require(case.get("notes", "").strip() and case.get("evidence"), f'{case["id"]}: needs notes and evidence')
        for item in case["evidence"]:
            require(digest(item["path"]) == item["sha256"], f'{case["id"]}: evidence missing/changed')
    return "Candidate has automated and attended sign-off. Publish only this tested artifact; any rebuild needs its own evidence."

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    init = sub.add_parser("init"); init.add_argument("report"); init.add_argument("output")
    gate = sub.add_parser("check"); gate.add_argument("manual")
    args = parser.parse_args()
    try:
        print(initialize(args.report, args.output) if args.command == "init" else check(args.manual))
    except (ValueError, OSError, KeyError, TypeError) as error:
        parser.exit(1, f"NOT READY: {error}\n")

if __name__ == "__main__":
    main()
