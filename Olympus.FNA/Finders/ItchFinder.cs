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
    public class ItchFinder : Finder {

        public ItchFinder(FinderManager manager)
            : base(manager) {
        }

        public string? FindDatabase() {
            if (PlatformHelper.Is(Platform.Windows)) {
                return IsFile(Combine(GetEnv("APPDATA"), "itch", "db", "butler.db"));
            }

            if (PlatformHelper.Is(Platform.MacOS)) {
                return IsFile(Combine(GetEnv("HOME"), "Library", "Application Support", "itch", "db", "butler.db"));
            }

            if (PlatformHelper.Is(Platform.Linux)) {
                return IsFile(Combine(IsDir(GetEnv("XDG_CONFIG_HOME")) ?? Combine(GetEnv("HOME"), ".config"), "itch", "db", "butler.db"));
            }

            return null;
        }

        public override async IAsyncEnumerable<Installation> FindCandidates() {
            string? dbPath = FindDatabase();
            if (string.IsNullOrEmpty(dbPath))
                yield break;

            string? dataRaw;

            using (SqliteConnection con = new(new SqliteConnectionStringBuilder() {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString())) {
                await con.OpenAsync();

                using SqliteCommand cmd = con.CreateCommand();
                cmd.CommandText = @"
                    SELECT verdict FROM caves
                    WHERE game_id == (
                        SELECT ID FROM games
                        WHERE title == ""Celeste""
                    )
                ";

                dataRaw = await cmd.ExecuteScalarAsync() as string;
            }

            if (string.IsNullOrEmpty(dataRaw))
                yield break;

            Dictionary<string, object>? data = await Task.Run(() => {
                using JsonTextReader jtr = new(new StringReader(dataRaw));
                return JsonHelper.Serializer.Deserialize<Dictionary<string, object>>(jtr);
            });

            if (data is not null &&
                data.TryGetValue("basePath", out object? pathRaw) && pathRaw is string path) {
                yield return new(FinderTypeDefault, "itch.io", path);
                yield break;
            }
        }

    }
}
