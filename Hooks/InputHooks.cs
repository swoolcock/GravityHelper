// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class InputHooks
    {
        // ReSharper disable once InconsistentNaming
        private static Hook hook_Input_GrabCheck;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Input)} hooks...");

            var grabCheckMethod = typeof(Input).GetProperty(nameof(Input.GrabCheck), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
            hook_Input_GrabCheck = new Hook(grabCheckMethod, typeof(InputHooks).GetMethod(nameof(Input_GrabCheck), BindingFlags.Static | BindingFlags.NonPublic));
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Input)} hooks...");

            hook_Input_GrabCheck?.Dispose();
            hook_Input_GrabCheck = null;
        }

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
}
