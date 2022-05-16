using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using OlympUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Olympus.Finders {
    public class LutrisYamlFinder : Finder {

        public LutrisYamlFinder(FinderManager manager)
            : base(manager) {
        }

        public string? FindRoot() {
            if (PlatformHelper.Is(Platform.Linux)) {
                return IsFile(Combine(IsDir(GetEnv("XDG_CONFIG_HOME")) ?? Combine(GetEnv("HOME"), ".config"), "lutris"));
            }

            return null;
        }

        public override async IAsyncEnumerable<Installation> FindCandidates() {
            string? root = FindRoot();
            if (string.IsNullOrEmpty(root))
                yield break;

            root = IsDir(Combine(root, "games"));
            if (string.IsNullOrEmpty(root))
                yield break;

            foreach (string dataPath in Directory.GetFiles(root, "celeste-*.yml")) {
                Dictionary<string, object>? data = await Task.Run(() => {
                    using StreamReader sr = new(dataPath);
                    return YamlHelper.Deserializer.Deserialize<Dictionary<string, object>>(sr);
                });

                if (data is not null &&
                    data.TryGetValue("game", out object? gameRaw) && gameRaw is Dictionary<string, object> game &&
                    game.TryGetValue("exe", out object? exeRaw) && exeRaw is string exe &&
                    IsDir(Path.GetDirectoryName(exe)) is string path) {
                    yield return new(FinderTypeDefault, "Lutris (YML)", path);
                    yield break;
                }
            }
        }

    }
}
