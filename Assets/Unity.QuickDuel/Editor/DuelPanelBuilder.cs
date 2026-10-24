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
    /// No theme style sheet is assigned: the duel screen is built entirely from plain VisualElement/Label
    /// with its own Duel.uss, never a themed built-in control (Button, Toggle, ...), so it needs none.
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

            // PanelSettings.OnEnable auto-assigns (and, the first time in a project, auto-copies) Unity's
            // own default runtime theme if nothing clears it. This screen uses no themed built-in control
            // (Button, Toggle, ...), only plain VisualElement/Label styled by its own Duel.uss, so it needs
            // no theme; undoing the default here, every time, keeps a clean checkout's regenerated asset
            // identical to the committed one, matching the class doc's "no theme style sheet is assigned".
            panel.themeStyleSheet = null;
            var serialized = new SerializedObject(panel);
            var warning = serialized.FindProperty("m_DisableNoThemeWarning");
            if (warning != null) warning.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(panel, PanelPath);
            AssetDatabase.SaveAssets();

            // The auto-copy above (before this method ever ran) is what put this folder in the project;
            // clear it so Ensure() never leaves a stray, unreferenced "Assets/UI Toolkit" behind.
            if (AssetDatabase.IsValidFolder("Assets/UI Toolkit"))
                AssetDatabase.DeleteAsset("Assets/UI Toolkit");

            return panel;
        }
    }
}
