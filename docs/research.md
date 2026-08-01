# Research and decision record (2026-08-01)

## Duplicate and community check

Current Workshop/community searches found active adjacent tools, not the same
behavior. [Better Mechanoid Sorting](https://steamcommunity.com/sharedfiles/filedetails/?id=3730840942)
groups the Mechs table and supports direct drag/reorder of vanilla control
groups. [WVC - Work Modes](https://steamcommunity.com/sharedfiles/filedetails/?id=2888380373)
adds group work modes. [[AV] Mechanoid Skins](https://steamcommunity.com/sharedfiles/filedetails/?id=3667667489)
remembers a mech's previous overseer/control group as part of appearance
management. None exposes per-mechanitor desired type counts or deficit-aware
assignment. A current community report describes the unresolved friction of
[multiple mechanitors](https://www.reddit.com/r/RimWorld/comments/1syjip0/multiple_mechinator_frustrations/),
including manually constraining gestator/resurrection bills. Another recent
management discussion received work-mode suggestions rather than a roster
allocator: [RimWorld community thread](https://www.reddit.com/r/RimWorld/comments/1rpk0ge/are_there_any_mods_to_improve_the_management_of/).

The canonical CoolNether123 Discord export dated 2026-07-29 in the private
RimWorld Server research corpus
contains no prior Mech Muster spec. It does document large simultaneous mech
armies (roughly 60, 20, 15, and 14 links across named mechanitors), repeated
gestation, and mechanitor deathrest edge cases. That supports a per-mechanitor
composition tool and preserving absent/dead pawn plans.

## RimWorld 1.6 behavior

The current local RimWorld 1.6 `Assembly-CSharp` decompile was inspected.

- `PawnRelationWorker_Overseer.OnRelationCreated` calls
  `AssignPawnControlGroup`; removal calls `UnassignPawnFromAnyControlGroup`.
- `Pawn_MechanitorTracker.AssignPawnControlGroup` preserves vanilla group
  ownership and recomputes bandwidth. `Notify_BandwidthChanged` controls the
  active subset in assignment order.
- `MechanitorUtility.CanControlMech` checks colony-mech status, downed/dead/
  attacking state, controllability, existing overseer, and free bandwidth.
- Both mech production and resurrection create the vanilla overseer relation
  inside `CreateProducts`; manual claiming/transfers do so in
  `JobDriver_ControlMech`.
- `MechanitorControlGroup.ExposeData` owns assigned-mech order, work mode,
  target, tags, and recharge thresholds. Mech Muster therefore never serializes
  or mirrors group data.

The public [Mechanitor documentation](https://rimworldwiki.com/wiki/Mechanitor)
corroborates that bandwidth is per mechanitor and limits controlled mechs; the
[control-group documentation](https://rimworldwiki.com/wiki/Mech_Control_Groups)
describes groups as independently ordered units. These reinforce the separation
between roster assignment and group/work-mode ownership.

## Licensing

No third-party source code or assets were copied. Workshop pages and community
posts were used only to identify product overlap and compatibility surfaces.
Their pages do not grant a reusable code license, so their implementations are
treated as unavailable. RimWorld decompilation was used only to identify API
contracts and patch points; the shipped mod contains an original implementation
and requires the player's RimWorld/Biotech installation. Spine is referenced as
a runtime dependency and is not bundled.
