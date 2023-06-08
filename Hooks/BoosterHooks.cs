// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BoosterHooks
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_Booster_BoostRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Booster)} hooks...");

            On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
            IL.Celeste.Booster.Update += Booster_Update;
            IL.Celeste.Booster.Render += Booster_Render;

            hook_Booster_BoostRoutine = new ILHook(ReflectionCache.Booster_BoostRoutine.GetStateMachineTarget(), Booster_BoostRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Booster)} hooks...");

            On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
            IL.Celeste.Booster.Update -= Booster_Update;
            IL.Celeste.Booster.Render -= Booster_Render;

            hook_Booster_BoostRoutine?.Dispose();
            hook_Booster_BoostRoutine = null;
        }

        private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction)
        {
            orig(self, player, direction);
            if (self is GravityBooster gravityBooster)
                GravityHelperModule.PlayerComponent?.SetGravity(gravityBooster.GravityType);
        }

        private static void Booster_BoostRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Booster>("playerOffset")))
                throw new HookException("Couldn't find playerOffset");

            cursor.EmitInvertVectorDelegate();
        });

        private static void Booster_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Booster>("playerOffset")))
                throw new HookException("Couldn't find playerOffset");

            cursor.EmitInvertVectorDelegate();
        });

        private static void Booster_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Booster>("sprite"),
                instr => instr.MatchLdcI4(1),
                instr => instr.MatchCallvirt<GraphicsComponent>(nameof(GraphicsComponent.DrawOutline))))
                throw new HookException("Couldn't find sprite.DrawOutline");

            var cursor2 = cursor.Clone();
            if (!cursor2.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchCall<Entity>(nameof(Entity.Render))))
                throw new HookException("Couldn't find base.Render");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Booster, bool>>(self =>
            {
                if (self is not GravityBooster gravityBooster) return false;
                var data = DynamicData.For(gravityBooster);
                var sprite = data.Get<Sprite>("sprite");
                sprite.DrawOutline(gravityBooster.GravityType.Color());
                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Br_S, cursor2.Next);
        });
    }
}
