// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BadelineBoostHooks
    {
        // ReSharper disable InconsistentNaming
        private static IDetour hook_BadelineBoost_BoostRoutine;
        // ReSharper restore InconsistentNaming

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(BadelineBoost)} hooks...");

            On.Celeste.BadelineBoost.OnPlayer += BadelineBoost_OnPlayer;

            hook_BadelineBoost_BoostRoutine = new ILHook(ReflectionCache.BadelineBoost_BoostRoutine.GetStateMachineTarget(), BadelineBoost_BoostRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(BadelineBoost)} hooks...");

            On.Celeste.BadelineBoost.OnPlayer -= BadelineBoost_OnPlayer;

            hook_BadelineBoost_BoostRoutine?.Dispose();
            hook_BadelineBoost_BoostRoutine = null;
        }

        private static void BadelineBoost_OnPlayer(On.Celeste.BadelineBoost.orig_OnPlayer orig, BadelineBoost self, Player player)
        {
            if (GravityHelperModule.PlayerComponent is { } playerComponent)
            {
                // update to the expected gravity if custom entity
                if (self is GravityBadelineBoost gravityBadelineBoost)
                    playerComponent.SetGravity(gravityBadelineBoost.CurrentDirection, 0f);
                // otherwise force normal gravity to prevent regular badeline boosts from breaking
                else if (playerComponent.CurrentGravity != GravityType.Normal)
                    playerComponent.SetGravity(GravityType.Normal, 0f);
            }

            orig(self, player);
        }

        private static void BadelineBoost_BoostRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(instr => instr.MatchLdfld<BadelineDummy>(nameof(BadelineDummy.Sprite)),
                instr => instr.MatchLdflda<GraphicsComponent>(nameof(GraphicsComponent.Scale))))
                throw new HookException("Couldn't find BadelineDummy.Sprite.Scale");

            // flip Badeline's sprite if Madeline is also flipped
            cursor.EmitDelegate<Func<BadelineDummy, BadelineDummy>>(dummy =>
            {
                if (GravityHelperModule.ShouldInvertPlayer)
                    dummy.Sprite.Scale.Y *= -1;
                return dummy;
            });
        });
    }
}
