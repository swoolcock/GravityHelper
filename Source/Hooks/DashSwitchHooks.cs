// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class DashSwitchHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(DashSwitch)} hooks...");
        IL.Celeste.DashSwitch.Update += DashSwitch_Update;
        On.Celeste.DashSwitch.ctor += DashSwitch_ctor;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(DashSwitch)} hooks...");
        IL.Celeste.DashSwitch.Update -= DashSwitch_Update;
        On.Celeste.DashSwitch.ctor -= DashSwitch_ctor;
    }

    private static void DashSwitch_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // trick a ceiling switch into thinking it's a floor switch
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<DashSwitch>(nameof(DashSwitch.side))))
            throw new HookException("Couldn't find ldfld DashSwitch.side");
        cursor.EmitDelegate<Func<DashSwitch.Sides, DashSwitch.Sides>>(s =>
            GravityHelperModule.ShouldInvertPlayer && s == DashSwitch.Sides.Up ? DashSwitch.Sides.Down : s);

        // send an upward dash into Up switches if holding something on top of them
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY")))
            throw new HookException("Couldn't find Vector2.UnitY");
        cursor.EmitInvertVectorDelegate();

        // make Up switches rise into the ceiling
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(2f)))
            throw new HookException("Couldn't find ldcr4 2f");
        cursor.EmitInvertFloatDelegate();
    });

    private static void DashSwitch_ctor(On.Celeste.DashSwitch.orig_ctor orig, DashSwitch self, Vector2 position, DashSwitch.Sides side, bool persistent, bool allgates, EntityID id, string spritename)
    {
        orig(self, position, side, persistent, allgates, id, spritename);
        if (side == DashSwitch.Sides.Up)
            self.startY = self.Y;
    }
}
