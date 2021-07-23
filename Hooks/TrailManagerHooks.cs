using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class TrailManagerHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(TrailManager)} hooks...");
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool += TrailManager_Add;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(TrailManager)} hooks...");
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool -= TrailManager_Add;
        }

        private static TrailManager.Snapshot TrailManager_Add(
            On.Celeste.TrailManager.orig_Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool orig,
            Vector2 position,
            Image sprite,
            PlayerHair hair,
            Vector2 scale,
            Color color,
            int depth,
            float duration,
            bool frozenUpdate,
            bool useRawDeltaTime)
        {
            if (GravityHelperModule.ShouldInvert)
                scale = new Vector2(scale.X, -scale.Y);

            return orig(position, sprite, hair, scale, color, depth, duration, frozenUpdate, useRawDeltaTime);
        }
    }
}
