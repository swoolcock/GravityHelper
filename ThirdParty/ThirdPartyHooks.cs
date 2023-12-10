// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.ThirdParty;

internal static class ThirdPartyHooks
{
    public static IEnumerable<Type> ThirdPartyModTypes =>
        ReflectionCache.LoadableTypes.Where(t =>
            t.GetCustomAttribute<ThirdPartyModAttribute>() != null);

    public static readonly Dictionary<string, ThirdPartyModSupport> LoadedMods = new();
    public static readonly Dictionary<string, ThirdPartyModSupport> ForceLoadedMods = new();

    public static void Load(GravityHelperModule.HookLevel hookLevel)
    {
        Logger.Log(nameof(GravityHelperModule), "Loading third party hooks...");
        ReflectionCache.LoadThirdPartyTypes();

        foreach (var type in ThirdPartyModTypes)
        {
            // don't try to load if there's one force loaded
            if (ForceLoadedMods.Values.Any(type.IsInstanceOfType)) continue;

            if (Activator.CreateInstance(type) is ThirdPartyModSupport modSupport && modSupport.TryLoad(hookLevel))
            {
                LoadedMods[modSupport.Attribute.Name] = modSupport;
            }
        }
    }

    public static void ForceLoadType(Type type, GravityHelperModule.HookLevel hookLevel)
    {
        if (Activator.CreateInstance(type) is ThirdPartyModSupport modSupport && modSupport.TryLoad(hookLevel))
        {
            ForceLoadedMods[modSupport.Attribute.Name] = modSupport;
        }
    }

    public static void ForceUnloadType(Type type)
    {
        if (ForceLoadedMods.Values.FirstOrDefault(type.IsInstanceOfType) is { } modSupport && modSupport.TryUnload())
        {
            ForceLoadedMods.Remove(modSupport.Attribute.Name);
        }
    }

    // ReSharper disable once UnusedMember.Global
    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), "Unloading third party hooks...");
        var mods = LoadedMods.Values.ToArray();
        foreach (var mod in mods)
        {
            if (mod.TryUnload())
            {
                LoadedMods.Remove(mod.Attribute.Name);
            }
        }
    }
}