using Spine.Api;
using Verse;

namespace MechMuster.Settings
{
    /// <summary>
    /// Holds preferences that apply across saves; mechanitor-specific requests
    /// deliberately live in save data rather than this global settings object.
    /// </summary>
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
