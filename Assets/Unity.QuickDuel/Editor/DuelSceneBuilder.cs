using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.QuickDuel.Core;
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
            RenderPipelineBuilder.Ensure();
            DuelPanelBuilder.Ensure();
            OriginalFontBuilder.Ensure();
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
            duel.AddComponent<DuelInput>();
            duel.AddComponent<DuelQuit>();
            var view = duel.AddComponent<DuelView>();
            view.SetArenas(Arenas());
            view.SetFighters(Fighters());
            duel.AddComponent<DuelTaunt>();
            duel.AddComponent<DuelSession>();
            duel.AddComponent<DuelAudio>().SetClips(Clip("sfx_beep"), Clip("sfx_drumroll"), Clip("sfx_unsheath"), Clip("sfx_slash"),
                Clip("sfx_victory"), Clip("sfx_defeat"));
#if QUICKDUEL_MULTIPLAYER
            OnlineSceneBuilder.Build(scene, duel);
#endif

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"QuickDuel: wrote {ScenePath}");
        }

        // The 11 original arenas (UQD-T33) as the asset manifest lists them, each with its imported backdrop sprite.
        // A manifest that fails validation stops the build rather than shipping a scene with a guess.
        public static List<ArenaBackdrop> Arenas()
        {
            var manifest = Manifest();
            var arenas = new List<ArenaBackdrop>();
            foreach (var arena in manifest.Arenas)
                arenas.Add(new ArenaBackdrop(arena.Id, arena.Name, arena.Description, Frames(manifest, arena.Sprite, $"arena '{arena.Id}'")[0]));
            return arenas;
        }

        // The five original fighters (UQD-T34) as the asset manifest lists them (load_characters.gml's id order), each
        // with its imported idle, run, attack, defeat and weapon sprites. Cosmetic only: no stats travel.
        public static List<FighterLook> Fighters()
        {
            var manifest = Manifest();
            var fighters = new List<FighterLook>();
            foreach (var fighter in manifest.Fighters)
            {
                var owner = $"fighter '{fighter.Id}'";
                fighters.Add(new FighterLook(fighter.Id, fighter.Name,
                    Frames(manifest, fighter.Idle, owner)[0],
                    Frames(manifest, fighter.Run, owner)[0],
                    Frames(manifest, fighter.Attack, owner),
                    Frames(manifest, fighter.Defeat, owner),
                    Frames(manifest, fighter.Weapon, owner)[0],
                    Frames(manifest, fighter.GroundWeapon, owner)[0]));
            }
            return fighters;
        }

        // A manifest that fails validation stops the build rather than shipping a scene with a guess.
        private static AssetManifest Manifest() =>
            AssetManifest.Load(new DirectorySettingsSource(Path.GetFullPath(OriginalAssetGenerator.DataFolder)));

        // Every imported frame of a manifest sprite, in order; a missing one stops the build.
        private static List<Sprite> Frames(AssetManifest manifest, string spriteId, string owner)
        {
            var frames = new List<Sprite>();
            foreach (var frame in manifest.Sprite(spriteId).Frames)
            {
                var path = $"{OriginalAssetGenerator.GameFolder}/{frame}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    throw new InvalidOperationException($"QuickDuel: {owner} has no sprite at {path}.");
                frames.Add(sprite);
            }
            return frames;
        }

        private static AudioClip Clip(string name)
        {
            var path = $"Assets/Unity.QuickDuel/Audio/{name}.ogg";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"QuickDuel: missing {path}");
            return clip;
        }

        private static void AddToBuildSettings(string path)
        {
            var others = EditorBuildSettings.scenes.Where(s => s.path != path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) }.Concat(others).ToArray();
        }
    }
}
