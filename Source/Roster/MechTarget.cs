using Verse;

namespace MechMuster.Roster
{
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
                Desired = System.Math.Max(0, Desired);
                Priority = System.Math.Max(0, Priority);
            }
        }
    }
}
