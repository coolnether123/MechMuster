using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MechMuster.Roster
{
    public sealed class MechanitorPlan : IExposable
    {
        public Pawn Mechanitor;
        public bool AutomationEnabled = true;
        public List<MechTarget> Targets = new List<MechTarget>();

        public MechanitorPlan()
        {
        }

        public MechanitorPlan(Pawn mechanitor)
        {
            Mechanitor = mechanitor;
        }

        public MechTarget TargetFor(PawnKindDef kind, bool create)
        {
            MechTarget target = Targets.FirstOrDefault(item =>
                item?.MechKind == kind);
            if (target == null && create)
            {
                target = new MechTarget(kind);
                Targets.Add(target);
            }

            return target;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Mechanitor, "mechanitor");
            Scribe_Values.Look(
                ref AutomationEnabled,
                "automationEnabled",
                true);
            Scribe_Collections.Look(
                ref Targets,
                "targets",
                LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Targets == null)
                {
                    Targets = new List<MechTarget>();
                }

                Targets.RemoveAll(item => item?.MechKind == null);
            }
        }
    }
}
