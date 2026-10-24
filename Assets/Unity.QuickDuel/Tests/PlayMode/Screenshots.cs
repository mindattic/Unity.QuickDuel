using System;
using System.IO;
using UnityEngine;

namespace Unity.QuickDuel.Tests
{
    // Captures the whole screen -- the UI Toolkit panel DuelView draws (UQD-T31: it's a screen-space
    // overlay, not tied to any one camera the way the old uGUI Screen Space - Camera canvas was) plus the
    // main camera's backdrop behind it -- to a PNG in the folder named by QUICKDUEL_SHOTS. Does nothing
    // when the variable is unset or there's no graphics device.
    public static class Screenshots
    {
        public static string Capture(string name)
        {
            var dir = Environment.GetEnvironmentVariable("QUICKDUEL_SHOTS");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return null;

            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"QuickDuel screenshot: {path}");
            return path;
        }
    }
}
