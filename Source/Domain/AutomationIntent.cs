namespace MechMuster.Domain
{
    /// <summary>
    /// Captures the two automation switches as one decision result, preventing
    /// UI actions from accidentally enabling only half of the required state.
    /// </summary>
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

    /// <summary>
    /// Encodes how explicit player actions affect automation, keeping those
    /// semantics separate from RimWorld widgets and persisted plan objects.
    /// </summary>
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
