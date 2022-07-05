// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class HookMethodAttribute : Attribute
    {
        public Type TargetType { get; }
        public string TargetTypeName { get; }
        public string TargetMethod { get; }
        public List<string> Before { get; set; } = new List<string>();
        public List<string> After { get; set; } = new List<string>();
        public Type[] Types { get; }
        public int ArgCount { get; }

        public HookMethodAttribute(string targetTypeName, string targetMethod, int argCount = -1, Type[] types = null)
        {
            TargetTypeName = targetTypeName;
            TargetMethod = targetMethod;
            Types = types;
            ArgCount = argCount;
        }

        public HookMethodAttribute(Type targetType, string targetMethod, int argCount = -1, Type[] types = null)
        {
            TargetType = targetType;
            TargetMethod = targetMethod;
            Types = types;
            ArgCount = argCount;
        }
    }
}
