"""Run an offline managed test with the installed game's Mono runtime.

No game launch, Unity engine initialization, input injection or native FFB.
The executable must itself be an offline test. Game assemblies remain local.
"""
import argparse
import ctypes as c
import subprocess
import sys
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--game", default=r"D:\Program Files (x86)\Steam\steamapps\common\artofrally")
parser.add_argument("--worker", action="store_true", help=argparse.SUPPRESS)
parser.add_argument("executable")
parser.add_argument("arguments", nargs="*")
args = parser.parse_args()
if not args.worker:
    # A teardown regression can hang inside native Mono. Keep the gate bounded
    # and isolate that runtime from this supervisor process.
    try:
        result = subprocess.run([sys.executable, str(Path(__file__).resolve()), "--worker", *sys.argv[1:]], timeout=30)
    except subprocess.TimeoutExpired:
        raise SystemExit("Unity Mono offline test exceeded 30 seconds (possible runtime/IPC teardown hang)")
    raise SystemExit(result.returncode)
game = Path(args.game).resolve()
managed = game / "artofrally_Data" / "Managed"
runtime = game / "MonoBleedingEdge"
dll = c.CDLL(str(runtime / "EmbedRuntime" / "mono-2.0-bdwgc.dll"))

def api(name, result, *types):
    fn = getattr(dll, name)
    fn.restype, fn.argtypes = result, list(types)
    return fn

api("mono_set_assemblies_path", None, c.c_char_p)(str(managed).encode())
api("mono_set_dirs", None, c.c_char_p, c.c_char_p)(str(managed).encode(), str(runtime / "etc").encode())
domain = api("mono_jit_init_version", c.c_void_p, c.c_char_p, c.c_char_p)(b"aosr-offline-tests", b"v4.0.30319")
if not domain:
    raise RuntimeError("Could not initialize Mono")
exe = str(Path(args.executable).resolve()).encode()
assembly = api("mono_domain_assembly_open", c.c_void_p, c.c_void_p, c.c_char_p)(domain, exe)
if not assembly:
    raise RuntimeError("Could not load offline test assembly")
argv = [exe] + [arg.encode() for arg in args.arguments]
result = api("mono_jit_exec", c.c_int, c.c_void_p, c.c_void_p, c.c_int, c.POINTER(c.c_char_p))(
    domain, assembly, len(argv), (c.c_char_p * len(argv))(*argv))
api("mono_jit_cleanup", None, c.c_void_p)(domain)
raise SystemExit(result)
