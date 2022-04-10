// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class TrackerExtensions
    {
        public static TController GetController<TController>(this Tracker tracker, bool persistentFallback = true, TController exclude = default)
            where TController : BaseGravityController =>
            tracker.GetController(typeof(TController), persistentFallback, exclude) as TController;

        public static BaseGravityController GetController(this Tracker tracker, Type controllerType, bool persistentFallback = true, BaseGravityController exclude = default, string levelName = default)
        {
            if (!tracker.Entities.TryGetValue(controllerType, out var list))
                return default;
            BaseGravityController controller = default;
            // find the first non-persistent one
            controller = (BaseGravityController)list.FirstOrDefault(e => !((BaseGravityController)e).Persistent && e != exclude);
            // find the first persistent if we should
            if (persistentFallback)
                controller ??= (BaseGravityController)list.FirstOrDefault(e => ((BaseGravityController)e).Persistent && e != exclude);
            return controller;
        }

        public static TController GetController<TController>(this Level level, bool persistentFallback = true, TController exclude = default)
            where TController : BaseGravityController =>
            level.GetController(typeof(TController), persistentFallback, exclude) as TController;

        public static BaseGravityController GetController(this Level level, Type controllerType, bool persistentFallback = true, BaseGravityController exclude = default, string levelName = default)
        {
            var entities = level.Entities
                .Concat(level.Entities.ToAdd)
                .Where(e => e != exclude && e.GetType() == controllerType)
                .Cast<BaseGravityController>()
                .ToList();
            BaseGravityController controller = default;
            // find the first non-persistent one in the room bounds
            controller = entities.FirstOrDefault(e => !e.Persistent && level.IsInBounds(e));
            // fallback to the first persistent if we should
            if (persistentFallback)
                controller ??= entities.FirstOrDefault(e => e.Persistent);
            return controller;
        }

        public static T GetEntityOrDefault<T>(this Tracker tracker, Func<T, bool> predicate = null)
            where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return default;
            return predicate == null
                ? list.FirstOrDefault() as T
                : list.FirstOrDefault(e => predicate(e as T)) as T;
        }

        public static IEnumerable<Entity> GetEntitiesOrEmpty<T>(this Tracker tracker, Func<T, bool> predicate = null)
            where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return Enumerable.Empty<T>();
            return predicate == null ? list : list.Where(e => predicate(e as T));
        }

        public static IEnumerable<Entity> GetEntitiesOrEmpty(this Tracker tracker, Type entityType, Func<Entity, bool> predicate = null)
        {
            if (entityType == null || !tracker.Entities.TryGetValue(entityType, out var list))
                return Enumerable.Empty<Entity>();
            return predicate == null ? list : list.Where(predicate);
        }

        public static T GetComponentOrDefault<T>(this Tracker tracker, Func<T, bool> predicate = null)
            where T : Component
        {
            if (!tracker.Components.TryGetValue(typeof(T), out var list))
                return default;
            return predicate == null
                ? list.FirstOrDefault() as T
                : list.FirstOrDefault(c => predicate(c as T)) as T;
        }

        public static IEnumerable<Component> GetComponentsOrEmpty<T>(this Tracker tracker, Func<T, bool> predicate = null)
            where T : Component
        {
            if (!tracker.Components.TryGetValue(typeof(T), out var list))
                return Enumerable.Empty<T>();
            return predicate == null ? list : list.Where(e => predicate(e as T));
        }

        public static IEnumerable<Component> GetComponentsOrEmpty(this Tracker tracker, Type entityType, Func<Component, bool> predicate = null)
        {
            if (entityType == null || !tracker.Components.TryGetValue(entityType, out var list))
                return Enumerable.Empty<Component>();
            return predicate == null ? list : list.Where(predicate);
        }
    }
}
