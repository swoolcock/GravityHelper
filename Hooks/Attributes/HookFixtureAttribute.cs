// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public class HookFixtureAttribute : Attribute
    {
        public Type TargetType { get; set; }
        public string TargetTypeName { get; set; }

        private static readonly List<HookFixtureAttribute> fixture_attributes = new List<HookFixtureAttribute>();
        private readonly List<BaseHookAttribute> _hookAttributes = new List<BaseHookAttribute>();
        private Type _fixtureType;

        public HookFixtureAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public HookFixtureAttribute(string targetTypeName)
        {
            TargetTypeName = targetTypeName;
        }

        public static void InitAll()
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Initialising all hook fixtures...");
            var allTypes = typeof(HookFixtureAttribute).Assembly.GetTypesSafe().Where(t => t.GetCustomAttribute<HookFixtureAttribute>() != null);
            foreach (var type in allTypes)
            {
                if (type.GetCustomAttribute<HookFixtureAttribute>() is { } hookFixtureAttribute)
                {
                    fixture_attributes.Add(hookFixtureAttribute.Init(type));
                }
            }
        }

        public HookFixtureAttribute Init(Type fixtureType)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Initialising hook fixture: {fixtureType.Name}");
            _fixtureType = fixtureType;
            var allMethods = fixtureType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in allMethods)
            {
                if (method.GetCustomAttribute<ILHookAttribute>() is { } ilHookAttribute)
                    _hookAttributes.Add(ilHookAttribute.Init(fixtureType, method));
                else if (method.GetCustomAttribute<OnHookAttribute>() is { } onHookAttribute)
                    _hookAttributes.Add(onHookAttribute.Init(fixtureType, method));
            }

            return this;
        }

        public static void LoadAll()
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Loading all hook fixtures...");
            foreach(var attr in fixture_attributes)
                attr.Load();
        }

        public void Load()
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Loading all hooks for fixture: {_fixtureType.Name}");
            foreach (var attr in _hookAttributes)
                attr.Load();
        }

        public static void UnloadAll()
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Unloading all hook fixtures...");
            foreach(var attr in fixture_attributes)
                attr.Unload();
        }

        public void Unload()
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Unloading all hooks for fixture: {_fixtureType.Name}");
            foreach (var attr in _hookAttributes)
                attr.Unload();
        }
    }
}
