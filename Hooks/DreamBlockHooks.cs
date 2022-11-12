// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class DreamBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(DreamBlock)} hooks...");
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
            IL.Celeste.DreamBlock.WobbleLine += DreamBlock_WobbleLine;
            IL.Celeste.DreamBlock.Render += DreamBlock_Render;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(DreamBlock)} hooks...");
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
            IL.Celeste.DreamBlock.WobbleLine -= DreamBlock_WobbleLine;
            IL.Celeste.DreamBlock.Render -= DreamBlock_Render;
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
        {
            orig(self);
            if (self is GravityDreamBlock gravityDreamBlock)
                gravityDreamBlock.InitialiseParticleColors();
        }

        private static void DreamBlock_WobbleLine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            emitReplaceActiveLineColor(cursor);
            emitReplaceActiveBackColor(cursor);
        });

        private static void DreamBlock_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // change background colour
            emitReplaceActiveBackColor(cursor);

            // change colour of corner rectangles
            emitReplaceActiveLineColor(cursor);
            emitReplaceActiveLineColor(cursor);
            emitReplaceActiveLineColor(cursor);
            emitReplaceActiveLineColor(cursor);
        });

        private static void emitReplaceActiveLineColor(ILCursor cursor)
        {
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<DreamBlock>("activeLineColor")))
                throw new HookException("Couldn't find activeLineColor");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Color, DreamBlock, Color>>((color, self) =>
            {
                if (self is not GravityDreamBlock gravityDreamBlock) return color;
                return
                    gravityDreamBlock.LineColor?.Lighter(gravityDreamBlock.WasEntered ? 0.2f : 0f) ??
                    gravityDreamBlock.GravityType.Color().Lighter(gravityDreamBlock.WasEntered ? 0.6f : 0.4f);
            });
        }

        private static void emitReplaceActiveBackColor(ILCursor cursor)
        {
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<DreamBlock>("activeBackColor")))
                throw new HookException("Couldn't find activeBackColor");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Color, DreamBlock, Color>>((oldColor, self) =>
            {
                if (self is not GravityDreamBlock gravityDreamBlock) return oldColor;
                var baseColor = gravityDreamBlock.BackColor ?? gravityDreamBlock.GravityType.Color();
                var color = baseColor * (gravityDreamBlock.WasEntered ? 0.6f : 0.15f);
                return new Color(color.R, color.G, color.B, 255);
            });
        }
    }
}
