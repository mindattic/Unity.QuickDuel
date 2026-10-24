using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Unity.QuickDuel.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.QuickDuel.EditorTools
{
    /// <summary>
    /// Copies the original Bushido Coda art, sound and fonts the rebuild uses (UQD-T32) out of the GameMaker
    /// project into <see cref="GameFolder"/>, and writes the asset manifest (<see cref="ManifestPath"/>) and the
    /// credits text next to it. Each sprite's size, origin and frame list are read from its <c>.sprite.gmx</c>,
    /// the arenas from <c>load_arenas.gml</c>, so nothing is typed in by hand. Import settings are not clicked
    /// either: <see cref="OriginalAssetImportRules"/> applies them from the manifest on every import.
    /// The copied files are committed, so the game and CI never need the original; rerun this only to refresh
    /// them. Run from the menu or in batch mode:
    /// <c>-executeMethod Unity.QuickDuel.EditorTools.OriginalAssetGenerator.Generate [-originalRoot &lt;BushidoCoda.gmx&gt;]</c>
    /// (or set QUICKDUEL_ORIGINAL_ROOT). The default is the owner's archive path.
    /// </summary>
    public static class OriginalAssetGenerator
    {
        public const string GameFolder = "Assets/Unity.QuickDuel";
        public const string DataFolder = GameFolder + "/Data";
        public const string ManifestPath = DataFolder + "/" + AssetManifest.File;
        public const string CreditsFile = "Data/credits.txt";
        public const string DefaultRoot = @"R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\GameMaker\gmlBushidoCoda\BushidoCoda.gmx";
        public const string RootArgument = "-originalRoot";
        public const string RootVariable = "QUICKDUEL_ORIGINAL_ROOT";

        [MenuItem("QuickDuel/Import Original Assets")]
        public static void Generate()
        {
            var ok = false;
            try
            {
                var manifest = Run(ResolveRoot());
                Debug.Log($"QuickDuel original assets: {manifest.Sprites.Count} sprites, {manifest.Sounds.Count} sounds, " +
                          $"{manifest.Fonts.Count} fonts, {manifest.Typefaces.Count} typefaces, {manifest.Arenas.Count} arenas -> {ManifestPath}");
                ok = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"QuickDuel original assets failed: {e.Message}\n{e.StackTrace}");
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>The GameMaker project folder: <c>-originalRoot</c>, else QUICKDUEL_ORIGINAL_ROOT, else <see cref="DefaultRoot"/>.</summary>
        public static string ResolveRoot()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == RootArgument)
                    return args[i + 1];
            var variable = Environment.GetEnvironmentVariable(RootVariable);
            return string.IsNullOrEmpty(variable) ? DefaultRoot : variable;
        }

        /// <summary>Copies everything, writes the manifest, imports it all and returns the validated manifest.</summary>
        public static AssetManifest Run(string root)
        {
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"The original GameMaker project was not found at {root} (pass {RootArgument} or set {RootVariable}).");

            var json = new ManifestWriter();
            var sprites = new List<SpriteRecord>();
            foreach (var entry in OriginalAssetCatalog.Sprites())
                sprites.Add(CopySprite(root, entry.Id, entry.Group, entry.Folder));

            var arenas = ReadArenas(Path.Combine(root, "scripts", "load_arenas.gml"));
            var arenaSprites = new List<SpriteRecord>();
            foreach (var arena in arenas)
                if (arenaSprites.FindIndex(s => s.Id == arena.Sprite) < 0)
                    arenaSprites.Add(CopySprite(root, arena.Sprite, "arena", OriginalAssetCatalog.ArenaFolder));
            // Keep the manifest in the group order AssetManifest.SpriteGroups lists.
            sprites.InsertRange(sprites.FindIndex(s => s.Group == "loading"), arenaSprites);

            var sounds = new List<KeyValuePair<string, string>>();
            foreach (var sound in OriginalAssetCatalog.Sounds)
            {
                var path = $"{OriginalAssetCatalog.AudioFolder}/{sound}.ogg";
                if (Array.IndexOf(OriginalAssetCatalog.ReencodedSounds, sound) >= 0)
                {
                    if (!File.Exists(Full($"{GameFolder}/{path}")))
                        throw new FileNotFoundException($"{sound}'s original .ogg does not import in Unity; re-encode its .wav to {path} first.");
                }
                else
                {
                    Copy(Path.Combine(root, "sound", "audio", sound + ".ogg"), path);
                }
                sounds.Add(new KeyValuePair<string, string>(sound, path));
            }

            var fonts = new List<FontRecord>();
            foreach (var font in OriginalAssetCatalog.Fonts)
                fonts.Add(CopyFont(root, font));

            var typefaces = new List<KeyValuePair<string, string>>();
            foreach (var typeface in OriginalAssetCatalog.Typefaces)
            {
                var path = $"{OriginalAssetCatalog.FontFolder}/{typeface}.ttf";
                Copy(Path.Combine(root, "..", "fonts", typeface + ".ttf"), path);
                typefaces.Add(new KeyValuePair<string, string>(typeface, path));
            }

            Copy(Path.Combine(root, "datafiles", "credits.txt"), CreditsFile);

            var text = json.Write(sprites, sounds, fonts, typefaces, arenas, CreditsFile);
            Directory.CreateDirectory(Full(DataFolder));
            File.WriteAllText(Full(ManifestPath), text, new UTF8Encoding(false));

            // The manifest is on disk before anything imports, so the import rules can read it.
            AssetDatabase.Refresh();
            var manifest = AssetManifest.Load(new DirectorySettingsSource(Full(DataFolder)));
            OriginalAssetImportRules.Forget();
            foreach (var sprite in manifest.Sprites)
                foreach (var frame in sprite.Frames)
                    AssetDatabase.ImportAsset($"{GameFolder}/{frame}", ImportAssetOptions.ForceUpdate);
            foreach (var font in manifest.Fonts)
                AssetDatabase.ImportAsset($"{GameFolder}/{font.Atlas}", ImportAssetOptions.ForceUpdate);

            var missing = new List<string>();
            foreach (var file in manifest.Files())
                if (!File.Exists(Full($"{GameFolder}/{file}")))
                    missing.Add(file);
            if (missing.Count > 0)
                throw new InvalidOperationException("The manifest names files that were not copied: " + string.Join(", ", missing));
            return manifest;
        }

        private static SpriteRecord CopySprite(string root, string id, string group, string folder)
        {
            var definition = Path.Combine(root, "sprites", id + ".sprite.gmx");
            if (!File.Exists(definition))
                throw new FileNotFoundException($"The original has no sprite {id}", definition);

            var xml = new XmlDocument();
            xml.Load(definition);
            var sprite = xml.SelectSingleNode("/sprite");
            var record = new SpriteRecord
            {
                Id = id,
                Group = group,
                Width = Int(sprite, "width"),
                Height = Int(sprite, "height"),
                OriginX = Int(sprite, "xorig"),
                OriginY = Int(sprite, "yorigin"),
            };

            var frames = sprite.SelectNodes("frames/frame");
            var ordered = new SortedDictionary<int, string>();
            foreach (XmlNode frame in frames)
                ordered[int.Parse(frame.Attributes["index"].Value, CultureInfo.InvariantCulture)] = frame.InnerText.Trim();
            foreach (var frame in ordered.Values)
            {
                // "images\spr_x_0.png", relative to the sprites folder.
                var name = Path.GetFileName(frame.Replace('\\', '/'));
                var path = $"{folder}/{name}";
                Copy(Path.Combine(root, "sprites", frame.Replace('\\', Path.DirectorySeparatorChar)), path);
                record.Frames.Add(path);
            }
            if (record.Frames.Count == 0)
                throw new InvalidDataException($"The original sprite {id} has no frames.");
            return record;
        }

        private static FontRecord CopyFont(string root, string id)
        {
            var definition = Path.Combine(root, "fonts", id + ".font.gmx");
            var xml = new XmlDocument();
            xml.Load(definition);
            var font = xml.SelectSingleNode("/font");
            var record = new FontRecord
            {
                Id = id,
                Face = font.SelectSingleNode("name").InnerText.Trim(),
                Size = Int(font, "size"),
                Atlas = $"{OriginalAssetCatalog.FontFolder}/{id}.png",
                Glyphs = $"{OriginalAssetCatalog.FontFolder}/{id}.glyphs.xml",
            };
            Copy(Path.Combine(root, "fonts", id + ".png"), record.Atlas);
            Copy(definition, record.Glyphs);
            return record;
        }

        private static readonly Regex ArenaLine = new Regex(
            @"^\s*arena_(name|asset|description)\[id_(\w+)\]\s*(\+?=)\s*(.+?)\s*;\s*$", RegexOptions.Compiled);

        /// <summary>
        /// The arenas load_arenas.gml defines, in its order. Each is keyed by its GML id name, not its number:
        /// the original gives Sozza's Point and South Ridge the same number (5), which is T33's to resolve.
        /// "#" (GameMaker's line break) becomes a newline.
        /// </summary>
        public static List<ArenaRecord> ReadArenas(string script)
        {
            var arenas = new List<ArenaRecord>();
            foreach (var line in File.ReadAllLines(script))
            {
                var match = ArenaLine.Match(line);
                if (!match.Success)
                    continue;
                var id = match.Groups[2].Value;
                var arena = arenas.Find(a => a.Id == id);
                if (arena == null)
                {
                    arena = new ArenaRecord { Id = id };
                    arenas.Add(arena);
                }

                var value = match.Groups[4].Value;
                var append = match.Groups[3].Value == "+=";
                switch (match.Groups[1].Value)
                {
                    case "name":
                        arena.Name = Literal(value);
                        break;
                    case "asset":
                        arena.Sprite = value;
                        break;
                    default:
                        arena.Description = (append ? arena.Description : string.Empty) + Literal(value);
                        break;
                }
            }

            foreach (var arena in arenas)
                arena.Description = arena.Description?.Replace('#', '\n').TrimEnd('\n');
            return arenas;
        }

        private static string Literal(string value)
        {
            if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
                throw new InvalidDataException($"Expected a quoted string in load_arenas.gml, found {value}");
            return value.Substring(1, value.Length - 2);
        }

        private static int Int(XmlNode node, string child) =>
            int.Parse(node.SelectSingleNode(child).InnerText.Trim(), CultureInfo.InvariantCulture);

        private static void Copy(string source, string relative)
        {
            if (!File.Exists(source))
                throw new FileNotFoundException($"The original has no file {source}", source);
            var target = Full($"{GameFolder}/{relative}");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            // Skip identical files so a rerun leaves the working tree untouched.
            if (File.Exists(target) && SameBytes(source, target))
                return;
            File.Copy(source, target, true);
        }

        private static bool SameBytes(string a, string b)
        {
            var left = File.ReadAllBytes(a);
            var right = File.ReadAllBytes(b);
            if (left.Length != right.Length)
                return false;
            for (var i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;
            return true;
        }

        private static string Full(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        public sealed class SpriteRecord
        {
            public string Id;
            public string Group;
            public int Width;
            public int Height;
            public int OriginX;
            public int OriginY;
            public readonly List<string> Frames = new List<string>();
        }

        public sealed class FontRecord
        {
            public string Id;
            public string Face;
            public int Size;
            public string Atlas;
            public string Glyphs;
        }

        public sealed class ArenaRecord
        {
            public string Id;
            public string Name;
            public string Description;
            public string Sprite;
        }

        // Writes assets.json by hand in a fixed layout, so a rerun on the same original is byte-identical.
        private sealed class ManifestWriter
        {
            private readonly StringBuilder sb = new StringBuilder();

            public string Write(List<SpriteRecord> sprites, List<KeyValuePair<string, string>> sounds,
                List<FontRecord> fonts, List<KeyValuePair<string, string>> typefaces, List<ArenaRecord> arenas, string credits)
            {
                sb.Append("{\n");
                Line(1, $"\"format\": {Str(AssetManifest.Format)},");
                Line(1, $"\"version\": {AssetManifest.CurrentVersion},");
                Line(1, $"\"_source\": {Str("BushidoCoda.gmx (GameMaker Studio, 2013); paths are relative to " + GameFolder)},");
                Line(1, $"\"_generated\": {Str("by Unity.QuickDuel.EditorTools.OriginalAssetGenerator (QuickDuel > Import Original Assets); do not edit by hand")},");

                Line(1, "\"sprites\": {");
                for (var i = 0; i < sprites.Count; i++)
                {
                    var s = sprites[i];
                    var frames = new List<string>();
                    foreach (var frame in s.Frames)
                        frames.Add(Str(frame));
                    Line(2, $"{Str(s.Id)}: {{ \"group\": {Str(s.Group)}, \"width\": {s.Width}, \"height\": {s.Height}, " +
                            $"\"originX\": {s.OriginX}, \"originY\": {s.OriginY}, \"frames\": [ {string.Join(", ", frames)} ] }}{Comma(i, sprites.Count)}");
                }
                Line(1, "},");

                Line(1, "\"sounds\": {");
                for (var i = 0; i < sounds.Count; i++)
                    Line(2, $"{Str(sounds[i].Key)}: {{ \"path\": {Str(sounds[i].Value)} }}{Comma(i, sounds.Count)}");
                Line(1, "},");

                Line(1, "\"fonts\": {");
                for (var i = 0; i < fonts.Count; i++)
                {
                    var f = fonts[i];
                    Line(2, $"{Str(f.Id)}: {{ \"face\": {Str(f.Face)}, \"size\": {f.Size}, \"atlas\": {Str(f.Atlas)}, \"glyphs\": {Str(f.Glyphs)} }}{Comma(i, fonts.Count)}");
                }
                Line(1, "},");

                Line(1, "\"typefaces\": {");
                for (var i = 0; i < typefaces.Count; i++)
                    Line(2, $"{Str(typefaces[i].Key)}: {{ \"path\": {Str(typefaces[i].Value)} }}{Comma(i, typefaces.Count)}");
                Line(1, "},");

                Line(1, "\"arenas\": {");
                for (var i = 0; i < arenas.Count; i++)
                {
                    var a = arenas[i];
                    Line(2, $"{Str(a.Id)}: {{ \"name\": {Str(a.Name)}, \"description\": {Str(a.Description)}, \"sprite\": {Str(a.Sprite)} }}{Comma(i, arenas.Count)}");
                }
                Line(1, "},");

                Line(1, $"\"credits\": {Str(credits)}");
                sb.Append("}\n");
                return sb.ToString();
            }

            private void Line(int depth, string text) => sb.Append(' ', depth * 2).Append(text).Append('\n');

            private static string Comma(int index, int count) => index < count - 1 ? "," : string.Empty;

            private static string Str(string value)
            {
                var b = new StringBuilder("\"");
                foreach (var c in value ?? string.Empty)
                {
                    switch (c)
                    {
                        case '"': b.Append("\\\""); break;
                        case '\\': b.Append("\\\\"); break;
                        case '\n': b.Append("\\n"); break;
                        case '\r': break;
                        case '\t': b.Append("\\t"); break;
                        default:
                            if (c < ' ')
                                b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                b.Append(c);
                            break;
                    }
                }
                return b.Append('"').ToString();
            }
        }
    }
}
