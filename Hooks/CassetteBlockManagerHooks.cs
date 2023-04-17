// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Hooks.Attributes;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace Celeste.Mod.GravityHelper.Hooks;

[HookFixture(typeof(CassetteBlockManager))]
public static class CassetteBlockManagerHooks {
    private static readonly FieldInfo current_index_field_info = typeof(CassetteBlockManager).GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);

    [OnHook(nameof(CassetteBlockManager.SetActiveIndex))]
    private static void CassetteBlockManager_SetActiveIndex(On.Celeste.CassetteBlockManager.orig_SetActiveIndex orig, CassetteBlockManager self, int index) {
        orig(self, index);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>()) {
            if (!component.Enabled) continue;
            component.CassetteState = component.CassetteIndex == index ? CassetteStates.On : CassetteStates.Off;
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>()) {
            if (!component.Enabled) continue;
            component.DidBecomeActive?.Invoke(index);
        }
    }

    [OnHook(nameof(CassetteBlockManager.StopBlocks))]
    private static void CassetteBlockManager_StopBlocks(On.Celeste.CassetteBlockManager.orig_StopBlocks orig, CassetteBlockManager self) {
        orig(self);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>()) {
            if (!component.Enabled) continue;
            component.CassetteState = CassetteStates.Off;
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>()) {
            if (!component.Enabled) continue;
            component.DidStop?.Invoke();
        }
    }

    [OnHook(nameof(CassetteBlockManager.SetWillActivate))]
    private static void CassetteBlockManager_SetWillActivate(On.Celeste.CassetteBlockManager.orig_SetWillActivate orig, CassetteBlockManager self, int index) {
        orig(self, index);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>()) {
            if (!component.Enabled) continue;
            if (component.CassetteState == CassetteStates.Off && component.CassetteIndex == index)
                component.CassetteState = CassetteStates.Appearing;
            else if (component.CassetteState == CassetteStates.On && component.CassetteIndex != index)
                component.CassetteState = CassetteStates.Disappearing;
        }

        var currentIndex = (int) current_index_field_info.GetValue(self);

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>()) {
            if (!component.Enabled) continue;
            component.WillToggle?.Invoke(index, currentIndex != index);
        }
    }

    [OnHook("SilentUpdateBlocks", BindingFlags.Instance | BindingFlags.NonPublic)]
    private static void CassetteBlockManager_SilentUpdateBlocks(On.Celeste.CassetteBlockManager.orig_SilentUpdateBlocks orig, CassetteBlockManager self) {
        orig(self);

        var currentIndex = (int) current_index_field_info.GetValue(self);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>()) {
            if (!component.Enabled) continue;
            component.TrySetActivatedSilently(currentIndex);
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>()) {
            if (!component.Enabled) continue;
            component.DidBecomeActive?.Invoke(currentIndex);
        }
    }
}
