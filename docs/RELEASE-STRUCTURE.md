# Component releases and game-ready bundles

Keep wheel and triple versions/pins independent. Existing tags, ZIP names, component mod IDs, release links and installer receipts remain compatible. Wheel build/package scripts now live under `components/wheel`; triple scripts under `components/triple`. Run each from its component folder. Root `tools/game` remains the shared setup entry point.

`game-release.json` is the authoritative candidate combination of exact previously published artifacts. It records public visibility, release URLs, package-manifest identities, source checkpoints and acceptance limits. It is not a new binary release or evidence that unpublished 0.3.12 source was installed.

Future bundles may use `dbce-mods-art-of-rally-<bundle-version>-windows-x64.zip`, containing independent component packages, checksums, notices and one setup/readiness guide. New tags can use `wheel/vX.Y.Z`, `triple/vX.Y.Z`, `bundle/vX.Y.Z`; retain historical wheel tags and triple origin refs. Publish only a frozen, tested archive; do not rebuild during publication. No proprietary Unity/game assemblies, owner settings, ROMs, credentials or recordings enter packages.

Readiness records each feature as native, verified adapter, needs UAT, explicit fallback or blocked, tied to exact source/binary/settings identities. Offline installer/projection results do not establish torque, comfort, rendering or panel alignment. Unknown game versions fail compatibility policy; do not enable unsupported probes as everyday components.

GitHub mapping: reuse public `d-b-c-e/art-of-sim-rally` as public `d-b-c-e/dbce-mods-art-of-rally`. Never reuse the old name, because that removes redirects. No GitHub Pages or hosted action entry point exists in the inspected repos. The old triple repository is archived with a migration notice; its existing releases remain available. Historical release links remain; update live documentation links after verified rename. No standalone updater URL was found in the inspected Art plugin/installer sources.
