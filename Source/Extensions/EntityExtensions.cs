// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Extensions;

internal static class EntityExtensions
{
    #region Collision Helpers

    public static T CollideFirstOrDefault<T>(this Entity entity) where T : Entity =>
        entity.Scene.Tracker.Entities.ContainsKey(typeof(T)) ? entity.CollideFirst<T>() : default;

    public static T CollideFirstOrDefault<T>(this Entity entity, Vector2 at) where T : Entity =>
        entity.Scene.Tracker.Entities.ContainsKey(typeof(T)) ? entity.CollideFirst<T>(at) : default;

    public static T CollideFirstOutsideOrDefault<T>(this Entity entity, Vector2 at) where T : Entity =>
        entity.Scene.Tracker.Entities.ContainsKey(typeof(T)) ? entity.CollideFirstOutside<T>(at) : default;

    public static Entity CollideFirstOutside(this Entity entity, Type type, Vector2 at, Type[] notTypes = null, bool checkAllEntities = false)
    {
        IEnumerable<Entity> entities = getEntities(entity.Scene, type, notTypes, checkAllEntities);
        if (entities == null) return null;

        foreach (Entity b in entities)
        {
            if (!Collide.Check(entity, b) && Collide.Check(entity, b, at))
                return b;
        }

        return default;
    }

    public static bool CollideCheckOutside(this Entity entity, Type type, Vector2 at, Type[] notTypes = null, bool checkAllEntities = false) =>
        entity.CollideFirstOutside(type, at, notTypes, checkAllEntities) != null;

    public static Entity CollideFirst(this Entity entity, Type type, Vector2 at, Type[] notTypes = null, bool checkAllEntities = false)
    {
        IEnumerable<Entity> entities = getEntities(entity.Scene, type, notTypes, checkAllEntities);
        return entities == null ? null : Collide.First(entity, entities, at);
    }

    public static bool CollideCheck(this Entity entity, Type type, Vector2 at, Type[] notTypes = null, bool checkAllEntities = false)
    {
        IEnumerable<Entity> entities = getEntities(entity.Scene, type, notTypes, checkAllEntities);
        return entities != null && Collide.Check(entity, entities, at);
    }

    public static bool CollideCheck(this Entity entity, Type type, Type[] notTypes = null, bool checkAllEntities = false)
    {
        IEnumerable<Entity> entities = getEntities(entity.Scene, type, notTypes, checkAllEntities);
        return entities != null && Collide.Check(entity, entities);
    }

    public static bool CollideCheckWhere<TEntity>(this Entity entity, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheck(e) && predicate(e))
                return true;
        }

        return false;
    }

    public static bool CollideCheckWhere<TEntity>(this Entity entity, Vector2 at, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheck(e, at) && predicate(e))
                return true;
        }

        return false;
    }

    public static bool CollideCheckOutsideWhere<TEntity>(this Entity entity, Vector2 at, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheckOutside(e, at) && predicate(e))
                return true;
        }

        return false;
    }

    public static TEntity CollideFirstWhere<TEntity>(this Entity entity, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheck(e) && predicate(e))
                return e;
        }

        return null;
    }

    public static TEntity CollideFirstWhere<TEntity>(this Entity entity, Vector2 at, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheck(e, at) && predicate(e))
                return e;
        }

        return null;
    }

    public static TEntity CollideFirstOutsideWhere<TEntity>(this Entity entity, Vector2 at, Predicate<TEntity> predicate)
        where TEntity : Entity
    {
        var all = entity.Scene.Tracker.GetEntities<TEntity>();

        foreach (TEntity e in all)
        {
            if (entity.CollideCheckOutside(e, at) && predicate(e))
                return e;
        }

        return null;
    }

    #endregion

    #region UpsideDownJumpThru Collision Helpers

    private static readonly List<Entity> _entityList = new List<Entity>();

    public static bool CollideCheckUpsideDownJumpThru(this Entity entity)
    {
        if (entity.CollideCheck<UpsideDownJumpThru>())
            return true;
        if (ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType != null && entity.CollideCheck(ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType))
            return true;
        return false;
    }

    public static bool CollideCheckNotUpsideDownJumpThru(this Entity entity)
    {
        if (!entity.Scene.Tracker.Entities.TryGetValue(typeof(JumpThru), out var entities))
            return false;

        _entityList.Clear();
        Collide.All(entity, entities, _entityList);
        var coll = _entityList.Any(e =>
            e.GetType() != ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType);
        _entityList.Clear();
        return coll;
    }

    public static bool CollideCheckUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        if (entity.CollideCheck<UpsideDownJumpThru>(at))
            return true;
        if (ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType != null && entity.CollideCheck(ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType, at))
            return true;
        return false;
    }

    public static bool CollideCheckNotUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        if (!entity.Scene.Tracker.Entities.TryGetValue(typeof(JumpThru), out var entities))
            return false;

        _entityList.Clear();
        Collide.All(entity, entities, _entityList, at);
        var coll = _entityList.Any(e =>
            e.GetType() != ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType);
        _entityList.Clear();
        return coll;
    }

    public static bool CollideCheckOutsideUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        if (entity.CollideCheckOutside<UpsideDownJumpThru>(at))
            return true;
        if (ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType != null && entity.CollideCheckOutside(ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType, at))
            return true;
        return false;
    }

    public static bool CollideCheckOutsideNotUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        foreach (Entity b in entity.Scene.Tracker.Entities[typeof(JumpThru)])
        {
            if (b is UpsideDownJumpThru || b.GetType() == ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType)
                continue;
            if (!Collide.Check(entity, b) && Collide.Check(entity, b, at))
                return true;
        }

        return false;
    }

    public static JumpThru CollideFirstOutsideUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        JumpThru collide = entity.CollideFirstOutside<UpsideDownJumpThru>(at);

        if (collide == null && ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType != null)
            collide = (JumpThru) entity.CollideFirstOutside(ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType, at);

        return collide;
    }

    public static JumpThru CollideFirstOutsideNotUpsideDownJumpThru(this Entity entity, Vector2 at)
    {
        foreach (Entity b in entity.Scene.Tracker.Entities[typeof(JumpThru)])
        {
            if (b is UpsideDownJumpThru || b.GetType() == ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType)
                continue;
            if (!Collide.Check(entity, b) && Collide.Check(entity, b, at))
                return b as JumpThru;
        }

        return null;
    }

    public static bool IsUpsideDownJumpThru(this JumpThru jumpThru)
    {
        if (jumpThru is UpsideDownJumpThru) return true;
        if (jumpThru.GetType() == ReflectionCache.MaddieHelpingHandUpsideDownJumpThruType) return true;
        return false;
    }

    #endregion

    private static IEnumerable<Entity> getEntities(Scene scene, Type type, Type[] notTypes = null, bool checkAllEntities = false)
    {
        IEnumerable<Entity> entities = null;
        if (checkAllEntities)
        {
            entities = scene.Tracker.Entities.SelectMany(p =>
                p.Value.Where(e => e.GetType() == type &&
                    (notTypes?.Contains(e.GetType()) ?? true)));
        }
        else if (scene.Tracker.Entities.ContainsKey(type))
        {
            entities = scene.Tracker.Entities[type];
            if (notTypes != null) entities = entities.Where(e => notTypes.Contains(e.GetType()));
        }

        return entities;
    }

    public static Rectangle ToRectangle(this Collider collider) =>
        new Rectangle((int)collider.Left, (int)collider.Top, (int)collider.Width, (int)collider.Height);

    public static void SetShouldInvert(this Entity entity, bool invert) =>
        DynamicData.For(entity).Data[GravityComponent.INVERTED_KEY] = invert;

    public static bool ShouldInvert(this Entity entity) =>
        DynamicData.For(entity).Data.TryGetValue(GravityComponent.INVERTED_KEY, out var value) && (bool)value;

    public static bool ShouldInvertChecked(this Entity entity)
    {
        if (entity is Player) return GravityHelperModule.ShouldInvertPlayerChecked;
        return entity.Get<GravityComponent>()?.ShouldInvertChecked ?? false;
    }

    public static bool SetGravity(this Entity entity, GravityType gravityType, float momentumMultiplier = 1f) =>
        entity?.Get<GravityComponent>()?.SetGravity(gravityType, momentumMultiplier) ?? false;

    public static GravityType GetGravity(this Entity entity) =>
        entity?.Get<GravityComponent>()?.CurrentGravity ?? GravityType.Normal;

    public static bool EnsureFallingSpeed(this Actor actor)
    {
        if (actor.Scene is not Level level)
            return false;

        if (actor is Player player)
            player.Speed.Y = Math.Max(Math.Abs(player.Speed.Y), 160f * (level.InSpace ? 0.6f : 1f));
        else if (actor is TheoCrystal theoCrystal)
            theoCrystal.Speed.Y = 200f;
        else if (actor is Glider glider)
            glider.Speed.Y = level.Wind.Y >= 0 ? 30f : 0f;
        else
            return false;

        return true;
    }
}
