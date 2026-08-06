using System.Collections.Generic;
using Spine.UI.SettingsFramework;

namespace MechMuster.Settings
{
    /// <summary>
    /// Declares the small global settings surface once for Spine to render and
    /// persist, leaving per-mechanitor roster state in the game component.
    /// </summary>
    internal static class MechMusterSettingsRegistry
    {
        internal static readonly IReadOnlyList<SettingDefinition> Definitions =
            new[]
            {
                SettingDefinitions.Header(
                    "automation.header",
                    "Automation",
                    "MechMuster_Settings_Automation"),
                SettingDefinitions.Toggle(
                    "automation.global",
                    nameof(MechMusterSettings.GlobalAutomationEnabled),
                    "Enable automatic muster",
                    "MechMuster_Settings_Global",
                    tooltipKey: "MechMuster_Settings_Global_Tip",
                    scribeKey: "globalAutomationEnabled"),
                SettingDefinitions.Toggle(
                    "feedback.messages",
                    nameof(MechMusterSettings.ShowAssignmentMessages),
                    "Show assignment messages",
                    "MechMuster_Settings_Messages",
                    tooltipKey: "MechMuster_Settings_Messages_Tip",
                    scribeKey: "showAssignmentMessages")
            };

    }
}
