// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BumperHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Bumper)} hooks...");
            IL.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Bumper)} hooks...");
            IL.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
        }

        private static void Bumper_OnPlayer(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch))))
                throw new HookException("Couldn't find ExplodeLaunch.");

            cursor.EmitInvertVectorDelegate();

            if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position))))
                throw new HookException("Couldn't find Entity.Position.");

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
                !GravityHelperModule.ShouldInvert ? v : new Vector2(v.X, p.CenterY - (v.Y - p.CenterY)));
        });
    }
}
