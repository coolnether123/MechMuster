# Compatibility

Mech Muster discovers mech types from every loaded `PawnKindDef` whose race is
mechanoid and contains `CompProperties_OverseerSubject`. No vanilla def-name
allowlist is used, so ordinary modded controllable mechs participate without a
patch.

The mod patches only these RimWorld 1.6 methods:

- `Bill_ProductionMech.CreateProducts` (postfix)
- `Bill_ResurrectMech.CreateProducts` (postfix)
- `Pawn_MechanitorTracker.GetGizmos` (postfix)

It does not patch the Mechs main tab, `MechanitorControlGroup`, mech work modes,
`JobDriver_ControlMech`, or pawn relation internals. UI sorting mods such as
Better Mechanoid Sorting therefore occupy adjacent rather than identical patch
surfaces. Mods replacing either bill product method or the complete mechanitor
gizmo enumerable should be checked for patch-order behavior. Mods that create
controllable colony mechs without vanilla gestation are covered when the mech
arrives on a map unassigned.

There is no public provider API in 1.0 because definition-driven discovery and
vanilla eligibility cover the known compatibility surface. A registration API
would be speculative until a concrete nonstandard mech system needs one.
