// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class BoosterHooks
{
    // ReSharper disable once InconsistentNaming
    private static IDetour hook_Booster_BoostRoutine;

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Booster)} hooks...");

        On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
        IL.Celeste.Booster.Update += Booster_Update;

        hook_Booster_BoostRoutine = new ILHook(ReflectionCache.Booster_BoostRoutine.GetStateMachineTarget(), Booster_BoostRoutine);
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Booster)} hooks...");

        On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
        IL.Celeste.Booster.Update -= Booster_Update;

        hook_Booster_BoostRoutine?.Dispose();
        hook_Booster_BoostRoutine = null;
    }

    private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction)
    {
        orig(self, player, direction);
        if (self is GravityBooster gravityBooster)
            GravityHelperModule.PlayerComponent?.SetGravity(gravityBooster.GravityType);
    }

    private static void Booster_BoostRoutine(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Booster>("playerOffset")))
            throw new HookException("Couldn't find playerOffset");

        cursor.EmitInvertVectorDelegate();
    });

    private static void Booster_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Booster>("playerOffset")))
            throw new HookException("Couldn't find playerOffset");

        cursor.EmitInvertVectorDelegate();
    });
}
