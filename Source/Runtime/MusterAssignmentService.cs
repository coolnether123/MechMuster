using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MechMuster.Bootstrap;
using MechMuster.Domain;
using MechMuster.Roster;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechMuster.Runtime
{
    internal static class MusterAssignmentService
    {
        internal static void RunAutomatic()
        {
            if (!ModsConfig.BiotechActive ||
                MechMusterMod.Settings?.GlobalAutomationEnabled != true)
            {
                return;
            }

            AssignUnassigned(null, false);
        }

        internal static int RunFor(Pawn mechanitor)
        {
            return AssignUnassigned(mechanitor, true);
        }

        internal static void HandleNewMech(Pawn mech)
        {
            if (mech == null ||
                MechMusterMod.Settings?.GlobalAutomationEnabled != true)
            {
                return;
            }

            Pawn initialOverseer = mech.GetOverseer();
            Pawn selected = SelectMechanitor(
                mech,
                null,
                initialOverseer,
                true);
            if (initialOverseer != null &&
                ReferenceEquals(selected, initialOverseer))
            {
                ReportAssignment(mech, selected);
                return;
            }

            if (initialOverseer != null)
            {
                initialOverseer.relations.TryRemoveDirectRelation(
                    PawnRelationDefOf.Overseer,
                    mech);
            }

            if (selected == null)
            {
                mech.OverseerSubject?.Notify_DisconnectedFromOverseer();
                if (MechMusterMod.Settings.ShowAssignmentMessages)
                {
                    Messages.Message(
                        "MechMuster_Message_Unrequested".Translate(
                            mech.LabelShortCap),
                        mech,
                        MessageTypeDefOf.NeutralEvent,
                        false);
                }

                return;
            }

            selected.relations.AddDirectRelation(
                PawnRelationDefOf.Overseer,
                mech);
            ReportAssignment(mech, selected);
        }

        internal static int CurrentCount(
            Pawn mechanitor,
            PawnKindDef kind,
            Pawn excluded = null)
        {
            if (mechanitor?.mechanitor == null || kind == null)
            {
                return 0;
            }

            return mechanitor.mechanitor.OverseenPawns.Count(mech =>
                mech != null && mech != excluded && mech.kindDef == kind);
        }

        internal static IReadOnlyList<PawnKindDef> ControllableMechKinds()
        {
            return DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(kind =>
                    kind?.race?.race?.IsMechanoid == true &&
                    kind.race.GetCompProperties<
                        CompProperties_OverseerSubject>() != null)
                .OrderBy(kind => kind.LabelCap.ToString(),
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(kind => kind.defName, StringComparer.Ordinal)
                .ToArray();
        }

        private static int AssignUnassigned(
            Pawn onlyMechanitor,
            bool ignoreGlobalSetting)
        {
            if (!ModsConfig.BiotechActive ||
                (!ignoreGlobalSetting &&
                    MechMusterMod.Settings?.GlobalAutomationEnabled != true))
            {
                return 0;
            }

            int assigned = 0;
            Pawn[] mechs = Find.Maps
                .SelectMany(map => map.mapPawns.PawnsInFaction(Faction.OfPlayer))
                .Where(mech =>
                    mech != null &&
                    mech.RaceProps.IsMechanoid &&
                    mech.OverseerSubject != null &&
                    mech.GetOverseer() == null)
                .Distinct()
                .OrderBy(mech => mech.thingIDNumber)
                .ToArray();
            for (int index = 0; index < mechs.Length; index++)
            {
                Pawn selected = SelectMechanitor(
                    mechs[index],
                    onlyMechanitor,
                    null,
                    !ignoreGlobalSetting);
                if (selected == null)
                {
                    continue;
                }

                selected.relations.AddDirectRelation(
                    PawnRelationDefOf.Overseer,
                    mechs[index]);
                assigned++;
                ReportAssignment(mechs[index], selected);
            }

            return assigned;
        }

        private static Pawn SelectMechanitor(
            Pawn mech,
            Pawn onlyMechanitor,
            Pawn initialOverseer,
            bool automaticRun)
        {
            MechMusterGameComponent component =
                Current.Game?.GetComponent<MechMusterGameComponent>();
            if (component == null || mech?.kindDef == null)
            {
                return null;
            }

            int bandwidthCost = Mathf.CeilToInt(
                mech.GetStatValue(StatDefOf.BandwidthCost));
            var pawnsById = new Dictionary<string, Pawn>(
                StringComparer.Ordinal);
            var candidates = new List<MusterCandidate>();
            foreach (MechanitorPlan plan in component.Plans)
            {
                Pawn mechanitor = plan?.Mechanitor;
                MechTarget target = plan?.TargetFor(mech.kindDef, false);
                if (mechanitor == null ||
                    target == null ||
                    !AutomationIntent.PlanEligible(
                        automaticRun,
                        plan.AutomationEnabled) ||
                    onlyMechanitor != null && mechanitor != onlyMechanitor)
                {
                    continue;
                }

                string id = mechanitor.thingIDNumber.ToString(
                    "D10",
                    CultureInfo.InvariantCulture);
                int availableBandwidth = mechanitor.mechanitor == null
                    ? 0
                    : mechanitor.mechanitor.TotalBandwidth -
                        mechanitor.mechanitor.UsedBandwidth;
                if (mechanitor == initialOverseer)
                {
                    availableBandwidth += bandwidthCost;
                }

                candidates.Add(new MusterCandidate(
                    id,
                    target.Desired,
                    CurrentCount(mechanitor, mech.kindDef, mech),
                    target.Priority,
                    availableBandwidth,
                    IsEligible(mechanitor, mech)));
                pawnsById[id] = mechanitor;
            }

            MusterCandidate? selected = MusterAllocator.Select(
                candidates,
                bandwidthCost);
            return selected.HasValue
                ? pawnsById[selected.Value.MechanitorId]
                : null;
        }

        private static bool IsEligible(Pawn mechanitor, Pawn mech)
        {
            if (mechanitor == null ||
                mech == null ||
                mechanitor.Dead ||
                !mechanitor.Spawned ||
                mechanitor.MapHeld != mech.MapHeld ||
                !MechanitorUtility.IsMechanitor(mechanitor) ||
                !(bool)mechanitor.mechanitor.CanControlMechs ||
                !mech.IsColonyMech ||
                mech.Downed ||
                mech.Dead ||
                mech.IsAttacking() ||
                !MechanitorUtility.EverControllable(mech))
            {
                return false;
            }

            return true;
        }

        private static void ReportAssignment(Pawn mech, Pawn mechanitor)
        {
            if (MechMusterMod.Settings?.ShowAssignmentMessages != true)
            {
                return;
            }

            Messages.Message(
                "MechMuster_Message_Assigned".Translate(
                    mech.LabelShortCap,
                    mechanitor.LabelShortCap),
                new LookTargets(new[] { mech, mechanitor }),
                MessageTypeDefOf.PositiveEvent,
                false);
        }
    }
}
