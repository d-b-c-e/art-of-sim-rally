# Optional Triple Screen Optimizer integration

The mod works on its own. Players can select **Off**, **Single wide display**, or
**Three separate displays**, enter screen measurements, and adjust field of view
in the mod's Unity Mod Manager panel. Triple Screen Optimizer is an optional
source of those measurements; it does not install or control the mod.

The stable interchange is the pinned JSON schemas in `contracts/` and the
projection semantics in `Dbce.TripleScreen.Core`. The optimizer may write a
desired layout to
`%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\desired-layout.json`.
When the game cannot see that per-user file, it may stage identical bytes at
`Mods/DbceTripleScreenArtOfRally/desired-layout.json`. The mod prefers the
canonical file and reads the staged copy only when the canonical file is absent.
Players can use measurements entered locally even when an imported file exists.

The mod reports observed state at
`%LOCALAPPDATA%\DBCE\TripleScreen\games\art-of-rally\status.json`. Consumers
should require an active state, the expected layout hash, and the required
capability before showing success. A matching hash alone does not prove that
the rendering path is active. Imported layouts do not automatically enable a
mode; the player chooses the output mode in the mod.

The current renderer supports three viewports on an exact three-panel-wide
output and direct rendering to three separate Unity displays. It does not
change NVIDIA Surround or Windows display topology. Both modes require a
matching output resolution and the same physical rig measurements. Surround
tearing remains [open](KNOWN-ISSUES.md).
