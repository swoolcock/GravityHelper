// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class SeekerHooks
{
    private static IDetour hook_Seeker_RegenerateCoroutine;

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Seeker)} hooks...");

        On.Celeste.Seeker.Update += Seeker_Update;

        hook_Seeker_RegenerateCoroutine = new ILHook(ReflectionCache.Seeker_RegenerateCoroutine.GetStateMachineTarget(), Seeker_RegenerateCoroutine);
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Seeker)} hooks...");

        On.Celeste.Seeker.Update -= Seeker_Update;

        hook_Seeker_RegenerateCoroutine?.Dispose();
        hook_Seeker_RegenerateCoroutine = null;
    }

    private static void Seeker_Update(On.Celeste.Seeker.orig_Update orig, Seeker self)
    {
        var bounceHitbox = self.bounceHitbox;
        var attackHitbox = self.attackHitbox;

        if (GravityHelperModule.ShouldInvertPlayer != bounceHitbox.Top > attackHitbox.Top)
        {
            bounceHitbox.Top = -bounceHitbox.Bottom;
            attackHitbox.Top = -attackHitbox.Bottom;
        }

        orig(self);
    }

    private static void Seeker_RegenerateCoroutine(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch))) ||
            !cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position))))
            throw new HookException("Couldn't find Entity.Position.");

        cursor.Emit(OpCodes.Ldloc_2);
        cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
            !GravityHelperModule.ShouldInvertPlayer ? v : new Vector2(v.X, p.CenterY - (v.Y - p.CenterY)));
    });
}