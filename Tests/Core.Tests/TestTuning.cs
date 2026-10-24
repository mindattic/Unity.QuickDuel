using System.IO;
using Unity.QuickDuel.Core;

namespace Unity.QuickDuel.Core.Tests
{
    // The tuning the game ships (Assets/StreamingAssets/Settings/tuning.json), loaded once for the tests.
    public static class TestTuning
    {
        public static string Folder => Path.Combine(System.AppContext.BaseDirectory, "Settings");

        public static TuningRegistry Registry { get; } = TuningRegistry.Load(new DirectorySettingsSource(Folder));

        public static TuningProfile Profile => Registry.Default;
    }
}
