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

namespace Olympus.Finders {
    public class LegendaryFinder : Finder {

        public LegendaryFinder(FinderManager manager)
            : base(manager) {
        }

        public string? FindDatabase() {
            // The Legendary finder in old Olympus was implemented before Legendary supported macOS.
            // At the time of writing this, it follows XDG_CONFIG_HOME and ~/.config/legendary on all platforms.
            return IsFile(Combine(IsDir(GetEnv("XDG_CONFIG_HOME")) ?? Combine(IsDir(GetEnv("USERPROFILE")) ?? GetEnv("HOME"), ".config"), "legendary", "installed.json"));
        }

        public override async IAsyncEnumerable<Installation> FindCandidates() {
            string? dbPath = FindDatabase();
            if (string.IsNullOrEmpty(dbPath))
                yield break;

            Dictionary<string, object>? db = await Task.Run(() => {
                using JsonTextReader jtr = new(new StreamReader(dbPath));
                return JsonHelper.Serializer.Deserialize<Dictionary<string, object>>(jtr);
            });

            if (db is not null &&
                db.TryGetValue("Salt", out object? dataRaw) && dataRaw is Dictionary<string, object> data &&
                data.TryGetValue("install_path", out object? pathRaw) && pathRaw is string path) {
                yield return new(FinderTypeDefault, "Legendary", path);
                yield break;
            }
        }

    }
}
