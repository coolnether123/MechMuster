using System.Collections.Generic;
using System.Linq;
using MechMuster.Runtime;
using Verse;

namespace MechMuster.Roster
{
    /// <summary>
    /// Owns save-local plans and schedules low-frequency automatic assignment,
    /// avoiding global persistence and work on every game tick.
    /// </summary>
    public sealed class MechMusterGameComponent : GameComponent
    {
        private const int AssignmentIntervalTicks = 250;
        private List<MechanitorPlan> plans = new List<MechanitorPlan>();

        public MechMusterGameComponent(Game game)
        {
        }

        public IReadOnlyList<MechanitorPlan> Plans => plans;

        public MechanitorPlan PlanFor(Pawn mechanitor, bool create)
        {
            MechanitorPlan plan = plans.FirstOrDefault(item =>
                item?.Mechanitor == mechanitor);
            if (plan == null && create)
            {
                plan = new MechanitorPlan(mechanitor);
                plans.Add(plan);
            }

            return plan;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % AssignmentIntervalTicks == 0)
            {
                MusterAssignmentService.RunAutomatic();
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref plans, "mechanitorPlans", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (plans == null)
                {
                    plans = new List<MechanitorPlan>();
                }

                plans.RemoveAll(item => item?.Mechanitor == null);
            }
        }
    }
}
