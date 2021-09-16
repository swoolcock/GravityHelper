using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
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
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), "Unloading third party hooks...");
            // executeIfAvailable("SpeedrunTool", false, unloadSpeedrunTool);
            executeIfAvailable("FancyTileEntities", false, unloadFancyTileEntities);
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

        private static bool speedrunToolLoaded;

        private static void loadSpeedrunTool()
        {
            // we only ever load this exactly once
            if (speedrunToolLoaded) return;
            speedrunToolLoaded = true;

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

        private static void executeIfAvailable(string name, bool loading, Action loader)
        {
            if (!Everest.Modules.Any(m => m.Metadata.Name == name))
                return;

            Logger.Log(nameof(GravityHelperModule), $"{(loading ? "Loading" : "Unloading")} {name} hooks...");
            loader();
        }
    }
}
