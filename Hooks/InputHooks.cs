// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class InputHooks
{
    // ReSharper disable once InconsistentNaming
    private static Hook hook_Input_GrabCheck;

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Input)} hooks...");

        IL.Celeste.Input.GetAimVector += Input_GetAimVector;

        var grabCheckMethod = typeof(Input).GetProperty(nameof(Input.GrabCheck), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
        hook_Input_GrabCheck = new Hook(grabCheckMethod, typeof(InputHooks).GetMethod(nameof(Input_GrabCheck), BindingFlags.Static | BindingFlags.NonPublic));
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Input)} hooks...");

        IL.Celeste.Input.GetAimVector -= Input_GetAimVector;

        hook_Input_GrabCheck?.Dispose();
        hook_Input_GrabCheck = null;
    }

    private static void Input_GetAimVector(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdloc(0),
            instr => instr.MatchCall(typeof(Calc), nameof(Calc.Angle)),
            instr => instr.MatchStloc(1),
            instr => instr.MatchLdloc(1)))
            throw new HookException("Couldn't find num < 0f ? 1 : 0");

        cursor.EmitDelegate<Func<float, float>>(f =>
            GravityHelperModule.Settings.ControlScheme == GravityHelperModuleSettings.ControlSchemeSetting.Absolute &&
            GravityHelperModule.ShouldInvertPlayer ? -f : f);
    });

    private static bool Input_GrabCheck(Func<bool> orig)
    {
        var session = GravityHelperModule.Session;
        if (session.DisableGrab &&
            (session.VvvvvvMode == VvvvvvMode.TriggerBased && session.VvvvvvTrigger ||
            session.VvvvvvMode == VvvvvvMode.On))
            return false;
        return orig();
    }
}