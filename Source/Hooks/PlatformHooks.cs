// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class PlatformHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Platform)} hooks...");

        IL.Celeste.Platform.MoveVExactCollideSolids += Platform_MoveVExactCollideSolids;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Platform)} hooks...");

        IL.Celeste.Platform.MoveVExactCollideSolids -= Platform_MoveVExactCollideSolids;
    }

    private static void Platform_MoveVExactCollideSolids(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        ILLabel skipLabel = null;
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCallGeneric<Entity>(nameof(Entity.CollideFirstOutside), out _)) ||
            !cursor.TryGotoPrev(MoveType.AfterLabel, i => i.MatchLdarg(1), i => i.MatchLdcI4(0), i => i.MatchBle(out skipLabel)))
            throw new HookException("Couldn't find moveV > 0");

        var cursor2 = cursor.Clone();
        ILLabel breakLabel = null;
        if (!cursor2.TryGotoNext(i => i.MatchLdloc(3), i => i.MatchBrtrue(out breakLabel)))
            throw new HookException("Couldn't find platform != null");

        // use custom collision that separates regular and upside down jumpthrus
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(collideCorrectJumpThrus);
        cursor.Emit(OpCodes.Stloc_3);

        // if a platform was found, break out of the loop
        cursor.Emit(OpCodes.Ldloc_3);
        cursor.Emit(OpCodes.Brtrue_S, breakLabel);

        // skip over the old collision code
        cursor.Emit(OpCodes.Br_S, skipLabel);
    });

    private static Platform collideCorrectJumpThrus(Platform self, int moveV, int num)
    {
        Platform platform = null;
        if (moveV > 0)
        {
            platform = self.CollideFirstOutsideNotUpsideDownJumpThru(self.Position + Vector2.UnitY * num);
        }
        else if (moveV < 0)
        {
            platform = self.CollideFirstOutsideUpsideDownJumpThru(self.Position + Vector2.UnitY * num);
        }

        return platform;
    }
}
