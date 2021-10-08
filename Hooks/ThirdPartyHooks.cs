// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class ThirdPartyHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), "Loading third party hooks...");
            ReflectionCache.LoadThirdPartyTypes();
            executeIfAvailable("SpeedrunTool", true, loadSpeedrunTool);
            executeIfAvailable("FancyTileEntities", true, loadFancyTileEntities);
            executeIfAvailable("MaddyCrown", true, loadMaddyCrown);
            executeIfAvailable("MaxHelpingHand", true, loadMaxHelpingHand);
            executeIfAvailable("FrostHelper", true, loadFrostHelper);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), "Unloading third party hooks...");
            executeIfAvailable("SpeedrunTool", false, unloadSpeedrunTool);
            executeIfAvailable("FancyTileEntities", false, unloadFancyTileEntities);
            executeIfAvailable("MaddyCrown", false, unloadMaddyCrown);
            executeIfAvailable("MaxHelpingHand", false, unloadMaxHelpingHand);
            executeIfAvailable("FrostHelper", false, unloadFrostHelper);
        }

        #region FancyTileEntities

        private static IDetour hook_FancyFallingBlock_MoveVExact;
        private static IDetour hook_FancyFallingBlock_GetLandSoundIndex;
        private static IDetour hook_FancyFallingBlock_GetStepSoundIndex;

        private static void loadFancyTileEntities()
        {
            var ffbt = ReflectionCache.FancyFallingBlockType;
            var ffbtMoveVExactMethod = ffbt?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetLandSoundIndexMethod = ffbt?.GetMethod("GetLandSoundIndex", BindingFlags.Instance | BindingFlags.Public);
            var ffbtGetStepSoundIndexMethod = ffbt?.GetMethod("GetStepSoundIndex", BindingFlags.Instance | BindingFlags.Public);

            if (ffbtMoveVExactMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_MoveVExact), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_MoveVExact = new Hook(ffbtMoveVExactMethod, target);
            }

            if (ffbtGetLandSoundIndexMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_GetLandSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetLandSoundIndex = new Hook(ffbtGetLandSoundIndexMethod, target);
            }

            if (ffbtGetStepSoundIndexMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(FancyFallingBlock_GetStepSoundIndex), BindingFlags.Static | BindingFlags.NonPublic);
                hook_FancyFallingBlock_GetStepSoundIndex = new Hook(ffbtGetStepSoundIndexMethod, target);
            }
        }

        private static void unloadFancyTileEntities()
        {
            hook_FancyFallingBlock_MoveVExact?.Dispose();
            hook_FancyFallingBlock_MoveVExact = null;

            hook_FancyFallingBlock_GetLandSoundIndex?.Dispose();
            hook_FancyFallingBlock_GetLandSoundIndex = null;

            hook_FancyFallingBlock_GetStepSoundIndex?.Dispose();
            hook_FancyFallingBlock_GetStepSoundIndex = null;
        }

        private static void FancyFallingBlock_MoveVExact(Action<FallingBlock, int> orig, FallingBlock self, int move)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, move);
                return;
            }

            GravityHelperModule.SolidMoving = true;
            orig(self, move);
            GravityHelperModule.SolidMoving = false;
        }

        private static int FancyFallingBlock_GetLandSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        private static int FancyFallingBlock_GetStepSoundIndex(Func<FallingBlock, Entity, int> orig, FallingBlock self, Entity entity)
        {
            if (!GravityHelperModule.ShouldInvert)
                return orig(self, entity);

            int num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopCenter - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopLeft - Vector2.UnitY * 4f);
            if (num == -1) num = self.CallFancyFallingBlockSurfaceSoundIndexAt(entity.TopRight - Vector2.UnitY * 4f);
            return num;
        }

        #endregion

        #region SpeedrunTool

        private static object _speedrunToolSaveLoadAction;

        private static void loadSpeedrunTool()
        {
            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;
            var slActionDelegateType = slat.GetNestedType("SlAction");

            var saveState = Delegate.CreateDelegate(slActionDelegateType, typeof(ThirdPartyHooks).GetMethod(nameof(speedrunToolSaveState), BindingFlags.NonPublic | BindingFlags.Static)!);
            var loadState = Delegate.CreateDelegate(slActionDelegateType, typeof(ThirdPartyHooks).GetMethod(nameof(speedrunToolLoadState), BindingFlags.NonPublic | BindingFlags.Static)!);

            var cons = slat.GetConstructors().First(c => c.GetParameters().Length == 3);
            _speedrunToolSaveLoadAction = cons.Invoke(new object[] {saveState, loadState, null});
            all.Add(_speedrunToolSaveLoadAction);
        }

        private static void speedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedValues, Level level)
        {
            if (!savedValues.TryGetValue(typeof(GravityHelperModule), out var dict)) return;
            GravityHelperModule.LoadState(dict, level);
        }

        private static void speedrunToolSaveState(Dictionary<Type, Dictionary<string, object>> savedValues, Level level)
        {
            if (!savedValues.TryGetValue(typeof(GravityHelperModule), out var dict)) dict = savedValues[typeof(GravityHelperModule)] = new Dictionary<string, object>();
            GravityHelperModule.SaveState(dict, level);
        }

        private static void unloadSpeedrunTool()
        {
            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;
            all.Remove(_speedrunToolSaveLoadAction);
            _speedrunToolSaveLoadAction = null;
        }

        #endregion

        #region MaddyCrown

        private static IDetour hook_MaddyCrownModule_Player_Update;

        private static void loadMaddyCrown()
        {
            var mcmt = ReflectionCache.MaddyCrownModuleType;
            var playerUpdateMethod = mcmt?.GetMethod("Player_Update", BindingFlags.Instance | BindingFlags.NonPublic);

            if (playerUpdateMethod != null)
                hook_MaddyCrownModule_Player_Update = new ILHook(playerUpdateMethod, MaddyCrownModule_Player_Update);
        }

        private static void unloadMaddyCrown()
        {
            hook_MaddyCrownModule_Player_Update?.Dispose();
            hook_MaddyCrownModule_Player_Update = null;
        }

        private static void MaddyCrownModule_Player_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchStfld<GraphicsComponent>(nameof(GraphicsComponent.Position))))
                throw new HookException("Couldn't patch MaddyCrownModule.Player_Update");

            cursor.EmitDelegate<Action<GraphicsComponent, Vector2>>((sprite, pos) =>
            {
                sprite.Position = new Vector2(pos.X, Math.Abs(pos.Y) * (GravityHelperModule.ShouldInvert ? 1 : -1));
                sprite.Scale.Y = GravityHelperModule.ShouldInvert ? -1 : 1;
            });
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });

        #endregion

        #region MaxHelpingHand

        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp;
        private static IDetour hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove;

        private static void loadMaxHelpingHand()
        {
            var mhhudjt = ReflectionCache.MaxHelpingHandUpsideDownJumpThruType;

            var playerMovingUpMethod = mhhudjt?.GetMethod("playerMovingUp", BindingFlags.Static | BindingFlags.NonPublic);
            if (playerMovingUpMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(MaxHelpingHand_UpsideDownJumpThru_playerMovingUp), BindingFlags.Static | BindingFlags.NonPublic);
                hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp = new Hook(playerMovingUpMethod, target);
            }

            var updateClimbMoveMethod = mhhudjt?.GetMethod("updateClimbMove", BindingFlags.Static | BindingFlags.NonPublic);
            if (updateClimbMoveMethod != null)
            {
                var target = typeof(ThirdPartyHooks).GetMethod(nameof(MaxHelpingHand_UpsideDownJumpThru_updateClimbMove), BindingFlags.Static | BindingFlags.NonPublic);
                hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove = new Hook(updateClimbMoveMethod, target);
            }
        }

        private static void unloadMaxHelpingHand()
        {
            hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_playerMovingUp = null;
            hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove?.Dispose();
            hook_MaxHelpingHand_UpsideDownJumpThru_updateClimbMove = null;
        }

        private static int MaxHelpingHand_UpsideDownJumpThru_updateClimbMove(Func<Player, int, int> orig, Player player, int lastClimbMove) =>
            GravityHelperModule.ShouldInvert ? lastClimbMove : orig(player, lastClimbMove);

        private static bool MaxHelpingHand_UpsideDownJumpThru_playerMovingUp(Func<Player, bool> orig, Player player) =>
            orig(player) != GravityHelperModule.ShouldInvert;

        #endregion

        #region FrostHelper

        private static IDetour hook_FrostHelper_CustomSpring_OnCollide;

        private static void loadFrostHelper()
        {
            var fhcst = ReflectionCache.FrostHelperCustomSpringType;
            var onCollideMethod = fhcst?.GetMethod("OnCollide", BindingFlags.Instance | BindingFlags.NonPublic);

            if (onCollideMethod != null)
                hook_FrostHelper_CustomSpring_OnCollide = new ILHook(onCollideMethod, FrostHelper_CustomSpring_OnCollide);
        }

        private static void unloadFrostHelper()
        {
            hook_FrostHelper_CustomSpring_OnCollide?.Dispose();
            hook_FrostHelper_CustomSpring_OnCollide = null;
        }

        private static void FrostHelper_CustomSpring_OnCollide(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert first Speed.Y check
            if (!cursor.TryGotoNext(instr => instr.MatchLdcR4(0), instr => instr.MatchBltUn(out _)))
                throw new HookException("Couldn't find first Speed.Y check.");
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find first SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // invert second Speed.Y check
            if (!cursor.TryGotoNext(instr => instr.MatchLdcR4(0), instr => instr.MatchBgtUn(out _)))
                throw new HookException("Couldn't find second Speed.Y check.");
            cursor.EmitInvertFloatDelegate();

            // replace SuperBounce with GravityHelper version
            if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.SuperBounce))))
                throw new HookException("Couldn't find second SuperBounce.");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.EmitDelegate<Action<Player, float>>(GravitySpring.InvertedSuperBounce);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // cancel the negative
            if (!cursor.TryGotoNext(instr => instr.MatchNeg()))
                throw new HookException("Couldn't find neg");
            cursor.EmitLoadShouldInvert();
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        });

        #endregion

        private static void executeIfAvailable(string name, bool loading, Action loader)
        {
            if (!Everest.Modules.Any(m => m.Metadata.Name == name))
                return;

            Logger.Log(nameof(GravityHelperModule), $"{(loading ? "Loading" : "Unloading")} {name} hooks...");
            loader();
        }
    }
}
