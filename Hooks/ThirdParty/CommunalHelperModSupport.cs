// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.ThirdParty
{
    [ThirdPartyMod("CommunalHelper")]
    public class CommunalHelperModSupport : ThirdPartyModSupport
    {
        // ReSharper disable once InconsistentNaming
        private static IDetour hook_CommunalHelper_ConnectedSolid_MoveVExact;

        protected override void Load()
        {
            var chcst = ReflectionCache.CommunalHelperConnectedSolidType;

            var moveVExactMethod = chcst?.GetMethod("MoveVExact", BindingFlags.Instance | BindingFlags.Public);
            if (moveVExactMethod != null)
            {
                var target = GetType().GetMethod(nameof(ConnectedSolid_MoveVExact), BindingFlags.Static | BindingFlags.NonPublic);
                hook_CommunalHelper_ConnectedSolid_MoveVExact = new Hook(moveVExactMethod, target);
            }
        }

        protected override void Unload()
        {
            hook_CommunalHelper_ConnectedSolid_MoveVExact?.Dispose();
            hook_CommunalHelper_ConnectedSolid_MoveVExact = null;
        }

        private static void ConnectedSolid_MoveVExact(Action<Solid, int> orig, Solid self, int move)
        {
            GravityHelperModule.SolidMoving = true;
            orig(self, move);
            GravityHelperModule.SolidMoving = false;
        }
    }
}
