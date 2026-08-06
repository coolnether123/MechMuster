using Verse;

namespace MechMuster.Roster
{
    /// <summary>
    /// Stores one mech type's desired count and allocation priority as part of a
    /// mechanitor plan, rather than attaching policy to shared PawnKindDefs.
    /// </summary>
    public sealed class MechTarget : IExposable
    {
        public PawnKindDef MechKind;
        public int Desired;
        public int Priority;

        public MechTarget()
        {
        }

        public MechTarget(PawnKindDef mechKind)
        {
            MechKind = mechKind;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref MechKind, "mechKind");
            Scribe_Values.Look(ref Desired, "desired", 0);
            Scribe_Values.Look(ref Priority, "priority", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Older or externally edited saves may contain invalid negatives;
                // normalize them before allocation and UI arithmetic consume them.
                Desired = System.Math.Max(0, Desired);
                Priority = System.Math.Max(0, Priority);
            }
        }
    }
}
