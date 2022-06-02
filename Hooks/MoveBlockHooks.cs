// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class MoveBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(MoveBlock)} hooks...");

            On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool += MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
            On.Celeste.MoveBlock.Render += MoveBlock_Render;
            IL.Celeste.MoveBlock.MoveVExact += MoveBlock_MoveVExact;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(MoveBlock)} hooks...");

            On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool -= MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
            On.Celeste.MoveBlock.Render -= MoveBlock_Render;
            IL.Celeste.MoveBlock.MoveVExact -= MoveBlock_MoveVExact;
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

        private static void MoveBlock_MoveVExact(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert all the checks that ensure we don't squish ourselves

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(1)))
                throw new HookException("Couldn't find move");

            cursor.EmitInvertIntDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<MoveBlock>("noSquish"),
                instr => instr.MatchCallOrCallvirt<Entity>("get_Y")))
                throw new HookException("Couldn't find noSquish.Y");

            cursor.EmitInvertFloatDelegate();

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(0),
                instr => instr.MatchCallOrCallvirt<Entity>("get_Y")))
                throw new HookException("Couldn't find this.Y");

            cursor.EmitInvertFloatDelegate();
        });
    }
}
