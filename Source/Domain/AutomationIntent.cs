namespace MechMuster.Domain
{
    public readonly struct AutomationState
    {
        public AutomationState(bool globalEnabled, bool planEnabled)
        {
            GlobalEnabled = globalEnabled;
            PlanEnabled = planEnabled;
        }

        public bool GlobalEnabled { get; }

        public bool PlanEnabled { get; }
    }

    public static class AutomationIntent
    {
        public static AutomationState ForWantedEdit(bool globalEnabled)
        {
            return new AutomationState(globalEnabled, true);
        }

        public static AutomationState ForExplicitEnable()
        {
            return new AutomationState(true, true);
        }

        public static bool PlanEligible(
            bool automaticRun,
            bool planEnabled)
        {
            return !automaticRun || planEnabled;
        }
    }
}
