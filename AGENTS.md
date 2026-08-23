# Mech Muster Agent Guide

Own only this repository. Automatic operation never changes an existing
overseer assignment or control group. Candidates are newly
gestated/resurrected mechs and currently unassigned colony mechs only.
Selection is deterministic: explicit priority, proportional fulfilment, then
stable pawn ID. Respect bandwidth and same-map eligibility; leave unrequested
mechs unassigned.

Save-scoped targets remain attached to temporarily absent or dead mechanitors
and are removed only when the pawn reference is permanently unavailable after
load.

Use `SpineMod<MechMusterSettings>` and Spine's patching facade. Keep gameplay
and dynamic mech UI local; keep mod gizmos at the far right.

Metadata, `Tools\CascadeManifest.json`, project files, and `Tests` are the local
authorities. Use the shared workflow in `A:\Dev\RimWorld\AGENTS.md` and the
harness's `biotech` profile for live verification.
