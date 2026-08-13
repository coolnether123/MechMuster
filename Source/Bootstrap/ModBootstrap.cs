using MechMuster.Patches;
using MechMuster.Settings;
using Spine.Api;
using Spine.UI.SettingsFramework;
using Verse;

namespace MechMuster.Bootstrap
{
    /// <summary>
    /// Composes Spine settings and Harmony integration at startup so gameplay,
    /// persistence, and presentation remain independent of mod initialization.
    /// </summary>
    public sealed class MechMusterMod : SpineMod<MechMusterSettings>
    {
        public MechMusterMod(ModContentPack content)
            : base(
                content,
                "CoolNether123.MechMuster",
                new SemanticVersion(1, 1, 0),
                MechMusterSettingsRegistry.Schema.Definitions,
                SpineCapability.HarmonyPatching |
                SpineCapability.SettingsSchema,
                new ModSettingsPageOptions { RowHeight = 38f })
        {
            MechMusterPatches.Install();
        }

        protected override string SettingsCategoryLabel =>
            "MechMuster_Name".Translate();
    }
}
