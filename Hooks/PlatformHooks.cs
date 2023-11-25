// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks;

public static class PlatformHooks
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
        if (!cursor.TryGotoNext(instr => instr.MatchCallGeneric<Entity, JumpThru>(nameof(Entity.CollideFirstOutside), out _)))
            throw new HookException("Couldn't find CollideFirstOutside<JumpThru>()");

        // replace CollideFirstOutside<JumpThru> with a manual implementation that ignores upside down jumpthrus
        cursor.EmitDelegate<Func<Platform, Vector2, Platform>>((self, at) =>
        {
            foreach (Entity b in self.Scene.Tracker.Entities[typeof(JumpThru)])
            {
                // skip upside down jumpthrus
                if (b is JumpThru jt && jt.IsUpsideDownJumpThru())
                    continue;
                if (!Collide.Check(self, b) && Collide.Check(self, b, at))
                    return b as Platform;
            }
            return null;
        });

        // skip over the existing one
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
    });
}
