# Mech Muster

Mech Muster gives every mechanitor a save-specific wanted count for every
controllable mech type. Newly gestated, resurrected, or currently unassigned
colony mechs can be assigned automatically to an eligible mechanitor with a
deficit.

Select a mechanitor, open the single **Mech Muster** gizmo, and set **Wanted**
with the direct −/+ controls. The native window shows a plain Current and
Wanted count for every discovered vanilla or modded mech type. Optional
priority stays behind **Advanced**; higher priorities are filled first, while
equal-priority requests are filled proportionally with stable pawn-ID ties.

Automatic muster defaults on, but is inert until a Wanted count is above zero,
and can be disabled globally or per mechanitor. Editing a Wanted count never
overrides an explicit global disable. **Muster now** is an explicit action and
works for the open mechanitor even while either automatic switch is off. Mech
Muster never rebalances or steals an existing overseer assignment, and it never
moves a mech between vanilla control groups. Manual claims and transfers remain
authoritative. If automation is on and a newly completed mech has no eligible
request, it remains unassigned.

## Requirements

- RimWorld 1.6
- Biotech
- Harmony
- Spine (`CoolNether123.Spine`)

Load Harmony and Spine before Mech Muster. No gameplay mod other than Biotech
is required.

## Build and verification

Pure allocation tests live in `Tests/Mod.Tests.csproj`. The production assembly
is built through the centralized RimWorld tooling with dependencies
`harmony,spine`; the validated release allowlist is `About`,
`1.6/Assemblies/MechMuster.dll`, `Languages`, and `LICENSE`.

See `docs/architecture.md`, `docs/compatibility.md`, `docs/research.md`, and
`docs/tests.md` for exact contracts. Completed build, package, and in-game
evidence is recorded in `docs/verification.md` and
`Engineering/evidence.json`.

Licensed under the MIT License; the license notice is included in every
release package.
