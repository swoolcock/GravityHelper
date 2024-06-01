// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
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
        IL.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
        IL.Celeste.Bumper.Update += Bumper_Update;
        On.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Bumper)} hooks...");
        IL.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
        IL.Celeste.Bumper.Update -= Bumper_Update;
        On.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
    }

    private static void Bumper_OnPlayer(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch))))
            throw new HookException("Couldn't find ExplodeLaunch.");

        cursor.EmitInvertVectorDelegate();

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Bumper>>(self =>
        {
            if (self is GravityBumper gravityBumper && !gravityBumper.fireMode)
            {
                GravityHelperModule.PlayerComponent?.SetGravity(gravityBumper.GravityType);
                if (gravityBumper.SingleUse)
                    gravityBumper.SetFireMode(true);
            }
        });

        if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position))))
            throw new HookException("Couldn't find Entity.Position.");

        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
            !GravityHelperModule.ShouldInvertPlayer ? v : new Vector2(v.X, p.CenterY - (v.Y - p.CenterY)));

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Bumper>(nameof(Bumper.P_Launch))))
            throw new HookException("Couldn't find P_Launch.");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<ParticleType, Bumper, ParticleType>>((pt, self) =>
            self is GravityBumper gravityBumper ? gravityBumper.GetLaunchParticleType() : pt);
    });

    private static void Bumper_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Bumper>(nameof(Bumper.P_Ambience))))
            throw new HookException("Couldn't find P_Ambience.");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<ParticleType, Bumper, ParticleType>>((pt, self) =>
            self is GravityBumper gravityBumper ? gravityBumper.GetAmbientParticleType() : pt);
    });

    private static void Bumper_OnPlayer(On.Celeste.Bumper.orig_OnPlayer orig, Bumper self, Player player)
    {
        if (self is not GravityBumper gravityBumper)
        {
            orig(self, player);
            return;
        }

        var oldRespawnTimer = self.respawnTimer;
        orig(self, player);
        if (oldRespawnTimer <= 0 && self.respawnTimer > 0)
            self.respawnTimer = gravityBumper._respawnTime;
    }
}
