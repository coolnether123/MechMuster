using System.Collections.Generic;
using Spine.UI.SettingsFramework;

namespace MechMuster.Settings
{
    internal static class MechMusterSettingsRegistry
    {
        internal static readonly IReadOnlyList<SettingDefinition> Definitions =
            new[]
            {
                new SettingDefinition
                {
                    Id = "automation.header",
                    Type = SettingType.Header,
                    Label = "Automation",
                    LabelKey = "MechMuster_Settings_Automation",
                    SortOrder = 0,
                    ShowInSimpleView = true
                },
                new SettingDefinition
                {
                    Id = "automation.global",
                    FieldName = nameof(
                        MechMusterSettings.GlobalAutomationEnabled),
                    ScribeKey = "globalAutomationEnabled",
                    Type = SettingType.Bool,
                    Label = "Enable automatic muster",
                    LabelKey = "MechMuster_Settings_Global",
                    TooltipKey = "MechMuster_Settings_Global_Tip",
                    DefaultValue = true,
                    SortOrder = 10,
                    ShowInSimpleView = true
                },
                new SettingDefinition
                {
                    Id = "feedback.messages",
                    FieldName = nameof(
                        MechMusterSettings.ShowAssignmentMessages),
                    ScribeKey = "showAssignmentMessages",
                    Type = SettingType.Bool,
                    Label = "Show assignment messages",
                    LabelKey = "MechMuster_Settings_Messages",
                    TooltipKey = "MechMuster_Settings_Messages_Tip",
                    DefaultValue = true,
                    SortOrder = 20,
                    ShowInSimpleView = true
                }
            };

        internal static readonly SettingsHierarchy Hierarchy =
            new SettingsHierarchy(Definitions);
    }
}
