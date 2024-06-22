// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class BumperHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Bumper)} hooks...");
        IL.Celeste.Bumper.Update += Bumper_Update;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Bumper)} hooks...");
        IL.Celeste.Bumper.Update -= Bumper_Update;
    }

    private static void Bumper_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Bumper>(nameof(Bumper.P_Ambience))))
            throw new HookException("Couldn't find P_Ambience.");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<ParticleType, Bumper, ParticleType>>((pt, self) =>
            self is GravityBumper gravityBumper ? gravityBumper.GetAmbientParticleType() : pt);
    });
}
