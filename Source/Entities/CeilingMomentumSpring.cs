// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/CeilingMomentumSpring")]
public class CeilingMomentumSpring : Spring
{
    public CeilingMomentumSpring(Vector2 position, string spritePath)
        : base(position, Orientations.Floor, true)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            spritePath = "objects/GravityHelper/springGreen/";
        }

        if (!spritePath.EndsWith("/"))
        {
            spritePath += "/";
        }

        sprite.Reset(GFX.Game, spritePath);
        sprite.Add("idle", "", 0.0f, new int[1]);
        sprite.Add("bounce", "", 0.07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
        sprite.Add("disabled", "white", 0.07f);
        sprite.Play("idle");
        sprite.Origin.X = sprite.Width / 2f;
        sprite.Origin.Y = sprite.Height;
        sprite.Rotation = MathF.PI;

        var playerCollider = Get<PlayerCollider>();
        playerCollider.OnCollide = newOnCollide;

        var holdableCollider = Get<HoldableCollider>();
        var pufferCollider = Get<PufferCollider>();

        Collider.Top += 6f;
        pufferCollider.Collider.Top += 6f;
        staticMover.SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY);
        staticMover.JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY);
    }

    public CeilingMomentumSpring(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Attr("sprite"))
    {
    }

    private void newOnCollide(Player player)
    {
        if (player.StateMachine.State == Player.StDreamDash || !playerCanUse)
        {
            return;
        }

        if (!GravityHelperModule.ShouldInvertPlayer)
        {
            if (player.Speed.Y > 0) return;
            BounceAnimate();
            GravitySpring.InvertedSuperBounce(player, Bottom);
            return;
        }

        var origDashAttacking = player.DashAttacking;
        var origSpeedX = player.Speed.X;
        var origSpeedY = player.Speed.Y;

        if (origDashAttacking && player.Speed == Vector2.Zero)
        {
            Vector2 bDSpeed = player.beforeDashSpeed;
            origSpeedX = bDSpeed.X;
            origSpeedY = bDSpeed.Y;
        }

        Vector2 origDashDir = player.DashDir;
        player.Speed.X = 0;
        player.Speed.Y = 0;

        BounceAnimate();
        player.SuperBounce(Bottom);

        player.Speed.X = origSpeedX;

        if (origSpeedY <= 0.0f)
            player.varJumpSpeed = player.Speed.Y = origSpeedY - 185f;
        else
        {
            if (origDashDir.Y > 0.0f && origDashAttacking)
            {
                player.varJumpSpeed = player.Speed.Y = -370f / 3;
            }
            else
            {
                player.Speed.Y = -185f;
            }
        }
    }
}
