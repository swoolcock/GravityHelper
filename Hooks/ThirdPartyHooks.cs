// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class ThirdPartyHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), "Loading third party hooks...");
            ReflectionCache.LoadThirdPartyTypes();
            // executeIfAvailable("SpeedrunTool", true, loadSpeedrunTool);
            executeIfAvailable("FancyTileEntities", true, loadFancyTileEntities);
            executeIfAvailable("MaddyCrown", true, loadMaddyCrown);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), "Unloading third party hooks...");
            // executeIfAvailable("SpeedrunTool", false, unloadSpeedrunTool);
            executeIfAvailable("FancyTileEntities", false, unloadFancyTileEntities);
            executeIfAvailable("MaddyCrown", false, unloadMaddyCrown);
        }

        #region FancyTileEntities

        // ReSharper disable InconsistentNaming
        private static IDetour hook_FancyFallingBlock_MoveVExact;
        private static IDetour hook_FancyFallingBlock_GetLandSoundIndex;
        private static IDetour hook_FancyFallingBlock_GetStepSoundIndex;
        // ReSharper restore InconsistentNaming

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

        private static bool _speedrunToolLoaded;

        private static void loadSpeedrunTool()
        {
            // we only ever load this exactly once
            if (_speedrunToolLoaded) return;
            _speedrunToolLoaded = true;

            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;

            Action<Dictionary<Type, Dictionary<string, object>>, Level> saveState = (savedValues, _) =>
            {
                if (!savedValues.TryGetValue(typeof(GravityHelperModule), out var dict))
                    dict = savedValues[typeof(GravityHelperModule)] = new Dictionary<string, object>();
                GravityHelperModule.SaveState(dict);
            };

            Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState = (savedValues, _) =>
            {
                if (!savedValues.TryGetValue(typeof(GravityHelperModule), out var dict))
                    return;
                GravityHelperModule.LoadState(dict);
            };

            var cons = slat.GetConstructors().First(c => c.GetParameters().Length == 2);
            var saveLoadAction = cons.Invoke(new object[] {saveState, loadState});

            all.Add(saveLoadAction);
        }

        private static void unloadSpeedrunTool()
        {
            // at the moment it's just too hard to unload
        }

        #endregion

        #region MaddyCrown

        // ReSharper disable once InconsistentNaming
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

        private static void executeIfAvailable(string name, bool loading, Action loader)
        {
            if (!Everest.Modules.Any(m => m.Metadata.Name == name))
                return;

            Logger.Log(nameof(GravityHelperModule), $"{(loading ? "Loading" : "Unloading")} {name} hooks...");
            loader();
        }
    }
}
