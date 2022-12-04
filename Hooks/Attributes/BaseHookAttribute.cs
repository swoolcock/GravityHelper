// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    internal abstract class BaseHookAttribute : Attribute
    {
        public Type TargetType { get; set; }
        public string TargetTypeName { get; set; }
        public string TargetMethodName { get; set; }
        public Type[] TargetMethodArguments { get; set; }
        public BindingFlags BindingFlags { get; set; }

        protected List<string> Before = new List<string>();
        protected List<string> After = new List<string>();

        protected const BindingFlags DEFAULT_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        protected IDetour Detour;
        protected MethodInfo TargetMethod;
        protected MethodInfo HookMethod;

        protected BaseHookAttribute(
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            IEnumerable<string> before = null,
            IEnumerable<string> after = null,
            Type[] arguments = null)
        {
            TargetMethodName = targetMethodName;
            TargetMethodArguments = arguments;
            BindingFlags = bindingFlags;
            if (before != null)
                Before.AddRange(before);
            if (after != null)
                After.AddRange(after);
        }

        protected BaseHookAttribute(
            Type targetType,
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            IEnumerable<string> before = null,
            IEnumerable<string> after = null,
            Type[] arguments = null)
            : this(targetMethodName, bindingFlags, before, after, arguments)
        {
            TargetType = targetType;
        }

        protected BaseHookAttribute(
            string targetTypeName,
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            IEnumerable<string> before = null,
            IEnumerable<string> after = null,
            Type[] arguments = null)
            : this(targetMethodName, bindingFlags, before, after, arguments)
        {
            TargetTypeName = targetTypeName;
        }

        public BaseHookAttribute Init(Type fixtureType, MethodInfo hookMethod)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Initialising hook method: {fixtureType.Name}.{hookMethod.Name}");

            var fixtureAttribute = (HookFixtureAttribute)fixtureType.GetCustomAttribute(typeof(HookFixtureAttribute));
            if (TargetType == null && !string.IsNullOrWhiteSpace(TargetTypeName))
            {
                try
                {
                    TargetType = Type.GetType(TargetTypeName);
                }
                catch (Exception)
                {
                    Logger.Log(LogLevel.Error, nameof(GravityHelperModule), $"Couldn't find target type: {TargetTypeName}");
                    return null;
                }
            }

            TargetType ??= fixtureAttribute.TargetType;

            TargetTypeName = TargetType.Name;

            HookMethod = hookMethod;
            if (TargetMethod == null && !string.IsNullOrWhiteSpace(TargetMethodName))
            {
                var methods = TargetType.GetMethods(BindingFlags).Where(m => m.Name == TargetMethodName);
                TargetMethod = TargetMethodArguments == null
                    ? methods.FirstOrDefault()
                    : methods.FirstOrDefault(m => m.GetParameters()
                            .Select(p => p.ParameterType)
                            .SequenceEqual(TargetMethodArguments));
            }

            if (TargetMethod == null)
            {
                Logger.Log(LogLevel.Error, nameof(GravityHelperModule), $"Couldn't find target method: {TargetMethodName}");
                return null;
            }

            TargetMethodName = TargetMethod.Name;

            if (TargetMethod.ReturnType == typeof(IEnumerator))
                TargetMethod = TargetMethod.GetStateMachineTarget();

            return this;
        }

        public virtual void Load()
        {
            if (Detour != null) return;

            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Loading hook: {TargetTypeName}.{TargetMethodName}");

            if (!Before.Any() && !After.Any())
            {
                DoLoad();
                return;
            }

            using (var context = new DetourContext())
            {
                if (Before.Any())
                    context.Before.AddRange(Before);
                if (After.Any())
                    context.After.AddRange(After);
                DoLoad();
            }
        }

        public virtual void Unload()
        {
            if (Detour == null) return;
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Unloading hook: {TargetTypeName}.{TargetMethodName}");
            DoUnload();
        }

        protected abstract void DoLoad();

        protected virtual void DoUnload()
        {
            Detour?.Dispose();
            Detour = null;
        }
    }
}
