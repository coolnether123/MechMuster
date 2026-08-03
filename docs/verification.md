# Verification record (2026-08-01)

## Build, tests, and package

The centralized RimWorld build resolved RimWorld 1.6, Harmony 2.4.2, and
Spine, then completed with zero warnings and zero errors. That 2026-08-01
verified
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

## Colony overview follow-up (2026-08-02)

The roster now opens as a colony overview when the current map has more than
one living player mechanitor. The overview shows each mechanitor's automation
state, current and wanted roster totals, remaining shortage, and a compact
current/desired breakdown by requested mech type. Selecting the mechanitor's
name or its Roster button opens the existing detailed editor in the same
window; the Colony overview button returns without closing the dialog. A map
with one mechanitor still opens the detailed editor directly.

The isolated test suite passed 46 assertions, including the new summary totals,
non-negative shortage behavior, invalid negative-input clamping, and an empty
target list. The centralized Release build completed with zero warnings and
zero errors. `Test-RwtPackage` returned `RWT-BUILD-PACKAGE-VALID`. That
overview runtime assembly is 38,400 bytes with SHA-256
`37A486D18D5B35DD5EB5F74824DCFFA1255FA63FE25A86D9EA34C1EFDF302147`.

The live run used session `MechMuster-33aba0ee6385451fac829d557de0ac1f`
with Core, Harmony, RimWorld Agent, Biotech, Spine, and Mech Muster. Two real
mechanitors were created on one map. Ordinary activation of the rightmost Mech
Muster gizmo opened exactly one overview window. The captured interface showed
both mechanitors, all columns and buttons without clipping or overlap at UI
scale 1.0. Saving as `MechMusterOverview`, loading through the normal game path,
reselecting a mechanitor, and reopening the gizmo produced the same overview.

Harmony inspection still reported exactly three Mech Muster patches. The final
Player.log contained no `Error` or `Exception` matches and has SHA-256
`C520466002AACD1336E1ACFE5D1F04FC4E2F6747EDF6F7249C2A7139022468D1`.
Capture paths and hashes are recorded in
`Engineering/overview-evidence-20260802.json`; the harness session stopped
cleanly with exit code 0 and no forced termination.

## Current release candidate hardening (2026-08-02)

The centralized-service candidate rebuild completed with zero warnings and
zero errors against Spine SHA-256
`3E857A09793BBFF839D0C18D197E480C9365B6384148F49F48669F068BBB9086`.
The current `MechMuster.dll` is 31,232 bytes, has assembly version 1.0.0.0,
and has SHA-256
`640E742F06E25D01FC1CD7A5A9BB2914AE676AD202937010A4C8EEF383D37392`.
The shared test runner reports the complete 46 assertions, and
`Test-RwtPackage` returns `RWT-BUILD-PACKAGE-VALID`. The shipping package has
one DLL and excludes `Developer/MechMuster.TestFixture`; the fixture source and
metadata remain available to developers. The runtime records above remain
bound to their exact historical hashes, so the parent release pass must record
the final combined launch for this candidate.
## Final release-candidate gate — 2026-08-03

Passed its 46-assertion deterministic roster suite, clean build, and package
checks. A live Biotech colony with two mechanitors opened the colony-wide
overview, showed both mechanitors and roster entry points cleanly, and kept its
gizmo to the right of vanilla controls. The all-suite compatibility stacks
produced no Mech Muster exception.
