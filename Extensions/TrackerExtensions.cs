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
        public static T GetEntityOrDefault<T>(this Tracker tracker, Func<T, bool> predicate = null) where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return default;
            var ofType = list.OfType<T>();
            return predicate == null ? ofType.FirstOrDefault() : ofType.FirstOrDefault(predicate);
        }

        public static IEnumerable<T> GetEntitiesOrEmpty<T>(this Tracker tracker, Func<T, bool> predicate = null) where T : Entity
        {
            if (!tracker.Entities.TryGetValue(typeof(T), out var list))
                return Enumerable.Empty<T>();
            var ofType = list.OfType<T>();
            return predicate == null ? ofType : ofType.Where(predicate);
        }

        public static IEnumerable<Entity> GetEntitiesOrEmpty(this Tracker tracker, Type entityType, Func<Entity, bool> predicate = null)
        {
            if (entityType == null || !tracker.Entities.TryGetValue(entityType, out var list))
                return Enumerable.Empty<Entity>();
            return predicate == null ? list : list.Where(predicate);
        }
    }
}
