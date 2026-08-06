namespace MechMuster.Domain
{
    /// <summary>
    /// Carries only allocation-relevant facts into the pure policy layer,
    /// shielding it from mutable Pawn and mechanitor tracker state.
    /// </summary>
    public readonly struct MusterCandidate
    {
        public MusterCandidate(
            string mechanitorId,
            int desired,
            int current,
            int priority,
            int availableBandwidth,
            bool eligible)
        {
            MechanitorId = mechanitorId;
            Desired = desired;
            Current = current;
            Priority = priority;
            AvailableBandwidth = availableBandwidth;
            Eligible = eligible;
        }

        public string MechanitorId { get; }
        public int Desired { get; }
        public int Current { get; }
        public int Priority { get; }
        public int AvailableBandwidth { get; }
        public bool Eligible { get; }
        public int Deficit => Desired - Current;
    }
}
