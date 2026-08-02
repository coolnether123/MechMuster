using MechMuster.Patches;
using MechMuster.Settings;
using Spine.Api;
using Spine.UI.SettingsFramework;
using Verse;

namespace MechMuster.Bootstrap
{
    public sealed class MechMusterMod : SpineMod<MechMusterSettings>
    {
        public MechMusterMod(ModContentPack content)
            : base(
                content,
                "CoolNether123.MechMuster",
                new SemanticVersion(1, 0, 0),
                MechMusterSettingsRegistry.Definitions,
                SpineCapability.HarmonyPatching,
                new ModSettingsPageOptions { RowHeight = 38f })
        {
            MechMusterPatches.Install();
        }

        protected override string SettingsCategoryLabel =>
            "MechMuster_Name".Translate();
    }
}
