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
    internal sealed class Dialog_MechMuster : Window
    {
        private const float RowHeight = 34f;
        private const float CurrentColumnWidth = 76f;
        private const float WantedColumnWidth = 132f;
        private const float PriorityColumnWidth = 126f;
        private static readonly Vector2 MinimumWindowSize =
            new Vector2(660f, 520f);
        private readonly Pawn mechanitor;
        private bool showAdvanced;
        private Vector2 scrollPosition;
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

            MechanitorPlan plan = component.PlanFor(mechanitor, true);
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 34f),
                "MechMuster_Title".Translate(mechanitor.LabelShortCap));
            Text.Font = GameFont.Small;

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
