"""Repair selected local SimHub speed/RPM screens; no vendor dashboard is shipped.

Copies the original template and a receipt to a new backup directory. Only
selected dial value bindings and their speed text change. Reload the dashboard
in SimHub afterwards. This does not change telemetry, units, layouts or resources.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import os

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("template", type=Path)
parser.add_argument("backup", type=Path)
parser.add_argument("--screen", action="append", required=True)
args = parser.parse_args()
template = args.template.resolve(strict=True)
original = template.read_bytes()
dashboard = json.loads(original)
changes = []
selected = [s for s in dashboard["Screens"] if s["ScreenId"] in args.screen]
if len(selected) != len(set(args.screen)):
    raise SystemExit("Every requested screen must exist exactly once")
for screen in selected:
    dials = [item for item in screen["Items"] if "DialGaugeItem," in item.get("$type", "")]
    if len(dials) != 1:
        raise SystemExit("Expected exactly one dial on each selected screen")
    dial = dials[0]
    formula = dial["Bindings"]["Value"]["Formula"]
    expression = formula["Expression"]
    if expression not in ("[Rpms]", "[SpeedLocal]", "[SpeedMph]", "[SpeedKmh]"):
        raise SystemExit("Unsupported/already modified dial expression; inspect before changing")
    formula["Expression"] = f"if([DataCorePlugin.GameRunning], {expression}, 0)"
    changes.append({"screen": screen["Name"], "item": dial["Name"], "before": expression, "after": formula["Expression"]})
    for item in screen["Items"]:
        behavior = item.get("Behavior", {})
        if ".DoubleText.Imp.Speed," not in behavior.get("$type", ""):
            continue
        if item.get("Bindings") or behavior.get("Settings") != {"Decimals": 0, "AppendUnit": False, "AlwaysAddSign": False}:
            raise SystemExit("Unsupported speed text formatting; inspect before changing")
        item.pop("Behavior")
        item["Bindings"] = {"Text": {"FormatString": "0", "Formula": {"Expression": "if([DataCorePlugin.GameRunning], [SpeedLocal], 0)"}, "Mode": 2}}
        changes.append({"screen": screen["Name"], "item": item["Name"], "before": "Speed behavior, integer/local units", "after": item["Bindings"]["Text"]["Formula"]["Expression"]})
result = json.dumps(dashboard, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
args.backup.mkdir(parents=True, exist_ok=False)
(args.backup / template.name).write_bytes(original)
if template.read_bytes() != original:
    raise SystemExit("Template changed during preparation; original backup retained")
temporary = template.with_name(template.name + ".aosr-idle.tmp")
with temporary.open("xb") as file:
    file.write(result)
os.replace(temporary, template)
if template.read_bytes() != result:
    raise SystemExit("Written template failed verification")
receipt = {"utc": datetime.now(timezone.utc).isoformat(), "template": str(template),
           "beforeSha256": hashlib.sha256(original).hexdigest().upper(), "afterSha256": hashlib.sha256(result).hexdigest().upper(),
           "changes": changes, "dashboardReloadRequired": True, "hardwareVerified": False}
(args.backup / "receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
print(json.dumps(receipt))
