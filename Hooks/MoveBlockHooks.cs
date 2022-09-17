// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class MoveBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(MoveBlock)} hooks...");

            On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool += MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
            On.Celeste.MoveBlock.Render += MoveBlock_Render;
            On.Celeste.MoveBlock.MoveVExact += MoveBlock_MoveVExact;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(MoveBlock)} hooks...");

            On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool -= MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
            On.Celeste.MoveBlock.Render -= MoveBlock_Render;
            On.Celeste.MoveBlock.MoveVExact -= MoveBlock_MoveVExact;
        }

        private static void MoveBlock_ctor_Vector2_int_int_Directions_bool_bool(On.Celeste.MoveBlock.orig_ctor_Vector2_int_int_Directions_bool_bool orig, MoveBlock self, Vector2 position, int width, int height, MoveBlock.Directions direction, bool canSteer, bool fast)
        {
            orig(self, position, width, height, direction, canSteer, fast);

            if (canSteer && (direction == MoveBlock.Directions.Left || direction == MoveBlock.Directions.Right))
                self.Add(new MoveBlockBottomComponent());
        }

        private static void MoveBlock_Render(On.Celeste.MoveBlock.orig_Render orig, MoveBlock self)
        {
            if (self.Get<MoveBlockBottomComponent>() is { } component)
            {
                var oldPosition = self.Position;
                self.Position += self.Shake;
                component.Render();
                self.Position = oldPosition;
            }

            orig(self);
        }

        private static void MoveBlock_MoveVExact(On.Celeste.MoveBlock.orig_MoveVExact orig, MoveBlock self, int move)
        {
            if (!GravityHelperModule.ShouldInvertPlayer)
            {
                orig(self, move);
                return;
            }

            var data = DynamicData.For(self);
            var noSquish = data.Get<Player>("noSquish");

            if (noSquish != null && move > 0 && noSquish.Top >= self.Bottom)
            {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitY * move))
                    move -= Math.Sign(move);
            }

            orig(self, move);
        }
    }
}
