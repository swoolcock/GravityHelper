// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class TrackerExtensions
    {
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
