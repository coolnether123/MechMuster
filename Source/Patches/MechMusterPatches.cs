using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MechMuster.Presentation;
using MechMuster.Runtime;
using RimWorld;
using Verse;

namespace MechMuster.Patches
{
    internal static class MechMusterPatches
    {
        private const string HarmonyId = "CoolNether123.MechMuster";
        private static bool installed;

        internal static void Install()
        {
            if (installed)
            {
                return;
            }

            var harmony = new Harmony(HarmonyId);
            harmony.Patch(
                AccessTools.Method(
                    typeof(Bill_ProductionMech),
                    nameof(Bill_ProductionMech.CreateProducts)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(NewMechPostfix)));
            harmony.Patch(
                AccessTools.Method(
                    typeof(Bill_ResurrectMech),
                    nameof(Bill_ResurrectMech.CreateProducts)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(NewMechPostfix)));
            harmony.Patch(
                AccessTools.Method(
                    typeof(Pawn_MechanitorTracker),
                    nameof(Pawn_MechanitorTracker.GetGizmos)),
                postfix: new HarmonyMethod(
                    typeof(MechMusterPatches),
                    nameof(MechanitorGizmosPostfix)));
            installed = true;
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
