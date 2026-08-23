# Mech Muster 250-tick paired benchmark

This protocol isolates the recurring `MechMusterGameComponent.GameComponentTick`
path. Every 250 game ticks, the candidate arm enters `RunAutomatic`; the
baseline arm reaches the same hook but returns at the global automation gate.

The frozen workload contains eight automated mechanitor plans requesting
lifters and 64 unassigned agrihands. No candidate matches a target, so both arms
must finish with eight plans, eight targets, 64 unassigned mechs, and zero
assigned mechs. This keeps behavior equal while exercising the candidate scan.

Run `Tools/Invoke-250TickBenchmark.ps1` only when its preflight reports no other
`running` `rwa` sessions. The runner creates one frozen save, then executes two
balanced randomized blocks (`CBBC`, `BCCB`) with a fresh process for every arm.
Each trial uses the same save, payload, fixture, settings other than the one
automation toggle, 10,000 warmup ticks, and 50,000 measured ticks. The
measurement sequence surrounds five synchronous 10,000-tick batches with the
harness performance probe, yielding TPS, milliseconds per tick, process CPU,
memory, and GC evidence without requiring Dubs Performance Analyzer.

The runner rejects a payload or fixture hash mismatch and leaves every session
stopped in `finally`. Analyze paired/block deltas only after every before/after
workload report proves behavior parity and every trial log is clean.
