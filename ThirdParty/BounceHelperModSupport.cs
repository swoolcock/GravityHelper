// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [ThirdPartyMod("BounceHelper")]
    public class BounceHelperModSupport : ThirdPartyModSupport
    {
        // ReSharper disable InconsistentNaming
        private IDetour hook_BounceHelperModule_modDashUpdate;
        private IDetour hook_BounceHelperModule_modNormalUpdate;
        // ReSharper restore InconsistentNaming

        protected override void Load()
        {
            var bhmt = ReflectionCache.BounceHelperModuleType;

            var modDashUpdateMethod = bhmt?.GetMethod("modDashUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (modDashUpdateMethod != null)
                hook_BounceHelperModule_modDashUpdate = new ILHook(modDashUpdateMethod, BounceHelperModule_modDashUpdate);

            var modNormalUpdateMethod = bhmt?.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(m =>
                m.Name == "modNormalUpdate" && m.ReturnType == typeof(int));
            if (modNormalUpdateMethod != null)
                hook_BounceHelperModule_modNormalUpdate = new ILHook(modNormalUpdateMethod, BounceHelperModule_modNormalUpdate);
        }

        protected override void Unload()
        {
            hook_BounceHelperModule_modDashUpdate?.Dispose();
            hook_BounceHelperModule_modDashUpdate = null;
            hook_BounceHelperModule_modNormalUpdate?.Dispose();
            hook_BounceHelperModule_modNormalUpdate = null;
        }

        private static void BounceHelperModule_modDashUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.SubtractionPredicate))
                throw new HookException("Couldn't find sub UnitY");
            cursor.EmitInvertVectorDelegate();
        });

        private static void BounceHelperModule_modNormalUpdate(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(ILCursorExtensions.SubtractionPredicate))
                throw new HookException("Couldn't find sub UnitY");
            cursor.EmitInvertVectorDelegate();
        });
    }
}
