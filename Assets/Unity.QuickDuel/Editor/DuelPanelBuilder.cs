using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.QuickDuel.EditorTools
{
    /// <summary>
    /// Builds the PanelSettings asset DuelView's UIDocument renders through (UQD-T31): the UI was legacy
    /// uGUI Text off <c>Resources.GetBuiltinResource("LegacyRuntime.ttf")</c> with a hand-built Canvas;
    /// it is now UI Toolkit, styled by <c>Resources/UI/Duel.uss</c> and set in a font asset
    /// (<c>Resources/UI/Fonts/Cinzel.ttf</c>), read by <see cref="DuelView"/> through <c>Resources.Load</c>
    /// the same way <see cref="DuelAudio"/> reads its clips. Like <see cref="RenderPipelineBuilder"/>, this
    /// only creates the asset when it is missing, so rerunning it (and every import after it) leaves the
    /// committed asset untouched. Run from the menu or in batch mode via
    /// <c>-executeMethod Unity.QuickDuel.EditorTools.DuelPanelBuilder.BuildPanel</c>; <see cref="DuelSceneBuilder"/>
    /// runs it first so the scene is never generated without a panel for its UIDocument to use.
    /// Keeps Unity's default runtime theme style sheet (auto-assigned the first time any PanelSettings is
    /// created in a project with none, copying it to Assets/UI Toolkit/): DuelView itself is plain
    /// VisualElement/Label styled by its own Duel.uss and would need no theme, but OnlineDuelMenu's Host /
    /// Join panel (UQD-T21) is built from themed built-in controls (Button, TextField), which look and lay
    /// out wrong -- no default padding, border or background -- without one.
    /// </summary>
    public static class DuelPanelBuilder
    {
        public const string Folder = "Assets/Unity.QuickDuel/Resources/UI";
        public const string PanelPath = Folder + "/DuelPanelSettings.asset";
        public const string ResourcePath = "UI/DuelPanelSettings";

        [MenuItem("QuickDuel/Build UI Panel Settings")]
        public static void BuildPanel() => Ensure();

        /// <summary>Creates the panel settings asset if missing, matching the original Canvas's reference
        /// resolution and scale-with-screen-size behaviour (CanvasScaler, matchWidthOrHeight 0.5).</summary>
        public static PanelSettings Ensure()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel != null)
                return panel;

            Directory.CreateDirectory(Folder);
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "DuelPanelSettings";
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            // panel.themeStyleSheet is left at whatever PanelSettings.OnEnable already auto-assigned
            // (Unity's own default runtime theme) -- see the class doc for why this panel keeps it.

            AssetDatabase.CreateAsset(panel, PanelPath);
            AssetDatabase.SaveAssets();
            return panel;
        }
    }
}
