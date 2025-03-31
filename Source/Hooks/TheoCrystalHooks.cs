// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class TheoCrystalHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(TheoCrystal)} hooks...");

        IL.Celeste.TheoCrystal.Update += TheoCrystal_Update;
        IL.Celeste.TheoCrystal.OnCollideV += TheoCrystal_OnCollideV;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(TheoCrystal)} hooks...");

        IL.Celeste.TheoCrystal.Update -= TheoCrystal_Update;
        IL.Celeste.TheoCrystal.OnCollideV -= TheoCrystal_OnCollideV;
    }

    private static void TheoCrystal_OnCollideV(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        // make falling theo trigger ceiling dash switches
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y))))
            throw new HookException("Couldn't find Speed.Y");
        cursor.EmitActorInvertFloatDelegate(OpCodes.Ldarg_0);
    });

    private static void TheoCrystal_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // find the start of the bounds check
        if (!cursor.TryGotoNext(instr => instr.MatchLdarg(0),
            instr => instr.MatchCall<Entity>("get_Center"),
            instr => instr.MatchLdfld<Vector2>(nameof(Vector2.X))))
            throw new HookException("Couldn't find start of bounds check");

        // find the instruction after the bounds check
        var cursor2 = cursor.Clone();
        if (!cursor2.TryGotoNext(instr => instr.MatchLdarg(0),
            instr => instr.MatchCall<Entity>("get_X"),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<TheoCrystal>("Level")))
            throw new HookException("Couldn't find instruction after bounds check");

        // replace with custom checks
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<TheoCrystal, bool>>(self => self.ShouldInvert());
        cursor.Emit(OpCodes.Brfalse, cursor.Next);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<TheoCrystal>>(self =>
        {
            var level = self.SceneAs<Level>();
            if (self.Center.X > level.Bounds.Right)
            {
                self.MoveH(32f * Engine.DeltaTime);
                if (self.Left - 8f > level.Bounds.Right)
                    self.RemoveSelf();
            }
            else if (self.Left < level.Bounds.Left)
            {
                self.Left = level.Bounds.Left;
                self.Speed.X *= -0.4f;
            }
            else if (self.Bottom > level.Bounds.Bottom + 4)
            {
                self.Bottom = level.Bounds.Bottom - 4;
                self.Speed.Y = 0f;
            }
            else if (self.Top < level.Bounds.Top && SaveData.Instance.Assists.Invincible)
            {
                self.Top = level.Bounds.Top;
                self.Speed.Y = -300f;
                Audio.Play(SFX.game_assist_screenbottom, self.Position);
            }
            else if (self.Bottom < level.Bounds.Top)
                self.Die();
        });

        cursor.Emit(OpCodes.Br_S, cursor2.Next);
    });
}
