// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal static class HookUtils
    {
        public static void LogCurrentMethod(string message, [CallerMemberName] string caller = null) =>
            Logger.Log(nameof(GravityHelperModule), $"{caller}: {message}");

        public static void LogCurrentMethod(LogLevel logLevel, string message, [CallerMemberName] string caller = null) =>
            Logger.Log(logLevel, nameof(GravityHelperModule), $"{caller}: {message}");

        public static void SafeHook(Action action, [CallerMemberName] string caller = null)
        {
            LogCurrentMethod("Hooking IL...", caller);

            try
            {
                action();
            }
            catch (HookException hookException)
            {
                LogCurrentMethod(LogLevel.Error, hookException.Message, caller);
                throw;
            }
        }
    }
}
