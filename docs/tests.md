# Test contract

The pure suite covers no request, fulfilled request, disabled/ineligible
mechanitors, insufficient bandwidth, explicit priorities, proportional
fairness, stable-ID ties, sequential fair distribution, and every permutation
of a candidate set to prove enumeration-order independence. It also proves
that editing Wanted preserves an explicit global-off, explicit automation
enable turns on both gates, automatic runs honor a disabled plan, and manual
runs intentionally ignore that per-mechanitor automation gate.

Runtime verification uses `rwa.cmd` with the reusable `biotech` profile. The
completed run confirmed the package and dependencies load, opened a real
colony, exercised the mechanitor window and live relationship/control-group
probe, saved and reloaded the populated roster, inspected Harmony ownership,
captured the roster and settings UI, and found no `Error` or `Exception` log
matches. Build success alone is not treated as runtime evidence; exact results
are in `verification.md` and `Engineering/evidence.json`.

The release package must contain only `About`, `MechMuster.dll`, `Languages`,
and the root `LICENSE`; tests, research, PDBs, logs, and Engineering evidence
stay outside the shipping package.
