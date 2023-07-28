// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

[Tracked]
public class JumpThruCollision : Component {
    public readonly JumpThruCollisionType Type;

    public JumpThruCollision(JumpThruCollisionType type) : base(false, false)
    {
        Type = type;
    }

    public enum JumpThruCollisionType
    {
        // Indicates that the top of the collider will block actors moving down
        // Default for vanilla jumpthrus and clouds, resort platforms, etc.
        Top,
        // Indicates that the bottom of the collider will block actors moving up
        // Default for upside down jumpthru
        Bottom,
        // Indicates that actors can always stand on the jumpthru, matching their gravity
        Stand,
        // Indicates that actors will always bonk on the jumpthru, but they can fall through it
        Bonk,
    }
}

public static class JumpThruExtensions
{
    public static bool IsActorRiding(this JumpThru self, Actor actor)
    {
        if (actor.IgnoreJumpThrus) return false;
        var shouldInvert = actor.ShouldInvertChecked();
        var type = self.Get<JumpThruCollision>()?.Type ?? JumpThruCollision.JumpThruCollisionType.Top;
        if (type == JumpThruCollision.JumpThruCollisionType.Top ? !shouldInvert :
            type == JumpThruCollision.JumpThruCollisionType.Bottom ? shouldInvert :
            type == JumpThruCollision.JumpThruCollisionType.Stand)
        {
            var direction = shouldInvert ? -Vector2.UnitY : Vector2.UnitY;
            return actor.CollideCheckOutside(self, actor.Position + direction);
        }
        return false;
    }

    public static bool DoesActorBonk(this JumpThru self, Actor actor)
    {
        if (actor.IgnoreJumpThrus) return false;
        var shouldInvert = actor.ShouldInvertChecked();
        var type = self.Get<JumpThruCollision>()?.Type ?? JumpThruCollision.JumpThruCollisionType.Top;
        if (type == JumpThruCollision.JumpThruCollisionType.Top ? shouldInvert :
            type == JumpThruCollision.JumpThruCollisionType.Bottom ? !shouldInvert :
            type == JumpThruCollision.JumpThruCollisionType.Bonk)
        {
            var direction = shouldInvert ? Vector2.UnitY : -Vector2.UnitY;
            return actor.CollideCheckOutside(self, actor.Position + direction);
        }
        return false;
    }
}
