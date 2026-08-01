using System;
using System.Collections.Generic;
using System.Linq;
using MechMuster.Domain;

namespace MechMuster.Tests
{
    internal static class Program
    {
        private static int assertions;

        private static int Main()
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
            AssertEqual(false, wantedEdit.GlobalEnabled,
                "Wanted edit must preserve explicit global-off");
            AssertEqual(true, wantedEdit.PlanEnabled,
                "Wanted edit enables the selected mechanitor plan");
            AutomationState explicitEnable =
                AutomationIntent.ForExplicitEnable();
            AssertEqual(true, explicitEnable.GlobalEnabled,
                "Explicit automation enable turns on the global gate");
            AssertEqual(true, explicitEnable.PlanEnabled,
                "Explicit automation enable turns on the plan gate");
            AssertEqual(false, AutomationIntent.PlanEligible(true, false),
                "Automatic runs honor a disabled plan");
            AssertEqual(true, AutomationIntent.PlanEligible(false, false),
                "Manual runs ignore a disabled plan");

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

            Console.WriteLine(
                "PASS: " + assertions +
                " deterministic allocation assertions");
            return 0;
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
            assertions++;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Expected '" + (expected ?? "<none>") +
                    "' but selected '" + (actual ?? "<none>") + "'.");
            }
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

            assertions++;
            string actual = string.Join(",", sequence);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Expected sequence '" + expected +
                    "' but got '" + actual + "'.");
            }
        }

        private static IEnumerable<MusterCandidate[]> Permutations(
            MusterCandidate[] values)
        {
            return Permute(values, 0);
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string contract)
        {
            assertions++;
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(contract + ".");
            }
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
