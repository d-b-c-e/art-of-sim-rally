# Optimizer/mod contracts

These schemas are the canonical interchange boundary between `triple-screen-optimizer` and DBCE
game adapters. A game repository may vendor a pinned copy, but must not evolve it independently.

- `triple-screen-layout.schema.json` — physical rig inputs and requested output topology. Version 1
  matches the Art of Rally prototype contract.
- `adapter-manifest.schema.json` — installed adapter identity, compatibility, capabilities, and
  owned files.
- `runtime-status.schema.json` — evidence that the adapter loaded and accepted a particular layout.

All producers write atomically. Consumers reject unknown schema versions and preserve the last
valid configuration. See [mod integration](../docs/design/mod-integration.md) for lifecycle and
safety rules.
