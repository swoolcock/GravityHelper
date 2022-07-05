// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class HookFixtureAttribute : Attribute
    {
        // ReSharper disable once InconsistentNaming
        public static readonly List<string> BlacklistedMods = new();

        private static readonly List<Type> _fixtureTypes = new List<Type>();
        private static readonly Dictionary<Type, List<IDetour>> _detours = new Dictionary<Type, List<IDetour>>();
        private static readonly Dictionary<Type, List<Tuple<EventInfo, Delegate>>> _delegates = new Dictionary<Type, List<Tuple<EventInfo, Delegate>>>();

        public string ModName { get; }

        public HookFixtureAttribute(string modName = "")
        {
            ModName = modName;
        }

        public static void LoadAll(bool thirdPartyMods = false)
        {
            var types = typeof(HookFixtureAttribute).Assembly.GetTypesSafe();
            var fixtureTypes = types.Where(t => t.GetCustomAttribute<HookFixtureAttribute>() != null);
            foreach (var type in fixtureTypes)
            {
                var attribute = type.GetCustomAttribute<HookFixtureAttribute>();
                var hasModName = !string.IsNullOrWhiteSpace(attribute.ModName);
                if (thirdPartyMods != hasModName) continue;

                if (thirdPartyMods)
                {
                    var modExists = Everest.Modules.Any(m => m.Metadata.Name == attribute.ModName);
                    // skip if mod not loaded
                    if (!modExists)
                        continue;

                    // skip and log if mod blacklisted
                    if (BlacklistedMods.Contains(attribute.ModName))
                    {
                        Logger.Log(nameof(GravityHelperModule), $"Skipping hooks for blacklisted mod: {attribute.ModName}");
                        continue;
                    }

                    Logger.Log(nameof(GravityHelperModule), $"Loading hooks for mod: {attribute.ModName}");
                }

                attribute.Load(type);
                _fixtureTypes.Add(type);
            }
        }

        public static void UnloadAll(bool thirdPartyMods = false)
        {
            foreach (var type in _fixtureTypes)
            {
                var attribute = type.GetCustomAttribute<HookFixtureAttribute>();
                var hasModName = !string.IsNullOrWhiteSpace(attribute.ModName);
                if (thirdPartyMods == hasModName)
                    Unload(type);
            }

            _detours.Clear();
            _delegates.Clear();
            _fixtureTypes.Clear();
        }

        internal void Load(Type fixtureType)
        {
            // execute Load
            var loadMethod = fixtureType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public);
            loadMethod?.Invoke(null, new object[0]);

            // load types
            var fields = fixtureType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var typeFields = fields.Where(f => f.GetCustomAttribute<ReflectTypeAttribute>() != null);
            var properties = fixtureType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var typeProperties = properties.Where(f => f.GetCustomAttribute<ReflectTypeAttribute>() != null);

            foreach (var typeField in typeFields)
            {
                var attribute = typeField.GetCustomAttribute<ReflectTypeAttribute>();
                var targetType = ReflectionCache.GetModdedTypeByName(attribute.ModName, attribute.TypeName);
                if (targetType != null) typeField.SetValue(null, targetType);
            }

            foreach (var typeProperty in typeProperties)
            {
                var attribute = typeProperty.GetCustomAttribute<ReflectTypeAttribute>();
                var targetType = ReflectionCache.GetModdedTypeByName(attribute.ModName, attribute.TypeName);
                if (targetType != null) typeProperty.SetValue(null, targetType);
            }

            // load methods
            var methods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var hookMethodMethods = methods.Where(t => t.GetCustomAttribute<HookMethodAttribute>() != null);

            // create detours and event handlers
            var detours = new List<IDetour>();
            var delegates = new List<Tuple<EventInfo, Delegate>>();

            foreach (var hookMethod in hookMethodMethods)
            {
                var attribute = hookMethod.GetCustomAttribute<HookMethodAttribute>();
                var targetType = attribute.TargetType;
                targetType ??= string.IsNullOrWhiteSpace(ModName)
                    ? typeof(Celeste).Assembly.GetType(attribute.TargetTypeName)
                    : ReflectionCache.GetModdedTypeByName(ModName, attribute.TargetTypeName);
                if (targetType == null) continue;

                void processHookMethod()
                {
                    // handle events
                    var matchingEvent = targetType.GetEvent(targetType.Name, BindingFlags.Public | BindingFlags.Static);
                    if (matchingEvent != null)
                    {
                        var del = hookMethod.CreateDelegate(matchingEvent.EventHandlerType);
                        delegates.Add(new Tuple<EventInfo, Delegate>(matchingEvent, del));
                        matchingEvent.AddEventHandler(null, del);
                        return;
                    }

                    // handle detours
                    MethodBase targetMethod;
                    if (attribute.TargetMethod == "ctor")
                        targetMethod = targetType.GetConstructor(attribute.Types ?? Type.EmptyTypes);
                    else
                    {
                        var matchingMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(m => m.Name == attribute.TargetMethod);
                        targetMethod = matchingMethods.FirstOrDefault(m =>
                            attribute.Types == null || m.GetParameters().Select(p =>
                                p.ParameterType).SequenceEqual(attribute.Types));
                    }

                    if (targetMethod == null) return;

                    if (targetMethod is MethodInfo methodInfo && methodInfo.ReturnType == typeof(IEnumerator))
                        targetMethod = methodInfo.GetStateMachineTarget();

                    var parameters = hookMethod.GetParameters();
                    IDetour detour = null;
                    if (parameters.FirstOrDefault()?.ParameterType == typeof(ILContext))
                        detour = new ILHook(targetMethod, hookMethod.CreateDelegate<ILContext.Manipulator>());
                    else
                        detour = new Hook(targetMethod, hookMethod);

                    detours.Add(detour);
                }

                DetourContext context = null;
                if (attribute.Before.Any() || attribute.After.Any())
                {
                    context = new DetourContext();
                    if (attribute.Before.Any())
                        context.Before = attribute.Before;
                    if (attribute.After.Any())
                        context.After = attribute.After;
                }

                if (context != null)
                    using (context)
                        processHookMethod();
                else
                    processHookMethod();
            }

            _delegates[fixtureType] = delegates;
            _detours[fixtureType] = detours;
        }

        internal static void Unload(Type fixtureType)
        {
            if (_detours.TryGetValue(fixtureType, out var detours))
            {
                foreach (var detour in detours)
                    detour.Dispose();
            }
            _detours.Remove(fixtureType);

            if (_delegates.TryGetValue(fixtureType, out var delegates))
            {
                foreach (var del in delegates)
                    del.Item1.RemoveEventHandler(null, del.Item2);
            }
            _delegates.Remove(fixtureType);

            // execute Unload
            var unloadMethod = fixtureType.GetMethod("Unload", BindingFlags.Static | BindingFlags.Public);
            unloadMethod?.Invoke(null, new object[0]);
        }
    }
}
