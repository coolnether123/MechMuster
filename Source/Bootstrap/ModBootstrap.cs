using MechMuster.Patches;
using MechMuster.Settings;
using Spine.Api;
using Spine.UI.ContextualSettings;
using Spine.UI.SettingsFramework;
using UnityEngine;
using Verse;

namespace MechMuster.Bootstrap
{
    public sealed class MechMusterMod : Mod
    {
        private readonly MechMusterSettings settings;
        private IModSettingsPage settingsPage;

        public MechMusterMod(ModContentPack content)
            : base(content)
        {
            SpineApi.Runtime.Require(new SpineRequirement(
                "CoolNether123.MechMuster",
                new SemanticVersion(1, 2, 0),
                SpineCapability.Settings |
                SpineCapability.HarmonyPatching |
                SpineCapability.ContextualSettings |
                SpineCapability.ModSettingsPages));

            settings = GetSettings<MechMusterSettings>();
            Settings = settings;
            Instance = this;
            MechMusterPatches.Install();
        }

        private static MechMusterMod Instance { get; set; }

        public static MechMusterSettings Settings { get; private set; }

        internal static IContextualSettingsLease ContextualSettings =>
            Instance?.GetSettingsPage().ContextualSettings;

        public override string SettingsCategory()
        {
            return "MechMuster_Name".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GetSettingsPage().Draw(inRect);
        }

        private IModSettingsPage GetSettingsPage()
        {
            if (settingsPage == null)
            {
                settingsPage = SpineApi.Settings.Acquire(
                    "CoolNether123.MechMuster",
                    this,
                    settings,
                    MechMusterSettingsRegistry.Definitions,
                    settings.Write,
                    new ModSettingsPageOptions { RowHeight = 38f });
            }

            return settingsPage;
        }
    }
}
