// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class PlayerDeadBodyHooks
    {
        private static IDetour hook_PlayerDeadBody_DeathRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(PlayerDeadBody)} hooks...");

            On.Celeste.PlayerDeadBody.ctor += PlayerDeadBody_ctor;
            IL.Celeste.PlayerDeadBody.Render += PlayerDeadBody_Render;

            hook_PlayerDeadBody_DeathRoutine = new ILHook(ReflectionCache.PlayerDeadBody_DeathRoutine.GetStateMachineTarget(), PlayerDeadBody_DeathRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(PlayerDeadBody)} hooks...");

            On.Celeste.PlayerDeadBody.ctor -= PlayerDeadBody_ctor;
            IL.Celeste.PlayerDeadBody.Render -= PlayerDeadBody_Render;

            hook_PlayerDeadBody_DeathRoutine?.Dispose();
            hook_PlayerDeadBody_DeathRoutine = null;
        }

        private static void PlayerDeadBody_ctor(On.Celeste.PlayerDeadBody.orig_ctor orig, PlayerDeadBody self, Player player, Vector2 direction)
        {
            orig(self, player, direction);
            self.SetShouldInvert(player.ShouldInvert());
        }

        private static void PlayerDeadBody_DeathRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // playerDeadBody1.deathEffect = new DeathEffect(playerDeadBody1.initialHairColor, new Vector2?(playerDeadBody1.Center - playerDeadBody1.Position));
            cursor.GotoNext(instr => instr.MatchLdfld<PlayerDeadBody>("initialHairColor"));
            cursor.GotoNextSubtraction(MoveType.After);
            cursor.EmitInvertVectorDelegate();

            // playerDeadBody1.Position = playerDeadBody1.Position + Vector2.UnitY * -5f;
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-5));
            cursor.EmitInvertFloatDelegate();
        });

        private static void PlayerDeadBody_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.sprite.Scale.Y = this.scale;
            cursor.GotoNext(instr => instr.MatchStfld<Vector2>(nameof(Vector2.Y)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, PlayerDeadBody, float>>((f, self) => self.ShouldInvert() ? -f : f);
        });
    }
}
