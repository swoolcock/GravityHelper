// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Components;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class CassetteBlockManagerHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(CassetteBlockManager)} hooks...");
        On.Celeste.CassetteBlockManager.SetActiveIndex += CassetteBlockManager_SetActiveIndex;
        On.Celeste.CassetteBlockManager.StopBlocks += CassetteBlockManager_StopBlocks;
        On.Celeste.CassetteBlockManager.SetWillActivate += CassetteBlockManager_SetWillActivate;
        On.Celeste.CassetteBlockManager.SilentUpdateBlocks += CassetteBlockManager_SilentUpdateBlocks;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(CassetteBlockManager)} hooks...");
        On.Celeste.CassetteBlockManager.SetActiveIndex -= CassetteBlockManager_SetActiveIndex;
        On.Celeste.CassetteBlockManager.StopBlocks -= CassetteBlockManager_StopBlocks;
        On.Celeste.CassetteBlockManager.SetWillActivate -= CassetteBlockManager_SetWillActivate;
        On.Celeste.CassetteBlockManager.SilentUpdateBlocks -= CassetteBlockManager_SilentUpdateBlocks;
    }

    private static void CassetteBlockManager_SetActiveIndex(On.Celeste.CassetteBlockManager.orig_SetActiveIndex orig, CassetteBlockManager self, int index)
    {
        orig(self, index);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>())
        {
            if (!component.Enabled) continue;
            component.CassetteState = component.CassetteIndex == index ? CassetteStates.On : CassetteStates.Off;
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>())
        {
            if (!component.Enabled) continue;
            component.DidBecomeActive?.Invoke(index);
        }
    }

    private static void CassetteBlockManager_StopBlocks(On.Celeste.CassetteBlockManager.orig_StopBlocks orig, CassetteBlockManager self)
    {
        orig(self);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>())
        {
            if (!component.Enabled) continue;
            component.CassetteState = CassetteStates.Off;
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>())
        {
            if (!component.Enabled) continue;
            component.DidStop?.Invoke();
        }
    }

    private static void CassetteBlockManager_SetWillActivate(On.Celeste.CassetteBlockManager.orig_SetWillActivate orig, CassetteBlockManager self, int index)
    {
        orig(self, index);

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>())
        {
            if (!component.Enabled) continue;
            if (component.CassetteState == CassetteStates.Off && component.CassetteIndex == index)
                component.CassetteState = CassetteStates.Appearing;
            else if (component.CassetteState == CassetteStates.On && component.CassetteIndex != index)
                component.CassetteState = CassetteStates.Disappearing;
        }

        var currentIndex = self.currentIndex;

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>())
        {
            if (!component.Enabled) continue;
            component.WillToggle?.Invoke(index, currentIndex != index);
        }
    }

    private static void CassetteBlockManager_SilentUpdateBlocks(On.Celeste.CassetteBlockManager.orig_SilentUpdateBlocks orig, CassetteBlockManager self)
    {
        orig(self);

        var currentIndex = self.currentIndex;

        foreach (CassetteComponent component in self.Scene.Tracker.GetComponents<CassetteComponent>())
        {
            if (!component.Enabled) continue;
            component.TrySetActivatedSilently(currentIndex);
        }

        foreach (CassetteListener component in self.Scene.Tracker.GetComponents<CassetteListener>())
        {
            if (!component.Enabled) continue;
            component.DidBecomeActive?.Invoke(currentIndex);
        }
    }
}