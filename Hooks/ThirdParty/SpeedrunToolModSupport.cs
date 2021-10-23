// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("SpeedrunTool", "3.6.18")]
    public class SpeedrunToolModSupport : ThirdPartyModSupport
    {
        private static object _speedrunToolSaveLoadAction;

        protected override void Load()
        {
            var slat = ReflectionCache.GetModdedTypeByName("SpeedrunTool", "Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction");
            var allFieldInfo = slat?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
            if (allFieldInfo?.GetValue(null) is not IList all) return;
            var slActionDelegateType = slat.GetNestedType("SlAction");

            var saveState = Delegate.CreateDelegate(slActionDelegateType, GetType().GetMethod(nameof(speedrunToolSaveState), BindingFlags.NonPublic | BindingFlags.Static)!);
            var loadState = Delegate.CreateDelegate(slActionDelegateType, GetType().GetMethod(nameof(speedrunToolLoadState), BindingFlags.NonPublic | BindingFlags.Static)!);

            var cons = slat.GetConstructors().First(c => c.GetParameters().Length == 3);
            _speedrunToolSaveLoadAction = cons.Invoke(new object[] {saveState, loadState, null});
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
