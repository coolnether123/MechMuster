using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MechMuster.Presentation;
using MechMuster.Runtime;
using RimWorld;
using Spine.Api;
using Spine.Harmony;
using Verse;

namespace MechMuster.Patches
{
    /// <summary>
    /// Connects vanilla mech lifecycle and gizmo extension points to the mod's
    /// services while keeping Harmony details outside domain and presentation.
    /// </summary>
    internal static class MechMusterPatches
    {
        private const string HarmonyId = "CoolNether123.MechMuster";
        private static readonly IHarmonyPatchInstaller Installer =
            SpineApi.Patching.CreateInstaller(HarmonyId, "[Mech Muster]");

        internal static void Install()
        {
            Installer.TryPatch(
                "gestation completion",
                AccessTools.Method(
                    typeof(Bill_ProductionMech),
                    nameof(Bill_ProductionMech.CreateProducts)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(NewMechPostfix)));
            Installer.TryPatch(
                "resurrection completion",
                AccessTools.Method(
                    typeof(Bill_ResurrectMech),
                    nameof(Bill_ResurrectMech.CreateProducts)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(NewMechPostfix)));
            Installer.TryPatch(
                "mechanitor gizmos",
                AccessTools.Method(
                    typeof(Pawn_MechanitorTracker),
                    nameof(Pawn_MechanitorTracker.GetGizmos)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(MechanitorGizmosPostfix)));
        }

        private static void NewMechPostfix(Thing __result)
        {
            MusterAssignmentService.HandleNewMech(__result as Pawn);
        }

        private static void MechanitorGizmosPostfix(
            Pawn_MechanitorTracker __instance,
            ref IEnumerable<Gizmo> __result)
        {
            Pawn mechanitor = __instance?.Pawn;
            if (mechanitor == null || mechanitor.Faction != Faction.OfPlayer)
            {
                return;
            }

            var command = new Command_MechMuster(mechanitor);
            __result = (__result ?? Enumerable.Empty<Gizmo>())
                .Concat(new Gizmo[] { command });
        }
    }
}
