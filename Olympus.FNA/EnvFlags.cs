using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Olympus.NativeImpls;
using System;

namespace Olympus {
    public static class EnvFlags {

        public static bool IsTenfoot { get; private set; }
        public static bool IsDeck { get; private set; }
        public static bool IsTenfootOrDeck { get; private set; }

        public static bool IsFullscreen { get; private set; }
        public static bool HasKeyboard { get; private set; }
        public static bool PreferController { get; private set; }
        public static bool HasTouchscreen { get; private set; }
        public static ClientSideDecorationMode? UserCSD { get; private set; }

        static EnvFlags() {
            Update();
        }

        public static void Update() {
            IsTenfoot = Environment.GetEnvironmentVariable("SteamTenfoot") == "1";
            IsDeck = Environment.GetEnvironmentVariable("SteamDeck") == "1";
            IsTenfootOrDeck = IsTenfoot || IsDeck;

            UpdateFlag(nameof(IsFullscreen), v => v == "1", IsTenfootOrDeck);
            UpdateFlag(nameof(HasKeyboard), v => v == "1", !IsTenfootOrDeck);
            UpdateFlag(nameof(PreferController), v => v == "1", IsTenfootOrDeck);
            UpdateFlag(nameof(HasTouchscreen), v => v == "1", IsDeck);
            UpdateFlag(nameof(UserCSD), v => Enum.TryParse(v, out ClientSideDecorationMode e) ? e : null, IsDeck ? ClientSideDecorationMode.Title : default(ClientSideDecorationMode?));
        }

        private static void UpdateFlag<T>(string prop, Func<string, T> cb, T fallback) {
            string? varval = Environment.GetEnvironmentVariable($"OLYMPUS_ENVFLAG_{prop.ToUpperInvariant()}");
            object? value;

            if (!string.IsNullOrEmpty(varval)) {
                value = cb(varval);
            } else {
                value = fallback;
            }

            typeof(EnvFlags).GetProperty(prop)!.GetSetMethod(true)!.Invoke(null, new object?[] { value });
        }

    }
}
