# Architecture

Mech Muster has four explicit concerns.

1. `Domain/MusterAllocator` is a RimWorld-free deterministic selector. It
   rejects ineligible, fulfilled, unrequested, and bandwidth-incompatible
   candidates, then orders by explicit priority, proportional fulfilment,
   and stable mechanitor ID.
2. `Roster` owns save-scoped `MechanitorPlan` and `MechTarget` data through a
   `GameComponent`. Pawn and `PawnKindDef` references use normal Scribe
   reference/definition modes. Plans are retained while the referenced pawn is
   absent or dead and are discarded only if that reference cannot be restored
   after load.
3. `Runtime/MusterAssignmentService` adapts live RimWorld pawns to pure
   candidates. It requires a live player mechanitor on the same map, a usable
   mechanitor tracker, enough current bandwidth, a controllable living mech,
   per-mechanitor automation, and an outstanding type target.
4. `Presentation` owns the mechanitor window; `Patches` owns three narrow
   integration points. Spine 1.2 supplies the complete mod-settings page,
   definition scribing, translation fallback, contextual-settings lease, and
   contextual routing through `SpineApi.Settings`.

The ordinary Mech Muster gizmo sorts after vanilla commands at the far right
without changing vanilla order. It owns both paths: a normal click opens the
roster, while Alt-click is consumed by Spine's contextual binding and opens the
narrow global automation setting. The roster is not opened on the contextual
path.

## Mutation boundary

The periodic path enumerates only player mechs with no overseer. The gestation
and resurrection postfixes treat the returned product as a new assignment
decision because vanilla has just attached it to the bill's bound pawn. No
other existing relationship is changed. `AddDirectRelation` and
`TryRemoveDirectRelation` deliberately go through vanilla's
`PawnRelationWorker_Overseer`, so vanilla owns control-group membership and
bandwidth recalculation. Mech Muster never calls `MechanitorControlGroup.Assign`
directly and never selects or changes a work mode.

There is no rebalancing command. That is intentional: it keeps the no-steal
default absolute and avoids presenting a destructive preview for a feature the
mod does not implement.

## Lifecycle

- Gestation/resurrection: `Bill_ProductionMech.CreateProducts` and
  `Bill_ResurrectMech.CreateProducts` postfixes route the new product if global
  automation is enabled.
- Claim/transfer: manual `JobDriver_ControlMech` relationships are existing
  assignments and are never reconsidered.
- Death/loss: vanilla removes invalid overseer relations; any live mech that is
  truly unassigned becomes a future candidate. The mechanitor's desired plan is
  retained for resurrection or return.
- Map arrival: the game component scans unassigned colony mechs every 250 game
  ticks and requires same-map eligibility.
- Save/load: the component scribes plans, dynamic defs, automation, quantities,
  and priorities; no map-local cache is persisted.

## One-caller helper inventory

- `Dialog_MechMuster.DrawRow` has one production caller and remains local: it
  owns the cohesive row layout and keeps window orchestration readable.
- `MusterAssignmentService.IsEligible` has one production caller and remains a
  named domain boundary: inlining its vanilla eligibility contract would make
  selection harder to audit.
- `Command_MechMuster.BindSettings` is called by both full and shrunk gizmo
  rendering, so the two render paths share one exact contextual target.
- Debug entry points are reflection-discovered by RimWorld even where ordinary
  source reference counts are zero; they are not dead one-caller helpers.
