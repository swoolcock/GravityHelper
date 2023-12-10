// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions;

internal static class TrackerExtensions
{
    public static TController GetActiveController<TController>(this Scene scene, bool allowEphemeral = false)
        where TController : BaseGravityController =>
        GetActiveController(scene, typeof(TController), allowEphemeral) as TController;

    public static BaseGravityController GetActiveController(this Scene scene, Type controllerType, bool allowEphemeral = false)
    {
        if (scene == null || controllerType == null) return null;
        if (!scene.Tracker.Entities.TryGetValue(controllerType, out var list) || !list.Any()) return null;
        var level = scene as Level;

        BaseGravityController global = null;
        foreach (var item in list)
        {
            if (item is not BaseGravityController controller)
                continue;
            if (controller.Persistent && (!controller.Ephemeral || allowEphemeral))
                global = controller;
            if (level?.IsInBounds(item) ?? false)
                return controller;
        }
        return global;
    }

    public static TController GetPersistentController<TController>(this Scene scene, bool createIfRequired = false)
        where TController : BaseGravityController =>
        GetPersistentController(scene, typeof(TController), createIfRequired) as TController;

    public static BaseGravityController GetPersistentController(this Scene scene, Type controllerType, bool createIfRequired = false)
    {
        if (!scene.Tracker.Entities.TryGetValue(controllerType, out var list) && !createIfRequired)
            return null;

        // find the persistent controller if it exists
        var persistent = list?.FirstOrDefault(e => (e as BaseGravityController)?.Persistent == true) as BaseGravityController;
        // if we found a persistent, just return it
        if (persistent != null) return persistent;
        // if we have at least one non-persistent, or if we've been told to create one, create an ephemeral persistent
        if (createIfRequired || list?.Any() == true)
        {
            scene.Add(persistent = (BaseGravityController)Activator.CreateInstance(controllerType));
            scene.Entities.UpdateLists();
        }
        // return the new one, or null
        return persistent;
    }

    // ReSharper disable once UnusedMember.Global
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

    // ReSharper disable once UnusedMember.Global
    public static T GetComponentOrDefault<T>(this Tracker tracker, Func<T, bool> predicate = null)
        where T : Component
    {
        if (!tracker.Components.TryGetValue(typeof(T), out var list))
            return default;
        return predicate == null
            ? list.FirstOrDefault() as T
            : list.FirstOrDefault(c => predicate(c as T)) as T;
    }

    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<Component> GetComponentsOrEmpty<T>(this Tracker tracker, Func<T, bool> predicate = null)
        where T : Component
    {
        if (!tracker.Components.TryGetValue(typeof(T), out var list))
            return Enumerable.Empty<T>();
        return predicate == null ? list : list.Where(e => predicate(e as T));
    }

    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<Component> GetComponentsOrEmpty(this Tracker tracker, Type entityType, Func<Component, bool> predicate = null)
    {
        if (entityType == null || !tracker.Components.TryGetValue(entityType, out var list))
            return Enumerable.Empty<Component>();
        return predicate == null ? list : list.Where(predicate);
    }
}
