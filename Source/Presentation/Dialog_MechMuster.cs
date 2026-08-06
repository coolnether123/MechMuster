using System;
using System.Linq;
using MechMuster.Bootstrap;
using MechMuster.Domain;
using MechMuster.Roster;
using MechMuster.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechMuster.Presentation
{
    /// <summary>
    /// Presents colony and per-mechanitor roster editing in one window, keeping
    /// dynamic mech discovery and player intent at the UI boundary.
    /// </summary>
    internal sealed class Dialog_MechMuster : Window
    {
        private const float RowHeight = 34f;
        private const float CurrentColumnWidth = 76f;
        private const float WantedColumnWidth = 132f;
        private const float PriorityColumnWidth = 126f;
        private const float OverviewRowHeight = 48f;
        private static readonly Vector2 MinimumWindowSize =
            new Vector2(660f, 520f);
        private Pawn mechanitor;
        private bool showAdvanced;
        private bool? showOverview;
        private Vector2 scrollPosition;
        private Vector2 overviewScrollPosition;
        private string search = string.Empty;

        internal Dialog_MechMuster(Pawn mechanitor)
        {
            this.mechanitor = mechanitor;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            absorbInputAroundWindow = false;
            forcePause = false;
        }

        public override Vector2 InitialSize => new Vector2(760f, 650f);

        public override void WindowOnGUI()
        {
            float safeWidth = Mathf.Min(MinimumWindowSize.x, UI.screenWidth);
            float safeHeight = Mathf.Min(MinimumWindowSize.y, UI.screenHeight);
            if (windowRect.width < safeWidth)
            {
                windowRect.x = Mathf.Clamp(
                    windowRect.x,
                    0f,
                    UI.screenWidth - safeWidth);
                windowRect.width = safeWidth;
            }
            if (windowRect.height < safeHeight)
            {
                windowRect.y = Mathf.Clamp(
                    windowRect.y,
                    0f,
                    UI.screenHeight - safeHeight);
                windowRect.height = safeHeight;
            }

            base.WindowOnGUI();
        }

        public override void DoWindowContents(Rect inRect)
        {
            MechMusterGameComponent component =
                Current.Game?.GetComponent<MechMusterGameComponent>();
            if (component == null || mechanitor == null)
            {
                Widgets.Label(inRect, "MechMuster_NoGame".Translate());
                return;
            }

            Map colonyMap = mechanitor.MapHeld ?? Find.CurrentMap;
            Pawn[] mechanitors = colonyMap == null
                ? new[] { mechanitor }
                : colonyMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                    .Where(pawn =>
                        pawn != null &&
                        !pawn.Dead &&
                        MechanitorUtility.IsMechanitor(pawn))
                    .Append(mechanitor)
                    // The dialog may remain open while its original pawn is
                    // temporarily absent from the map; keep that plan reachable.
                    .Distinct()
                    .OrderBy(pawn => pawn.LabelShortCap.ToString(),
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(pawn => pawn.thingIDNumber)
                    .ToArray();
            if (!showOverview.HasValue)
            {
                showOverview = mechanitors.Length > 1;
            }

            if (showOverview == true)
            {
                DrawOverview(inRect, component, mechanitors);
                return;
            }

            MechanitorPlan plan = component.PlanFor(mechanitor, true);
            Text.Font = GameFont.Medium;
            const float overviewButtonWidth = 156f;
            float titleWidth = mechanitors.Length > 1
                ? inRect.width - overviewButtonWidth - 12f
                : inRect.width;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, titleWidth, 34f),
                "MechMuster_Title".Translate(mechanitor.LabelShortCap));
            Text.Font = GameFont.Small;
            if (mechanitors.Length > 1 && Widgets.ButtonText(
                new Rect(
                    inRect.xMax - overviewButtonWidth,
                    inRect.y,
                    overviewButtonWidth,
                    30f),
                "MechMuster_Overview_Button".Translate()))
            {
                showOverview = true;
                return;
            }

            bool automaticAssignments =
                MechMusterMod.Settings.GlobalAutomationEnabled &&
                plan.AutomationEnabled;
            bool previousAutomaticAssignments = automaticAssignments;
            Rect automationRect = new Rect(
                inRect.x,
                inRect.y + 42f,
                inRect.width,
                28f);
            Widgets.CheckboxLabeled(
                automationRect,
                "MechMuster_Automation".Translate(),
                ref automaticAssignments);
            TooltipHandler.TipRegion(
                automationRect,
                "MechMuster_Automation_Tip".Translate());
            if (automaticAssignments != previousAutomaticAssignments)
            {
                if (automaticAssignments)
                {
                    // Explicitly enabling here is stronger than editing a wanted
                    // count, so it restores both the global and per-plan switches.
                    AutomationState state =
                        AutomationIntent.ForExplicitEnable();
                    plan.AutomationEnabled = state.PlanEnabled;
                    MechMusterMod.Settings.GlobalAutomationEnabled =
                        state.GlobalEnabled;
                    MechMusterMod.Settings.Write();
                }
                else
                {
                    plan.AutomationEnabled = false;
                    MechMusterMod.Settings.Write();
                }
            }

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 72f, inRect.width, 26f),
                "MechMuster_Guidance".Translate());

            Widgets.Label(
                new Rect(inRect.x, inRect.y + 104f, 72f, 26f),
                "MechMuster_Search".Translate());
            Rect advancedRect = new Rect(
                inRect.xMax - 116f,
                inRect.y + 102f,
                116f,
                28f);
            float searchWidth = Mathf.Max(
                80f,
                advancedRect.x - (inRect.x + 74f) - 12f);
            search = Widgets.TextField(
                new Rect(
                    inRect.x + 74f,
                    inRect.y + 102f,
                    searchWidth,
                    28f),
                search ?? string.Empty);
            Widgets.CheckboxLabeled(
                advancedRect,
                "MechMuster_Advanced".Translate(),
                ref showAdvanced);
            TooltipHandler.TipRegion(
                advancedRect,
                "MechMuster_Advanced_Tip".Translate());

            float tableY = inRect.y + 140f;
            Rect headerRect = new Rect(
                inRect.x,
                tableY,
                inRect.width,
                28f);
            Widgets.DrawMenuSection(headerRect);
            float priorityWidth = showAdvanced ? PriorityColumnWidth : 0f;
            float typeWidth = headerRect.width - CurrentColumnWidth -
                WantedColumnWidth - priorityWidth;
            DrawLabel(
                new Rect(
                    headerRect.x + 8f,
                    headerRect.y,
                    typeWidth - 8f,
                    28f),
                "MechMuster_Column_Type".Translate(),
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(
                    headerRect.x + typeWidth,
                    headerRect.y,
                    CurrentColumnWidth,
                    28f),
                "MechMuster_Column_Count".Translate(),
                TextAnchor.MiddleCenter);
            Rect wantedHeader = new Rect(
                headerRect.x + typeWidth + CurrentColumnWidth,
                headerRect.y,
                WantedColumnWidth,
                28f);
            DrawLabel(
                wantedHeader,
                "MechMuster_Column_Desired".Translate(),
                TextAnchor.MiddleCenter);
            TooltipHandler.TipRegion(
                wantedHeader,
                "MechMuster_Wanted_Tip".Translate());
            if (showAdvanced)
            {
                Rect priorityHeader = new Rect(
                    wantedHeader.xMax,
                    headerRect.y,
                    PriorityColumnWidth,
                    28f);
                DrawLabel(
                    priorityHeader,
                    "MechMuster_Column_Priority".Translate(),
                    TextAnchor.MiddleCenter);
                TooltipHandler.TipRegion(
                    priorityHeader,
                    "MechMuster_Priority_Tip".Translate());
            }

            PawnKindDef[] loadedKinds = MusterAssignmentService
                .ControllableMechKinds()
                .ToArray();
            PawnKindDef[] kinds = loadedKinds
                .Where(kind =>
                    search.NullOrEmpty() ||
                    kind.LabelCap.ToString().IndexOf(
                        search,
                        StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    kind.defName.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            string footerText = "MechMuster_NoRebalance".Translate();
            const float musterWidth = 180f;
            const float footerGap = 12f;
            const float footerPadding = 4f;
            float footerTextWidth = Mathf.Max(
                1f,
                inRect.width - musterWidth - footerGap);
            float footerTextHeight = Mathf.Ceil(
                Text.CalcHeight(footerText, footerTextWidth));
            float footerHeight = Mathf.Max(34f, footerTextHeight) +
                footerPadding * 2f;
            float footerY = inRect.yMax - footerHeight;

            Rect outer = new Rect(
                inRect.x,
                tableY + 30f,
                inRect.width,
                Mathf.Max(1f, footerY - (tableY + 30f) - 8f));
            Rect view = new Rect(
                0f,
                0f,
                outer.width - 18f,
                Math.Max(outer.height, Math.Max(1, kinds.Length) * RowHeight));
            Widgets.BeginScrollView(outer, ref scrollPosition, view);
            if (kinds.Length == 0)
            {
                string emptyKey = loadedKinds.Length == 0
                    ? "MechMuster_NoMechTypes"
                    : "MechMuster_NoSearchResults";
                DrawLabel(
                    new Rect(8f, 0f, view.width - 16f, RowHeight),
                    emptyKey.Translate(),
                    TextAnchor.MiddleLeft);
            }
            else
            {
                for (int index = 0; index < kinds.Length; index++)
                {
                    DrawRow(
                        new Rect(0f, index * RowHeight, view.width, RowHeight),
                        plan,
                        kinds[index],
                        index % 2 == 1);
                }
            }
            Widgets.EndScrollView();

            Rect musterRect = new Rect(
                inRect.x,
                footerY + (footerHeight - 34f) * 0.5f,
                musterWidth,
                34f);
            if (Widgets.ButtonText(
                musterRect,
                "MechMuster_MusterNow".Translate()))
            {
                int count = MusterAssignmentService.RunFor(mechanitor);
                Messages.Message(
                    "MechMuster_Message_MusterComplete".Translate(count),
                    mechanitor,
                    MessageTypeDefOf.TaskCompletion,
                    false);
            }

            Widgets.Label(
                new Rect(
                    musterRect.xMax + footerGap,
                    footerY + footerPadding,
                    footerTextWidth,
                    footerTextHeight),
                footerText);
        }

        private void DrawOverview(
            Rect inRect,
            MechMusterGameComponent component,
            Pawn[] mechanitors)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 34f),
                "MechMuster_Overview_Title".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 38f, inRect.width, 26f),
                "MechMuster_Overview_Guidance".Translate());

            const float nameWidth = 154f;
            const float automationWidth = 86f;
            const float countWidth = 64f;
            const float missingWidth = 70f;
            const float openWidth = 88f;
            float requestsWidth = Mathf.Max(
                96f,
                inRect.width - 18f - nameWidth - automationWidth -
                    countWidth * 2f - missingWidth - openWidth);
            float headerY = inRect.y + 70f;
            Rect header = new Rect(inRect.x, headerY, inRect.width, 28f);
            Widgets.DrawMenuSection(header);
            float x = header.x;
            DrawLabel(
                new Rect(x + 8f, header.y, nameWidth - 8f, header.height),
                "MechMuster_Overview_Column_Mechanitor".Translate(),
                TextAnchor.MiddleLeft);
            x += nameWidth;
            DrawLabel(
                new Rect(x, header.y, automationWidth, header.height),
                "MechMuster_Overview_Column_Automation".Translate(),
                TextAnchor.MiddleCenter);
            x += automationWidth;
            DrawLabel(
                new Rect(x, header.y, countWidth, header.height),
                "MechMuster_Column_Count".Translate(),
                TextAnchor.MiddleCenter);
            x += countWidth;
            DrawLabel(
                new Rect(x, header.y, countWidth, header.height),
                "MechMuster_Column_Desired".Translate(),
                TextAnchor.MiddleCenter);
            x += countWidth;
            DrawLabel(
                new Rect(x, header.y, missingWidth, header.height),
                "MechMuster_Overview_Column_Missing".Translate(),
                TextAnchor.MiddleCenter);
            x += missingWidth;
            DrawLabel(
                new Rect(x + 6f, header.y, requestsWidth - 6f, header.height),
                "MechMuster_Overview_Column_Requests".Translate(),
                TextAnchor.MiddleLeft);

            const float footerHeight = 38f;
            Rect outer = new Rect(
                inRect.x,
                header.yMax + 4f,
                inRect.width,
                Mathf.Max(
                    1f,
                    inRect.yMax - header.yMax - footerHeight - 10f));
            Rect view = new Rect(
                0f,
                0f,
                outer.width - 18f,
                Math.Max(
                    outer.height,
                    Math.Max(1, mechanitors.Length) * OverviewRowHeight));
            Widgets.BeginScrollView(
                outer,
                ref overviewScrollPosition,
                view);

            int colonyCurrent = 0;
            int colonyDesired = 0;
            int colonyMissing = 0;
            if (mechanitors.Length == 0)
            {
                DrawLabel(
                    new Rect(8f, 0f, view.width - 16f, OverviewRowHeight),
                    "MechMuster_Overview_Empty".Translate(),
                    TextAnchor.MiddleLeft);
            }
            else
            {
                for (int index = 0; index < mechanitors.Length; index++)
                {
                    Pawn pawn = mechanitors[index];
                    MechanitorPlan plan = component.PlanFor(pawn, false);
                    MusterTargetCount[] targetCounts = plan?.Targets
                        .Where(target =>
                            target?.MechKind != null &&
                            target.Desired > 0)
                        .Select(target => new MusterTargetCount(
                            MusterAssignmentService.CurrentCount(
                                pawn,
                                target.MechKind),
                            target.Desired))
                        .ToArray() ?? Array.Empty<MusterTargetCount>();
                    int currentRoster = pawn.mechanitor?.OverseenPawns
                        .Count(mech => mech != null) ?? 0;
                    MusterRosterMetrics metrics =
                        MusterRosterMetrics.Calculate(
                            currentRoster,
                            targetCounts);
                    colonyCurrent += metrics.Current;
                    colonyDesired += metrics.Desired;
                    colonyMissing += metrics.Missing;
                    DrawOverviewRow(
                        new Rect(
                            0f,
                            index * OverviewRowHeight,
                            view.width,
                            OverviewRowHeight),
                        pawn,
                        plan,
                        metrics,
                        nameWidth,
                        automationWidth,
                        countWidth,
                        missingWidth,
                        requestsWidth,
                        openWidth,
                        index % 2 == 1);
                }
            }
            Widgets.EndScrollView();

            string summary = "MechMuster_Overview_Summary".Translate(
                mechanitors.Length,
                colonyCurrent,
                colonyDesired,
                colonyMissing);
            DrawLabel(
                new Rect(
                    inRect.x,
                    inRect.yMax - footerHeight + 4f,
                    inRect.width,
                    footerHeight - 4f),
                summary,
                TextAnchor.MiddleLeft);
        }

        private void DrawOverviewRow(
            Rect row,
            Pawn pawn,
            MechanitorPlan plan,
            MusterRosterMetrics metrics,
            float nameWidth,
            float automationWidth,
            float countWidth,
            float missingWidth,
            float requestsWidth,
            float openWidth,
            bool alternate)
        {
            if (alternate)
            {
                Widgets.DrawLightHighlight(row);
            }

            float x = row.x;
            Rect nameRect = new Rect(
                x + 6f,
                row.y + 7f,
                nameWidth - 12f,
                row.height - 14f);
            if (Widgets.ButtonTextSubtle(
                nameRect,
                pawn.LabelShortCap,
                textLeftMargin: 6f))
            {
                OpenRoster(pawn);
            }
            x += nameWidth;
            bool automation =
                MechMusterMod.Settings.GlobalAutomationEnabled &&
                (plan?.AutomationEnabled ?? true);
            DrawLabel(
                new Rect(x, row.y, automationWidth, row.height),
                automation
                    ? "MechMuster_Overview_Automation_On".Translate()
                    : "MechMuster_Overview_Automation_Off".Translate(),
                TextAnchor.MiddleCenter);
            x += automationWidth;
            DrawLabel(
                new Rect(x, row.y, countWidth, row.height),
                metrics.Current.ToString(),
                TextAnchor.MiddleCenter);
            x += countWidth;
            DrawLabel(
                new Rect(x, row.y, countWidth, row.height),
                metrics.Desired.ToString(),
                TextAnchor.MiddleCenter);
            x += countWidth;
            DrawLabel(
                new Rect(x, row.y, missingWidth, row.height),
                metrics.Missing.ToString(),
                TextAnchor.MiddleCenter);
            x += missingWidth;

            string requests = RequestedRosterText(pawn, plan);
            Rect requestsRect = new Rect(
                x + 6f,
                row.y,
                requestsWidth - 12f,
                row.height);
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.LabelFit(requestsRect, requests);
            Text.Anchor = previousAnchor;
            TooltipHandler.TipRegion(requestsRect, requests);
            x += requestsWidth;

            if (Widgets.ButtonText(
                new Rect(
                    x + 4f,
                    row.y + 7f,
                    openWidth - 8f,
                    row.height - 14f),
                "MechMuster_Overview_Open".Translate()))
            {
                OpenRoster(pawn);
            }
        }

        private static string RequestedRosterText(
            Pawn pawn,
            MechanitorPlan plan)
        {
            string[] requests = plan?.Targets
                .Where(target =>
                    target?.MechKind != null &&
                    target.Desired > 0)
                .OrderBy(target => target.MechKind.LabelCap.ToString(),
                    StringComparer.CurrentCultureIgnoreCase)
                .Select(target =>
                    target.MechKind.LabelCap.ToString() + " " +
                    MusterAssignmentService.CurrentCount(
                        pawn,
                        target.MechKind) + "/" + target.Desired)
                .ToArray() ?? Array.Empty<string>();
            return requests.Length == 0
                ? "MechMuster_Overview_NoRequests".Translate()
                : string.Join(", ", requests);
        }

        private void OpenRoster(Pawn pawn)
        {
            mechanitor = pawn;
            showOverview = false;
            scrollPosition = Vector2.zero;
            search = string.Empty;
        }

        private void DrawRow(
            Rect row,
            MechanitorPlan plan,
            PawnKindDef kind,
            bool alternate)
        {
            if (alternate)
            {
                Widgets.DrawLightHighlight(row);
            }

            MechTarget target = plan.TargetFor(kind, true);
            int current = MusterAssignmentService.CurrentCount(
                mechanitor,
                kind);
            float priorityWidth = showAdvanced ? PriorityColumnWidth : 0f;
            float typeWidth = row.width - CurrentColumnWidth -
                WantedColumnWidth - priorityWidth;
            DrawLabel(
                new Rect(row.x + 8f, row.y, typeWidth - 8f, row.height),
                kind.LabelCap,
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(
                    row.x + typeWidth,
                    row.y,
                    CurrentColumnWidth,
                    row.height),
                current.ToString(),
                TextAnchor.MiddleCenter);

            float wantedX = row.x + typeWidth + CurrentColumnWidth;
            Rect wantedMinus = new Rect(
                wantedX + 14f,
                row.y + 4f,
                28f,
                26f);
            Rect wantedValue = new Rect(
                wantedX + 44f,
                row.y,
                40f,
                row.height);
            Rect wantedPlus = new Rect(
                wantedX + 86f,
                row.y + 4f,
                28f,
                26f);
            if (Widgets.ButtonText(wantedMinus, "−") && target.Desired > 0)
            {
                target.Desired--;
            }
            DrawLabel(
                wantedValue,
                target.Desired.ToString(),
                TextAnchor.MiddleCenter);
            if (Widgets.ButtonText(wantedPlus, "+"))
            {
                bool firstRequest = target.Desired == 0;
                target.Desired++;
                if (firstRequest)
                {
                    // A first request opts this plan into automation but respects
                    // an explicit global disable made in mod settings.
                    AutomationState state = AutomationIntent.ForWantedEdit(
                        MechMusterMod.Settings.GlobalAutomationEnabled);
                    plan.AutomationEnabled = state.PlanEnabled;
                }
            }

            if (!showAdvanced)
            {
                return;
            }

            float priorityX = wantedX + WantedColumnWidth;
            Rect priorityMinus = new Rect(
                priorityX + 10f,
                row.y + 4f,
                28f,
                26f);
            Rect priorityValue = new Rect(
                priorityX + 40f,
                row.y,
                44f,
                row.height);
            Rect priorityPlus = new Rect(
                priorityX + 86f,
                row.y + 4f,
                28f,
                26f);
            if (Widgets.ButtonText(priorityMinus, "−") && target.Priority > 0)
            {
                target.Priority--;
            }
            DrawLabel(
                priorityValue,
                target.Priority == 0
                    ? "MechMuster_Priority_Normal".Translate()
                    : target.Priority.ToString(),
                TextAnchor.MiddleCenter);
            if (Widgets.ButtonText(priorityPlus, "+"))
            {
                target.Priority++;
            }

            TooltipHandler.TipRegion(
                new Rect(priorityX, row.y, PriorityColumnWidth, row.height),
                "MechMuster_Priority_Tip".Translate());
        }

        private static void DrawLabel(
            Rect rect,
            string text,
            TextAnchor anchor)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, text);
            Text.Anchor = previous;
        }
    }
}
