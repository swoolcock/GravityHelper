// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("SpeedrunTool", "3.7.2")]
    internal class SpeedrunToolModSupport : ThirdPartyModSupport
    {
        private static object _speedrunToolSaveLoadAction;

        protected override void Load(GravityHelperModule.HookLevel hookLevel)
        {
            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;
            var slActionDelegateType = slat.GetNestedType("SlAction");

            var saveState = Delegate.CreateDelegate(slActionDelegateType, GetType().GetMethod(nameof(speedrunToolSaveState), BindingFlags.NonPublic | BindingFlags.Static)!);
            var loadState = Delegate.CreateDelegate(slActionDelegateType, GetType().GetMethod(nameof(speedrunToolLoadState), BindingFlags.NonPublic | BindingFlags.Static)!);

            // this constructor changes with almost every version of SpeedrunTool,
            // so we'll just take the first with at least two parameters and hope for the best
            var cons = slat.GetConstructors().First(c => c.GetParameters().Length >= 2);
            var parameters = cons.GetParameters();
            var args = new object[parameters.Length];
            args[0] = saveState;
            args[1] = loadState;

            _speedrunToolSaveLoadAction = cons.Invoke(args);
            all.Add(_speedrunToolSaveLoadAction);
        }

        protected override void Unload()
        {
            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;
            all.Remove(_speedrunToolSaveLoadAction);
            _speedrunToolSaveLoadAction = null;
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
    }
}
