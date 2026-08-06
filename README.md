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

When a colony has several mechanitors, the gizmo opens a colony overview first.
It shows every mechanitor's automation state, total current and wanted mechs,
shortage, and requested composition in one list. A mechanitor name or its
**Roster** button opens that detailed roster inside the same window, and the
**Colony overview** button returns without closing or reselecting pawns.

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
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [SpineLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3778463813) — the shared runtime used by
  CoolNether123 mods

No gameplay mod other than Biotech is required.

## Installation

Install Harmony and SpineLib, copy `MechMuster` into RimWorld's `Mods` folder,
then enable Biotech, Harmony, SpineLib, and Mech Muster in that order.

The mod is inert in saves without configured Wanted counts and never requires
another gameplay mod from the suite.

## Build and verification

Pure allocation tests live in `Tests/Mod.Tests.csproj`. The production assembly
is built through the centralized RimWorld tooling with dependencies
`harmony,spine`; the validated release allowlist is `About`,
`1.6/Assemblies/MechMuster.dll`, `Languages`, `LICENSE`, and `README.md`.

## Documentation

- [Architecture](docs/architecture.md)
- [Compatibility](docs/compatibility.md)
- [Research](docs/research.md)
- [Test contracts](docs/tests.md)
- [Verification record](docs/verification.md) and
  [`Engineering/evidence.json`](Engineering/evidence.json)

## Developer fixture

Live debug actions are isolated in `Developer/MechMuster.TestFixture`, a
separately loadable developer mod. Build and load that folder only for harness
verification; it is never part of the Mech Muster shipping package.

## License

Released under the [MIT License](LICENSE). The license notice is included in
every release package. Harmony and SpineLib are used under their own licenses.
