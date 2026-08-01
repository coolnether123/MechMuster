using System.Linq;
using LudeonTK;
using MechMuster.Bootstrap;
using MechMuster.Presentation;
using MechMuster.Roster;
using MechMuster.Runtime;
using RimWorld;
using Verse;

namespace MechMuster.Diagnostics
{
    internal static class MechMusterDebugActions
    {
        [DebugAction(
            "Mech Muster",
            "Run Mech Muster live probe",
            actionType = DebugActionType.Action)]
        private static void RunLiveProbe()
        {
            Map map = Find.CurrentMap;
            PawnKindDef mechKind = DefDatabase<PawnKindDef>
                .GetNamedSilentFail("Mech_Lifter");
            MechMusterGameComponent component =
                Current.Game?.GetComponent<MechMusterGameComponent>();
            if (map == null || mechKind == null || component == null)
            {
                Log.Warning(
                    "[Mech Muster] liveProbe=failed missing map, Biotech " +
                    "lifter, or game component");
                return;
            }

            Pawn first = SpawnMechanitor(map);
            Pawn second = SpawnMechanitor(map);
            if (first?.mechanitor == null || second?.mechanitor == null)
            {
                Log.Warning(
                    "[Mech Muster] liveProbe=failed could not create " +
                    "mechanitors");
                return;
            }

            MechanitorPlan firstPlan = component.PlanFor(first, true);
            MechanitorPlan secondPlan = component.PlanFor(second, true);
            firstPlan.AutomationEnabled = true;
            secondPlan.AutomationEnabled = true;
            MechTarget firstTarget = firstPlan.TargetFor(mechKind, true);
            MechTarget secondTarget = secondPlan.TargetFor(mechKind, true);
            firstTarget.Desired = 1;
            secondTarget.Desired = 1;
            firstTarget.Priority = 0;
            secondTarget.Priority = 0;

            Pawn preserved = SpawnMech(map, mechKind);
            Pawn fairCandidate = SpawnMech(map, mechKind);
            Pawn extra = SpawnMech(map, mechKind);
            if (preserved == null || fairCandidate == null || extra == null)
            {
                Log.Warning(
                    "[Mech Muster] liveProbe=failed could not spawn mechs");
                return;
            }
            first.relations.AddDirectRelation(
                PawnRelationDefOf.Overseer,
                preserved);
            MechanitorControlGroup originalGroup =
                preserved.GetMechControlGroup();

            bool originalGlobal =
                MechMusterMod.Settings.GlobalAutomationEnabled;
            bool originalMessages =
                MechMusterMod.Settings.ShowAssignmentMessages;
            MechMusterMod.Settings.GlobalAutomationEnabled = true;
            MechMusterMod.Settings.ShowAssignmentMessages = true;
            PawnKindDef unrequestedKind = MusterAssignmentService
                .ControllableMechKinds()
                .FirstOrDefault(kind => kind != mechKind);
            Pawn newUnrequested = unrequestedKind == null
                ? null
                : SpawnMech(map, unrequestedKind);
            MusterAssignmentService.HandleNewMech(newUnrequested);
            bool newMechUnrequested =
                newUnrequested != null &&
                newUnrequested.GetOverseer() == null;
            MechMusterMod.Settings.ShowAssignmentMessages = false;
            MusterAssignmentService.RunAutomatic();

            bool noSteal = preserved.GetOverseer() == first;
            bool groupPreserved =
                preserved.GetMechControlGroup() == originalGroup;
            bool fairAssignment = fairCandidate.GetOverseer() == second;
            bool unrequested = extra.GetOverseer() == null;

            secondPlan.AutomationEnabled = false;
            firstTarget.Desired = 2;
            MusterAssignmentService.RunAutomatic();
            bool perAutomation =
                extra.GetOverseer() == first &&
                second.mechanitor.OverseenPawns.Count == 1;

            Pawn globalOffCandidate = SpawnMech(map, mechKind);
            firstTarget.Desired = 3;
            MechMusterMod.Settings.GlobalAutomationEnabled = false;
            MusterAssignmentService.RunAutomatic();
            bool globalOff = globalOffCandidate.GetOverseer() == null;
            int manualCount = MusterAssignmentService.RunFor(first);
            bool manual =
                manualCount == 1 &&
                globalOffCandidate.GetOverseer() == first;

            MechMusterMod.Settings.GlobalAutomationEnabled = originalGlobal;
            MechMusterMod.Settings.ShowAssignmentMessages = originalMessages;
            MechMusterMod.Settings.Write();

            bool complete = noSteal && groupPreserved && fairAssignment &&
                unrequested && newMechUnrequested && perAutomation &&
                globalOff && manual;
            string detail =
                "[Mech Muster] liveProbe=" +
                (complete ? "complete" : "failed") +
                " noSteal=" + noSteal.ToString().ToLowerInvariant() +
                " groupPreserved=" +
                groupPreserved.ToString().ToLowerInvariant() +
                " fairAssignment=" +
                fairAssignment.ToString().ToLowerInvariant() +
                " unrequested=" +
                unrequested.ToString().ToLowerInvariant() +
                " newMechUnrequested=" +
                newMechUnrequested.ToString().ToLowerInvariant() +
                " perAutomation=" +
                perAutomation.ToString().ToLowerInvariant() +
                " globalOff=" + globalOff.ToString().ToLowerInvariant() +
                " manual=" + manual.ToString().ToLowerInvariant() +
                " mechKinds=" +
                MusterAssignmentService.ControllableMechKinds().Count;
            if (complete)
            {
                Log.Message(detail);
            }
            else
            {
                Log.Warning(detail);
            }
        }

        [DebugAction(
            "Mech Muster",
            "Open Mech Muster window",
            actionType = DebugActionType.Action)]
        private static void OpenWindow()
        {
            Pawn mechanitor = FirstMechanitor();
            if (mechanitor == null)
            {
                Log.Warning(
                    "[Mech Muster] openWindow=failed no mechanitor");
                return;
            }

            Find.WindowStack.Add(new Dialog_MechMuster(mechanitor));
            Log.Message(
                "[Mech Muster] openWindow=complete mechanitor=" +
                mechanitor.ThingID);
        }

        [DebugAction(
            "Mech Muster",
            "Select Mech Muster mechanitor",
            actionType = DebugActionType.Action)]
        private static void SelectMechanitor()
        {
            Pawn mechanitor = FirstMechanitor();
            if (mechanitor == null)
            {
                Log.Warning(
                    "[Mech Muster] selectMechanitor=failed no mechanitor");
                return;
            }

            Find.Selector.ClearSelection();
            Find.Selector.Select(mechanitor);
            CameraJumper.TryJump(mechanitor);
            Log.Message(
                "[Mech Muster] selectMechanitor=complete mechanitor=" +
                mechanitor.ThingID);
        }

        [DebugAction(
            "Mech Muster",
            "Set UI scale 1.25",
            actionType = DebugActionType.Action)]
        private static void SetUiScaleOnePointTwoFive()
        {
            SetUiScale(1.25f);
        }

        [DebugAction(
            "Mech Muster",
            "Reset UI scale 1.0",
            actionType = DebugActionType.Action)]
        private static void ResetUiScale()
        {
            SetUiScale(1f);
        }

        [DebugAction(
            "Mech Muster",
            "Log Mech Muster state",
            actionType = DebugActionType.Action)]
        private static void LogState()
        {
            MechMusterGameComponent component =
                Current.Game?.GetComponent<MechMusterGameComponent>();
            int targetCount = component?.Plans.Sum(plan =>
                plan?.Targets?.Count ?? 0) ?? 0;
            Log.Message(
                "[Mech Muster] state plans=" +
                (component?.Plans.Count ?? 0) +
                " targets=" + targetCount +
                " mechKinds=" +
                MusterAssignmentService.ControllableMechKinds().Count +
                " globalAutomation=" +
                (MechMusterMod.Settings?.GlobalAutomationEnabled == true)
                    .ToString().ToLowerInvariant());
        }

        private static Pawn SpawnMechanitor(Map map)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer);
            if (!TrySpawn(pawn, map))
            {
                return null;
            }

            pawn.health.AddHediff(
                HediffDefOf.MechlinkImplant,
                pawn.health.hediffSet.GetBrain());
            PawnComponentsUtility.AddAndRemoveDynamicComponents(
                pawn,
                true);
            return pawn;
        }

        private static Pawn FirstMechanitor()
        {
            return Find.CurrentMap?.mapPawns.FreeColonists
                .Where(MechanitorUtility.IsMechanitor)
                .OrderBy(pawn => pawn.thingIDNumber)
                .FirstOrDefault();
        }

        private static void SetUiScale(float scale)
        {
            Prefs.UIScale = scale;
            UI.ApplyUIScale();
            Log.Message(
                "[Mech Muster] uiScale=complete value=" +
                scale.ToString("0.##"));
        }

        private static Pawn SpawnMech(Map map, PawnKindDef kind)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
            return TrySpawn(pawn, map) ? pawn : null;
        }

        private static bool TrySpawn(Pawn pawn, Map map)
        {
            if (pawn == null ||
                !CellFinder.TryFindRandomSpawnCellForPawnNear(
                    map.Center,
                    map,
                    out IntVec3 cell,
                    20))
            {
                return false;
            }

            GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
            return true;
        }
    }
}
