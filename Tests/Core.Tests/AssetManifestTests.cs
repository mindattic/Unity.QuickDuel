using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.QuickDuel.Core;

namespace Unity.QuickDuel.Core.Tests
{
    // The original asset pipeline (UQD-T32): the generated manifest lists every original sprite, sound and font
    // the parity tickets use, every file it names is committed, each image carries the generated pixel-art import
    // settings, and a bad manifest reports every problem by key and line.
    public class AssetManifestTests
    {
        private static readonly string Repo = FindRepo();
        private static readonly string GameFolder = Path.Combine(Repo, "Assets", "Unity.QuickDuel");
        private static readonly Lazy<AssetManifest> Shipped = new Lazy<AssetManifest>(() =>
            AssetManifest.Load(new DirectorySettingsSource(Path.Combine(GameFolder, "Data"))));

        private static string FindRepo()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "Core.slnx")))
                    return dir.FullName;
            throw new DirectoryNotFoundException("Core.slnx not found above the test output.");
        }

        [Test]
        public void The_shipped_manifest_lists_every_used_fighter_strip()
        {
            var m = Shipped.Value;
            foreach (var fighter in new[] { "exile", "gypsy", "myrmidon", "ronin", "vagabond" })
            {
                Assert.That(m.Sprite($"spr_character_{fighter}_idle").Frames, Has.Count.EqualTo(1));
                Assert.That(m.Sprite($"spr_character_{fighter}_run").Frames, Has.Count.EqualTo(1));
                Assert.That(m.Sprite($"spr_character_{fighter}_attack").Frames, Has.Count.EqualTo(4));
                Assert.That(m.Sprite($"spr_character_{fighter}_defeat").Frames, Has.Count.EqualTo(4));
                Assert.That(m.HasSprite($"spr_weapon_{fighter}"), Is.True);
                Assert.That(m.HasSprite($"spr_weapon_{fighter}_ground"), Is.True);
            }
            Assert.That(m.InGroup("fighter"), Has.Count.EqualTo(20));
            Assert.That(m.InGroup("weapon"), Has.Count.EqualTo(12));
            Assert.That(m.Sprite("spr_character_ronin_attack").OriginX, Is.EqualTo(64));
        }

        [Test]
        public void The_shipped_manifest_lists_the_arenas_transitions_frame_and_aftermath_art()
        {
            var m = Shipped.Value;
            Assert.That(m.Arenas, Has.Count.EqualTo(11));
            Assert.That(m.InGroup("arena"), Has.Count.EqualTo(11));
            Assert.That(m.Arenas[0].Name, Is.EqualTo("Wastes of Nod"));
            Assert.That(m.Arenas.All(a => m.HasSprite(a.Sprite)), Is.True);
            Assert.That(m.InGroup("loading"), Has.Count.EqualTo(14));
            Assert.That(m.InGroup("title"), Has.Count.EqualTo(1));
            Assert.That(m.InGroup("cornerart"), Has.Count.EqualTo(4));
            Assert.That(m.InGroup("cursor").Single().Id, Is.EqualTo("spr_katana_cursor"));
            Assert.That(m.InGroup("blood"), Has.Count.EqualTo(2));
            Assert.That(m.InGroup("splatter"), Has.Count.EqualTo(10));
            Assert.That(m.InGroup("flash").Single().Id, Is.EqualTo("spr_flash"));
        }

        [Test]
        public void The_shipped_manifest_lists_the_sounds_fonts_and_credits()
        {
            var m = Shipped.Value;
            foreach (var id in new[] { "sfx_hitground1", "sfx_hitground2", "sfx_hitground3", "sfx_hitground4", "sfx_flick", "sfx_click", "sfx_beep" })
                Assert.That(m.Sound(id).Path, Is.EqualTo($"Audio/{id}.ogg"));
            Assert.That(m.Fonts.Select(f => f.Id), Is.EqualTo(new[] { "fnt_basic", "fnt_mono", "fnt_oldenglish", "fnt_oldenglish_small", "fnt_tiny" }));
            Assert.That(m.Font("fnt_oldenglish").Size, Is.EqualTo(32));
            Assert.That(m.Typefaces.Single().Path, Is.EqualTo("Fonts/oldenglish.ttf"));
            Assert.That(m.Credits, Is.EqualTo("Data/credits.txt"));
            Assert.That(File.ReadAllText(Path.Combine(GameFolder, m.Credits)), Does.Contain("Dusty Junction"));
        }

        [Test]
        public void Every_file_the_manifest_names_is_committed()
        {
            var missing = Shipped.Value.Files().Where(f => !File.Exists(Path.Combine(GameFolder, f))).ToList();
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void Every_original_image_has_the_generated_pixel_art_import_settings()
        {
            var m = Shipped.Value;
            var images = m.Sprites.SelectMany(s => s.Frames).Concat(m.Fonts.Select(f => f.Atlas)).ToList();
            foreach (var image in images)
            {
                var meta = Path.Combine(GameFolder, image + ".meta");
                Assert.That(File.Exists(meta), Is.True, $"{image} has no .meta");
                var text = File.ReadAllText(meta);
                Assert.That(text, Does.Contain("textureType: 8"), $"{image} is not a sprite");
                Assert.That(text, Does.Contain("spriteMode: 1"), $"{image} is not a single sprite");
                Assert.That(text, Does.Contain("spritePixelsToUnits: 1\n").Or.Contain("spritePixelsToUnits: 1\r\n"), $"{image} is not 1 pixel per unit");
                Assert.That(text, Does.Contain("filterMode: 0"), $"{image} is not point filtered");
                Assert.That(text, Does.Contain("enableMipMap: 0"), $"{image} has mipmaps");
            }

            var ronin = File.ReadAllText(Path.Combine(GameFolder, m.Sprite("spr_character_ronin_attack").Frames[0] + ".meta"));
            Assert.That(ronin, Does.Contain("spritePivot: {x: 0.5, y: 0.5}"));
            var loading = File.ReadAllText(Path.Combine(GameFolder, m.Sprite("spr_loading1").Frames[0] + ".meta"));
            Assert.That(loading, Does.Contain("spritePivot: {x: 0, y: 1}"), "a top-left GameMaker origin pivots at the top left");
        }

        [Test]
        public void A_GameMaker_origin_becomes_a_bottom_left_pivot()
        {
            var sprite = new OriginalSprite("s", "flash", 90, 60, 30, 15, new[] { "Art/s_0.png" });
            Assert.That(sprite.PivotX, Is.EqualTo(1d / 3d));
            Assert.That(sprite.PivotY, Is.EqualTo(0.75));
        }

        private static readonly string Good = @"{
  ""format"": ""quickduel.assets"",
  ""version"": 1,
  ""sprites"": {
    ""spr_a"": { ""group"": ""arena"", ""width"": 10, ""height"": 10, ""originX"": 0, ""originY"": 0, ""frames"": [ ""Art/a_0.png"" ] }
  },
  ""sounds"": { ""sfx_a"": { ""path"": ""Audio/sfx_a.ogg"" } },
  ""fonts"": { ""fnt_a"": { ""face"": ""A"", ""size"": 10, ""atlas"": ""Fonts/fnt_a.png"", ""glyphs"": ""Fonts/fnt_a.glyphs.xml"" } },
  ""typefaces"": { ""a"": { ""path"": ""Fonts/a.ttf"" } },
  ""arenas"": { ""a"": { ""name"": ""A"", ""description"": ""An arena."", ""sprite"": ""spr_a"" } },
  ""credits"": ""Data/credits.txt""
}".Replace("\r\n", "\n");

        private static AssetManifest Load(string text) =>
            AssetManifest.Load(new MemorySettingsSource().Set(AssetManifest.File, text));

        private static string[] Errors(string text) =>
            Assert.Throws<SettingsException>(() => Load(text)).Errors.ToArray();

        [Test]
        public void A_minimal_manifest_loads()
        {
            var m = Load(Good);
            Assert.That(m.Sprite("spr_a").Frames, Is.EqualTo(new[] { "Art/a_0.png" }));
            Assert.That(m.Files(), Is.EqualTo(new[] { "Art/a_0.png", "Audio/sfx_a.ogg", "Fonts/fnt_a.png", "Fonts/fnt_a.glyphs.xml", "Fonts/a.ttf", "Data/credits.txt" }));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => m.Sprite("spr_missing"));
        }

        [Test]
        public void A_bad_manifest_reports_every_problem()
        {
            var bad = Good
                .Replace("\"group\": \"arena\"", "\"group\": \"portrait\"")
                .Replace("Art/a_0.png", "../a_0.png")
                .Replace("Audio/sfx_a.ogg", "C:/sfx_a.wav")
                .Replace("\"sprite\": \"spr_a\"", "\"sprite\": \"spr_b\"")
                .Replace("\"version\": 1", "\"version\": 2, \"extra\": true");
            var errors = Errors(bad);
            Assert.That(errors, Has.Some.Contains("'group' must be one of"));
            Assert.That(errors, Has.Some.Contains("'frames' must be a relative path").And.Contains("../a_0.png"));
            Assert.That(errors, Has.Some.Contains("'path' must be a relative path"));
            Assert.That(errors, Has.Some.Contains("'path' must name a .ogg file"));
            Assert.That(errors, Has.Some.Contains("'sprite' must name a sprite"));
            Assert.That(errors, Has.Some.Contains("'version' must be 1"));
            Assert.That(errors, Has.Some.Contains("unknown key 'extra'"));
        }

        [Test]
        public void A_missing_manifest_is_reported()
        {
            var errors = Assert.Throws<SettingsException>(() => AssetManifest.Load(new MemorySettingsSource())).Errors;
            Assert.That(errors, Is.EqualTo(new[] { "assets.json: not found" }));
        }
    }
}
