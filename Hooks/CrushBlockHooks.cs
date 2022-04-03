// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class CrushBlockHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(CrushBlock)} hooks...");
            IL.Celeste.CrushBlock.Update += CrushBlock_Update;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(CrushBlock)} hooks...");
            IL.Celeste.CrushBlock.Update -= CrushBlock_Update;
        }

        private static void CrushBlock_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert vertical CollideCheck<Player>
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(0),
                instr => instr.MatchLdcR4(-1)))
                throw new HookException("Couldn't find Vector2(0, -1)");
            cursor.EmitInvertFloatDelegate();

            // invert the subtraction if we must
            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdcR4(1)))
                throw new HookException("Couldn't find --this.face.Y");
            cursor.EmitInvertFloatDelegate();
        });
    }
}
