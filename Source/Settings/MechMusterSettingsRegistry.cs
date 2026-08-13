using Spine.UI.SettingsFramework;

namespace MechMuster.Settings
{
    /// <summary>
    /// Declares the small global settings surface once for Spine to render and
    /// persist, leaving per-mechanitor roster state in the game component.
    /// </summary>
    internal static class MechMusterSettingsRegistry
    {
        internal static readonly SettingsSchema<MechMusterSettings> Schema =
            new SettingsSchema<MechMusterSettings>(
                SettingsSchemaConventions.LowerCamelCase);

        static MechMusterSettingsRegistry()
        {
            var automation = Schema.Section(
                "automation.header",
                "Automation",
                "MechMuster_Settings_Automation");
            automation.Toggle("automation.global",
                settings => settings.GlobalAutomationEnabled,
                "Enable automatic muster")
                .Localized("MechMuster_Settings_Global", "MechMuster_Settings_Global_Tip");
            Schema.Root.Toggle("feedback.messages",
                settings => settings.ShowAssignmentMessages,
                "Show assignment messages")
                .Localized("MechMuster_Settings_Messages", "MechMuster_Settings_Messages_Tip");
        }
    }
}
