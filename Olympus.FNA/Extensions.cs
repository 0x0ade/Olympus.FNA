using Microsoft.Xna.Framework;
using System;

namespace Olympus {
    public static class Extensions {

        public static float GetDeltaTime(this GameTime gameTime, float max = 1f / 20f)
            => Math.Min((float) gameTime.ElapsedGameTime.TotalSeconds, max);

    }
}
