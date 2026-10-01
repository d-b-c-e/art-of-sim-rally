DBCE mods for art of rally - LOCAL UNRELEASED CANDIDATE

One setup and package version supplies wheel input, force feedback, telemetry,
and triple display features. Install Unity Mod Manager for Art of Rally first.
Close the game, then run Install.bat -GameDir "D:\Games\artofrally".
Add -DryRun to validate the package and migration without writing files.
Use only this installer: dragging this transactional payload ZIP into UMM is unsupported.

Existing wheel and triple settings stay in their original directories.
The ArtOfSimRally UMM entry owns both features and provides two settings pages.
The separate triple compatibility metadata row has no code or load method;
it preserves legacy optimizer discovery. Triple measurements work without the optimizer.
Saved rendering choices and FFB strengths are retained. No new choices are enabled by setup.

Recognized legacy package hashes can migrate; changed/unknown payloads are refused.
The installer records original owned files and verifies every new payload.
Interrupted setup requires Rollback.bat with the same -GameDir before retrying.
Rollback restores previous owned payload, preserving current user settings.
Uninstall.bat removes receipt-owned files and preserves settings/backups.
Changed files block destructive operations; never delete an entire mod directory.

This candidate has no new gameplay implementation. Anthony's previous acceptance
applies to installed wheel 0.2.7 / triple 0.3.12, not this unified artifact.
The exact installed triple 0.3.12 source is unknown. Offline checks do not certify
physical FFB, displayed seams, FOV transitions, performance, or tear-free output.
Public wheel 0.2.7 and triple 0.3.11 downloads remain unchanged.
