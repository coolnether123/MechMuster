using System;
using System.Collections.Generic;

namespace MechMuster.Domain
{
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

            long leftFulfilment = (long)left.Current * right.Desired;
            long rightFulfilment = (long)right.Current * left.Desired;
            result = leftFulfilment.CompareTo(rightFulfilment);
            if (result != 0)
            {
                return result;
            }

            return string.Compare(
                left.MechanitorId,
                right.MechanitorId,
                StringComparison.Ordinal);
        }
    }
}
