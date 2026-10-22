using System;
using System.IO;
using UnityEngine;

namespace Unity.QuickDuel.Tests
{
    // Renders the main camera (which carries the Screen Space - Camera canvas) to a PNG in the folder
    // named by QUICKDUEL_SHOTS. Does nothing when the variable is unset or there's no graphics device.
    public static class Screenshots
    {
        public static string Capture(string name)
        {
            var dir = Environment.GetEnvironmentVariable("QUICKDUEL_SHOTS");
            var camera = Camera.main;
            if (string.IsNullOrEmpty(dir) || camera == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return null;

            Canvas.ForceUpdateCanvases();
            // The canvas lays itself out for the screen, so render at the screen's size.
            var rt = new RenderTexture(Mathf.Max(Screen.width, 64), Mathf.Max(Screen.height, 64), 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            camera.targetTexture = previous;
            RenderTexture.active = null;

            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            Debug.Log($"QuickDuel screenshot: {path}");
            return path;
        }
    }
}
