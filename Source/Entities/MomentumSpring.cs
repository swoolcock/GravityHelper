// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities;

[CustomEntity("GravityHelper/MomentumSpring", "GravityHelper/CeilingMomentumSpring = LoadCeiling")]
public class MomentumSpring : Spring
{
    private Orientations _ourOrientation;

    public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
        new MomentumSpring(entityData.Position + offset,
            entityData.Attr("sprite"),
            Orientations.Ceiling,
            entityData.Bool("playerCanUse", true));

    public MomentumSpring(Vector2 position, string spritePath, Orientations orientation, bool playerCanUse)
        : base(position, (Spring.Orientations)((int)orientation % 3), playerCanUse)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            spritePath = "objects/GravityHelper/springGreen/";
        }

        if (!spritePath.EndsWith("/"))
        {
            spritePath += "/";
        }

        _ourOrientation = orientation;

        sprite.Reset(GFX.Game, spritePath);
        sprite.Add("idle", "", 0.0f, new int[1]);
        sprite.Add("bounce", "", 0.07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
        sprite.Add("disabled", "white", 0.07f);
        sprite.Play("idle");
        sprite.Origin.X = sprite.Width / 2f;
        sprite.Origin.Y = sprite.Height;

        var playerCollider = Get<PlayerCollider>();
        playerCollider.OnCollide = newOnCollide;

        var holdableCollider = Get<HoldableCollider>();
        var pufferCollider = Get<PufferCollider>();

        // update things by orientation
        switch (orientation)
        {
            case Orientations.Floor:
                sprite.Rotation = 0f;
                break;

            case Orientations.WallLeft:
                sprite.Rotation = (float)(Math.PI / 2f);
                break;

            case Orientations.WallRight:
                sprite.Rotation = (float)(-Math.PI / 2f);
                break;

            case Orientations.Ceiling:
                sprite.Rotation = (float)Math.PI;
                Collider.Top += 6f;
                pufferCollider.Collider.Top += 6;
                staticMover.SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY);
                staticMover.JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY);
                break;
        }
    }

    public MomentumSpring(EntityData data, Vector2 offset) : this(
        data.Position + offset,
        data.Attr("sprite"),
        data.Enum<Orientations>("orientation"),
        data.Bool("playerCanUse", true))
    {
    }

    private void newOnCollide(Player player)
    {
        if (player.StateMachine.State == Player.StDreamDash || !playerCanUse)
            return;

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

        var orientation = _ourOrientation;
        var inverted = GravityHelperModule.ShouldInvertPlayer;
        if (inverted)
        {
            // origSpeedY = -origSpeedY;
            // origDashDir.Y = -origDashDir.Y;
            if (orientation == Orientations.Floor)
                orientation = Orientations.Ceiling;
            else if (orientation == Orientations.Ceiling)
                orientation = Orientations.Floor;
        }

        switch (orientation)
        {
            case Orientations.Floor:
                player.SuperBounce(inverted ? Bottom : Top);
                player.Speed.X = origSpeedX;
                if (origSpeedY > 0f)
                {
                    if (origDashDir.Y > 0f && origDashAttacking)
                        player.varJumpSpeed = player.Speed.Y = -370f / 3;
                    else
                        player.Speed.Y = -185f;
                }
                else
                    player.varJumpSpeed = player.Speed.Y = origSpeedY - 185f;
                break;

            case Orientations.WallLeft:
                player.SideBounce(1, CenterRight.X, CenterRight.Y);
                player.varJumpSpeed = player.Speed.Y = origSpeedY - 140f;
                player.Speed.X = Math.Max(origSpeedX, 0f) + 240f;
                break;

            case Orientations.WallRight:
                player.SideBounce(-1, CenterLeft.X, CenterLeft.Y);
                player.varJumpSpeed = player.Speed.Y = origSpeedY - 140f;
                player.Speed.X = Math.Min(origSpeedX, 0f) - 240f;
                break;

            case Orientations.Ceiling:
                GravitySpring.InvertedSuperBounce(player, inverted ? Top : Bottom);
                player.Speed.X = origSpeedX;
                if (origSpeedY < 0f)
                {
                    if (origDashDir.Y < 0f && origDashAttacking)
                        player.varJumpSpeed = player.Speed.Y = 370f / 3;
                    else
                        player.Speed.Y = 185f;
                }
                else
                    player.varJumpSpeed = player.Speed.Y = origSpeedY + 185f;
                break;
        }
    }

    public new enum Orientations
    {
        Floor,
        WallLeft,
        WallRight,
        Ceiling,
    }
}
