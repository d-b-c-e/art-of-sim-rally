# dbce.game-delivery v1.0.0 pilot validator

Pinned common contract proposal and fixture expectations supplied by the coordinator
on 2026-10-01. Art is the first adopter; proposal status in the frozen inputs records
their original state. This local reusable parser/validator is a pilot for review,
not an independently approved shared-toolkit release.

Dot-source delivery-validator.ps1 and call Assert-DeliveryManifest -PackageRoot.
No entrypoint is executed. Supported full integrity adapter: dbce.art-unified-package.v1.
MetadataOnly validates shape/paths/declarations only and never certifies delivery.
Game-specific semantic mappings and operation tests remain owner responsibilities.
