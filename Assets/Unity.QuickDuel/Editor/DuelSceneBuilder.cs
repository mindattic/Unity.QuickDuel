using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.QuickDuel.EditorTools
{
    // Generates Assets/Scenes/Duel.unity so the scene is never hand-edited YAML. Run from the menu or
    // in batch mode: Unity -batchmode -quit -executeMethod Unity.QuickDuel.EditorTools.DuelSceneBuilder.Build
    public static class DuelSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Duel.unity";

        [MenuItem("QuickDuel/Rebuild Duel Scene")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            cameraObject.AddComponent<AudioListener>();

            var duel = new GameObject("Duel");
            duel.AddComponent<DuelController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"QuickDuel: wrote {ScenePath}");
        }

        private static void AddToBuildSettings(string path)
        {
            var others = EditorBuildSettings.scenes.Where(s => s.path != path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) }.Concat(others).ToArray();
        }
    }
}
