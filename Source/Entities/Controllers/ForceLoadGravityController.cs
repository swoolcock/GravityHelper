// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Entities.Controllers;

[CustomEntity("GravityHelper/ForceLoadGravityController")]
[Tracked]
public class ForceLoadGravityController : BaseGravityController<ForceLoadGravityController>
{
    public ForceLoadGravityController(EntityData data, Vector2 offset)
        : base(data, offset)
    {
    }

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), "Loading render-only hooks...");

        On.Celeste.BadelineDummy.Render += BadelineDummy_Render;
        On.Celeste.PlayerSprite.Render += PlayerSprite_Render;
        IL.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
        IL.Celeste.PlayerHair.Render += PlayerHair_Render;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), "Unloading render-only hooks...");

        On.Celeste.BadelineDummy.Render -= BadelineDummy_Render;
        On.Celeste.PlayerSprite.Render -= PlayerSprite_Render;
        IL.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
        IL.Celeste.PlayerHair.Render -= PlayerHair_Render;
    }

    private static void BadelineDummy_Render(On.Celeste.BadelineDummy.orig_Render orig, BadelineDummy self)
    {
        if (!self.ShouldInvert())
        {
            orig(self);
            return;
        }

        var scale = self.Sprite.Scale;
        self.Sprite.Scale.Y = -scale.Y;
        orig(self);
        self.Sprite.Scale.Y = scale.Y;
    }

    private static void PlayerSprite_Render(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self)
    {
        if (self.Entity is Player)
        {
            orig(self);
            return;
        }

        var invert = false;
        if (self.Entity != null &&
            (self.Entity is BadelineOldsite || self.Entity.GetType() == ReflectionCache.CelesteNetGhostType))
        {
            invert = self.Entity.ShouldInvert();
        }

        var scaleY = self.Scale.Y;
        if (invert) self.Scale.Y = -scaleY;
        orig(self);
        if (invert) self.Scale.Y = scaleY;
    }

    private static void PlayerHair_AfterUpdate(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        void invertAdditions()
        {
            cursor.GotoNextAddition();
            EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_0);
            cursor.Index += 2;
            cursor.GotoNextAddition();
            EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_0);
        }

        // this.Nodes[0] = this.Sprite.RenderPosition + new Vector2(0.0f, -9f * this.Sprite.Scale.Y) + this.Sprite.HairOffset * new Vector2((float) this.Facing, 1f);
        invertAdditions();

        // Vector2 target = this.Nodes[0] + new Vector2((float) ((double) -(int) this.Facing * (double) this.StepInFacingPerSegment * 2.0), (float) Math.Sin((double) this.wave) * this.StepYSinePerSegment) + this.StepPerSegment;
        cursor.GotoNext(instr => instr.MatchLdfld<PlayerHair>(nameof(PlayerHair.StepYSinePerSegment)));
        invertAdditions();

        // target = this.Nodes[index] + new Vector2((float) -(int) this.Facing * this.StepInFacingPerSegment, (float) Math.Sin((double) this.wave + (double) index * 0.800000011920929) * this.StepYSinePerSegment) + this.StepPerSegment;
        cursor.GotoNext(instr => instr.MatchLdfld<PlayerHair>(nameof(PlayerHair.StepYSinePerSegment)));
        invertAdditions();
    });

    private static void PlayerHair_Render(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // Vector2 hairScale = this.GetHairScale(index);
        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
        EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_0);

        // this.GetHairTexture(index).Draw(this.Nodes[index], origin, this.GetHairColor(index), this.GetHairScale(index));
        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerHair>("GetHairScale"));
        EmitInvertVecForPlayerHair(cursor, OpCodes.Ldarg_0);
    });

    public static void EmitInvertVecForPlayerHair(ILCursor cursor, OpCode loadHairOpCode)
    {
        cursor.Emit(loadHairOpCode);
        cursor.EmitDelegate<Func<Vector2, PlayerHair, Vector2>>((v, p) =>
        {
            var inverted = new Vector2(v.X, -v.Y);

            // do player check by itself since this is a hot path
            if (p.Entity is Player)
                return GravityHelperModule.ShouldInvertPlayer ? inverted : v;

            if (p.Entity is PlayerDeadBody or BadelineDummy or BadelineOldsite ||
                p.Entity.GetType() == ReflectionCache.CelesteNetGhostType)
                return p.Entity.ShouldInvert() ? inverted : v;

            return v;
        });
    }
}
