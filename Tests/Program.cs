using System;
using System.Collections.Generic;
using System.Linq;
using MechMuster.Domain;
using static RimWorld.ModTestSupport.Test;

namespace MechMuster.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            Start("Mech Muster contracts");
            Run("deterministic allocation and overview", AllContracts);
            return Finish();
        }

        private static void AllContracts()
        {
            AssertSelected(null, 1);
            AssertSelected(new[] { Candidate("a", 0, 0) }, 1);
            AssertSelected(new[] { Candidate("a", 2, 2) }, 1);
            AssertSelected(
                new[] { Candidate("a", 2, 0, eligible: false) }, 1);
            AssertSelected(
                new[] { Candidate("a", 2, 0, bandwidth: 0) }, 1);
            AssertSelected(
                new[]
                {
                    Candidate("low", 5, 0),
                    Candidate("high", 1, 0, priority: 2)
                },
                1,
                "high");
            AssertSelected(
                new[]
                {
                    Candidate("more-filled", 2, 1),
                    Candidate("less-filled", 4, 1)
                },
                1,
                "less-filled");
            AssertSelected(
                new[]
                {
                    Candidate("b", 2, 0),
                    Candidate("a", 2, 0)
                },
                1,
                "a");

            AssertSequence(
                new[]
                {
                    Candidate("a", 2, 0),
                    Candidate("b", 2, 0)
                },
                "a,b,a,b");
            AssertSequence(
                new[]
                {
                    Candidate("a", 4, 0),
                    Candidate("b", 2, 0)
                },
                "a,b,a,a,b,a");

            AutomationState wantedEdit =
                AutomationIntent.ForWantedEdit(false);
            Equal(false, wantedEdit.GlobalEnabled,
                "Wanted edit must preserve explicit global-off");
            Equal(true, wantedEdit.PlanEnabled,
                "Wanted edit enables the selected mechanitor plan");
            AutomationState explicitEnable =
                AutomationIntent.ForExplicitEnable();
            Equal(true, explicitEnable.GlobalEnabled,
                "Explicit automation enable turns on the global gate");
            Equal(true, explicitEnable.PlanEnabled,
                "Explicit automation enable turns on the plan gate");
            Equal(false, AutomationIntent.PlanEligible(true, false),
                "Automatic runs honor a disabled plan");
            Equal(true, AutomationIntent.PlanEligible(false, false),
                "Manual runs ignore a disabled plan");

            MusterRosterMetrics overview = MusterRosterMetrics.Calculate(
                7,
                new[]
                {
                    new MusterTargetCount(1, 4),
                    new MusterTargetCount(3, 2),
                    new MusterTargetCount(-1, -2)
                });
            Equal(7, overview.Current,
                "Overview reports the full controlled roster");
            Equal(6, overview.Desired,
                "Overview sums requested counts");
            Equal(3, overview.Missing,
                "Overview never reports negative shortages");
            MusterRosterMetrics emptyOverview =
                MusterRosterMetrics.Calculate(-4, null);
            Equal(0, emptyOverview.Current,
                "Overview clamps an invalid current count");
            Equal(0, emptyOverview.Desired,
                "Overview handles no requested types");
            Equal(0, emptyOverview.Missing,
                "Overview handles no shortages");

            MusterCandidate[] permutationSet =
            {
                Candidate("z", 5, 2, priority: 1),
                Candidate("a", 3, 1, priority: 1),
                Candidate("m", 4, 1, priority: 1),
                Candidate("disabled", 9, 0, priority: 9, eligible: false)
            };
            foreach (MusterCandidate[] permutation in
                Permutations(permutationSet))
            {
                AssertSelected(permutation, 1, "m");
            }

        }

        private static MusterCandidate Candidate(
            string id,
            int desired,
            int current,
            int priority = 0,
            int bandwidth = 99,
            bool eligible = true)
        {
            return new MusterCandidate(
                id, desired, current, priority, bandwidth, eligible);
        }

        private static void AssertSelected(
            IEnumerable<MusterCandidate> candidates,
            int bandwidth,
            string expected = null)
        {
            MusterCandidate? selected = MusterAllocator.Select(
                candidates ?? Enumerable.Empty<MusterCandidate>(),
                bandwidth);
            string actual = selected?.MechanitorId;
            Equal(
                expected,
                actual,
                "allocator selected an unexpected mechanitor");
        }

        private static void AssertSequence(
            MusterCandidate[] candidates,
            string expected)
        {
            var sequence = new List<string>();
            while (true)
            {
                MusterCandidate? selected = MusterAllocator.Select(
                    candidates,
                    1);
                if (!selected.HasValue)
                {
                    break;
                }

                sequence.Add(selected.Value.MechanitorId);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (candidates[index].MechanitorId !=
                        selected.Value.MechanitorId)
                    {
                        continue;
                    }

                    MusterCandidate old = candidates[index];
                    candidates[index] = new MusterCandidate(
                        old.MechanitorId,
                        old.Desired,
                        old.Current + 1,
                        old.Priority,
                        old.AvailableBandwidth,
                        old.Eligible);
                    break;
                }
            }

            string actual = string.Join(",", sequence);
            Equal(
                expected,
                actual,
                "allocator produced an unexpected assignment sequence");
        }

        private static IEnumerable<MusterCandidate[]> Permutations(
            MusterCandidate[] values)
        {
            return Permute(values, 0);
        }

        private static IEnumerable<MusterCandidate[]> Permute(
            MusterCandidate[] values,
            int start)
        {
            if (start == values.Length)
            {
                yield return values.ToArray();
                yield break;
            }

            for (int index = start; index < values.Length; index++)
            {
                Swap(values, start, index);
                foreach (MusterCandidate[] permutation in
                    Permute(values, start + 1))
                {
                    yield return permutation;
                }
                Swap(values, start, index);
            }
        }

        private static void Swap(
            MusterCandidate[] values,
            int left,
            int right)
        {
            MusterCandidate value = values[left];
            values[left] = values[right];
            values[right] = value;
        }
    }
}
