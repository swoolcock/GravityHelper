// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class HoldableHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Holdable)} hooks...");

        IL.Celeste.Holdable.Release += Holdable_Release;
        On.Celeste.Holdable.Added += Holdable_Added;
        On.Celeste.Holdable.Pickup += Holdable_Pickup;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Holdable)} hooks...");

        IL.Celeste.Holdable.Release -= Holdable_Release;
        On.Celeste.Holdable.Added -= Holdable_Added;
        On.Celeste.Holdable.Pickup -= Holdable_Pickup;
    }

    private static void Holdable_Added(On.Celeste.Holdable.orig_Added orig, Holdable self, Entity entity)
    {
        orig(self, entity);

        if (entity.Get<GravityHoldable>() != null) return;
        entity.Add(new GravityHoldable());
    }

    private static void Holdable_Release(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, ILCursorExtensions.UnitYPredicate))
            throw new HookException("Couldn't find Vector2.UnitY");
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Vector2, Holdable, Vector2>>((v, h) =>
        {
            if (h.Entity?.ShouldInvertChecked() ?? false)
                return new Vector2(v.X, -v.Y);
            return v;
        });
    });

    private static bool Holdable_Pickup(On.Celeste.Holdable.orig_Pickup orig, Holdable self, Player player)
    {
        var rv = orig(self, player);
        if (rv && self.Entity?.Get<GravityHoldable>() is { } gravityHoldable)
            gravityHoldable.SetGravityHeld();
        return rv;
    }
}
