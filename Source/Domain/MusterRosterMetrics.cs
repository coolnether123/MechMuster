using System;
using System.Collections.Generic;

namespace MechMuster.Domain
{
    /// <summary>
    /// Represents one requested mech type as sanitized inputs for aggregate
    /// roster calculations, independent of save and presentation objects.
    /// </summary>
    internal readonly struct MusterTargetCount
    {
        internal MusterTargetCount(int current, int desired)
        {
            Current = Math.Max(0, current);
            Desired = Math.Max(0, desired);
        }

        internal int Current { get; }

        internal int Desired { get; }
    }

    /// <summary>
    /// Computes colony-overview totals in one place so the UI cannot conflate
    /// overall roster size with shortages among specifically requested types.
    /// </summary>
    internal readonly struct MusterRosterMetrics
    {
        private MusterRosterMetrics(
            int current,
            int desired,
            int missing)
        {
            Current = current;
            Desired = desired;
            Missing = missing;
        }

        internal int Current { get; }

        internal int Desired { get; }

        internal int Missing { get; }

        internal static MusterRosterMetrics Calculate(
            int currentRoster,
            IEnumerable<MusterTargetCount> targets)
        {
            int desired = 0;
            int missing = 0;
            if (targets != null)
            {
                foreach (MusterTargetCount target in targets)
                {
                    desired += target.Desired;
                    missing += Math.Max(0, target.Desired - target.Current);
                }
            }

            return new MusterRosterMetrics(
                Math.Max(0, currentRoster),
                desired,
                missing);
        }
    }
}
