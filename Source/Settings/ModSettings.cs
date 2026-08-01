using Spine.Api;
using Verse;

namespace MechMuster.Settings
{
    public sealed class MechMusterSettings : ModSettings
    {
        public bool GlobalAutomationEnabled = true;
        public bool ShowAssignmentMessages = true;

        public override void ExposeData()
        {
            SpineApi.Settings.Scribe(
                this,
                MechMusterSettingsRegistry.Definitions);
            base.ExposeData();
        }
    }
}
