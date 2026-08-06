using MechMuster.Bootstrap;
using RimWorld;
using Spine.UI.ContextualSettings;
using UnityEngine;
using Verse;

namespace MechMuster.Presentation
{
    /// <summary>
    /// Gives mechanitors one native command for roster management and exposes
    /// the relevant global setting through Spine's contextual settings gesture.
    /// </summary>
    internal sealed class Command_MechMuster : Command_Action
    {
        internal Command_MechMuster(Pawn mechanitor)
        {
            defaultLabel = "MechMuster_Command".Translate();
            defaultDesc = "MechMuster_Command_Tip".Translate();
            icon = ContentFinder<Texture2D>.Get(
                "UI/Icons/SelectAllMechs");
            Order = float.MaxValue;
            action = () => Find.WindowStack.Add(
                new Dialog_MechMuster(mechanitor));
        }

        public override GizmoResult GizmoOnGUI(
            Vector2 topLeft,
            float maxWidth,
            GizmoRenderParms parms)
        {
            if (BindSettings(new Rect(
                topLeft.x,
                topLeft.y,
                GetWidth(maxWidth),
                75f)))
            {
                return new GizmoResult(GizmoState.Clear);
            }

            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }

        public override GizmoResult GizmoOnGUIShrunk(
            Vector2 topLeft,
            float size,
            GizmoRenderParms parms)
        {
            if (BindSettings(new Rect(topLeft.x, topLeft.y, size, size)))
            {
                return new GizmoResult(GizmoState.Clear);
            }

            return base.GizmoOnGUIShrunk(topLeft, size, parms);
        }

        private static bool BindSettings(Rect visibleRect)
        {
            return MechMusterMod.ContextualSettings?.Bind(
                visibleRect,
                ContextualSettingsTarget.Exact(
                    "automation.global",
                    "automation.header")) == true;
        }
    }
}
