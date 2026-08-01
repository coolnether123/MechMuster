# Verification record (2026-08-01)

## Build, tests, and package

The centralized RimWorld build resolved RimWorld 1.6, Harmony 2.4.2, and
Spine, then completed with zero warnings and zero errors. The shipping
`MechMuster.dll` is 32,256 bytes with SHA-256
`C81673FDD075FCC9CB6BA4C006C8AB66B41595A4746B9D254FC8FE93F9F4116E`.

The standalone allocation suite passed 40 assertions. It covers disabled,
ineligible, fulfilled, unrequested, and bandwidth-incompatible candidates;
priority; proportional fairness; stable-ID ties; sequential equal and weighted
distribution; and all 24 permutations of a four-candidate set. Focused policy
assertions prove that Wanted edits preserve global-off, explicit automation
enable turns on both gates, automatic runs honor a disabled plan, and manual
runs intentionally ignore the per-mechanitor automation gate. The exact
null-overseer regression and after-vanilla gizmo order are verified in the
live runtime record below rather than through wrapper-only unit abstractions.

`New-RwtReleasePackage` returned
`RWT-BUILD-RELEASE-PACKAGE-VALID`. Its explicit allowlist contained only
`About`, `1.6/Assemblies/MechMuster.dll`, `Languages`, and `LICENSE`. The
distributed MIT notice has SHA-256
`7942115C67A258A6F955EF971F48E7A04C8A1CDCEC8F862CED392F93DA44F05A`.

## Authoritative RimWorld run

The final run used `rwa.cmd` session
`MechMuster-d150dd76904044be947e6e72298c429d` with the isolated `biotech`
profile. Active packages were Core, Harmony, RimWorld Agent, Biotech, Spine,
and Mech Muster.

The exact in-game probe completed with:

```text
[Mech Muster] liveProbe=complete noSteal=true groupPreserved=true fairAssignment=true unrequested=true newMechUnrequested=true perAutomation=true globalOff=true manual=true mechKinds=22
```

This exercises live vanilla pawn relations and control groups, not only the
pure selector. The populated native roster rendered all 22 dynamically
discovered controllable mech kinds. Spine's settings page was opened and
closed through the ordinary mod-settings window after language initialization.

Immediately before saving, the diagnostic state was `plans=2 targets=23
mechKinds=22 globalAutomation=false`. The game saved normally, reloaded through
RimWorld's normal load path, completed load generation 1 while paused, and
reported the identical state afterward. Both the roster and Spine settings
page were reopened and captured after reload.

Harmony inspection reported exactly three Mech Muster postfixes:

- `Bill_ProductionMech.CreateProducts`
- `Bill_ResurrectMech.CreateProducts`
- `Pawn_MechanitorTracker.GetGizmos`

The final in-game log had no `Error` or `Exception` matches. Its SHA-256 is
`C33F873B004A63D6C47740A86C2B86EBDF8D735C426F38314D7F41999BA255E2`.
Capture names and hashes are recorded in `Engineering/evidence.json`; the
harness session stopped cleanly with exit code 0.

The final vanilla workflow was also exercised from a generic harness-created
mechanitor. At UI scale 1.0, semantic gizmo discovery found one enabled `Mech
Muster` command and ordinary activation reported `matched=1`, `activated=True`,
`ordinary-process-input-complete`; the resulting window was visible and was
closed by its exact type. The default roster presents only Mech type, Current,
and Wanted with direct −/+ controls; optional Priority is behind Advanced.

The resizeable roster reserves measured footer height plus bottom padding and
enforces a 660x520 safe minimum. Captures at UI scale 1.0 and 1.25 both show the
footer fully visible with no overlap or clipping. A separate final generic lane
reported zero `Error` and zero `Exception` matches and stopped cleanly. Exact
session IDs, hashes, and capture names are in `Engineering/evidence.json`.

The final reviewed lane also proved the mod command is visually rightmost after
all vanilla mechanitor gizmos. A normal semantic activation opened the roster.
Holding Alt while clicking the same gizmo opened Spine's narrow global
automation setting and did not open the roster. The real normal-click roster
capture is shipped unchanged as `About/Preview.png`; it is not a mockup.
