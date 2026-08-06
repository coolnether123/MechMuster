using System;
using System.Collections.Generic;

namespace MechMuster.Domain
{
    /// <summary>
    /// Chooses a mechanitor using pure, deterministic policy so allocation can
    /// be verified without loading RimWorld or depending on enumeration order.
    /// </summary>
    public static class MusterAllocator
    {
        public static MusterCandidate? Select(
            IEnumerable<MusterCandidate> candidates,
            int mechBandwidth)
        {
            MusterCandidate? best = null;
            foreach (MusterCandidate candidate in candidates)
            {
                if (!candidate.Eligible ||
                    candidate.Desired <= 0 ||
                    candidate.Deficit <= 0 ||
                    candidate.AvailableBandwidth < mechBandwidth ||
                    string.IsNullOrEmpty(candidate.MechanitorId))
                {
                    continue;
                }

                if (!best.HasValue || Compare(candidate, best.Value) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static int Compare(
            MusterCandidate left,
            MusterCandidate right)
        {
            int result = right.Priority.CompareTo(left.Priority);
            if (result != 0)
            {
                return result;
            }

            // Cross multiplication compares fulfilment ratios without floating-
            // point rounding changing which mechanitor wins a close tie.
            long leftFulfilment = (long)left.Current * right.Desired;
            long rightFulfilment = (long)right.Current * left.Desired;
            result = leftFulfilment.CompareTo(rightFulfilment);
            if (result != 0)
            {
                return result;
            }

            // A stable pawn-derived ID makes equal requests independent of the
            // order in which maps, plans, or UI selections were enumerated.
            return string.Compare(
                left.MechanitorId,
                right.MechanitorId,
                StringComparison.Ordinal);
        }
    }
}
