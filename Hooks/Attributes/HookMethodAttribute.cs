// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

        public HookMethodAttribute(string targetTypeName, string targetMethod)
        {
            TargetTypeName = targetTypeName;
            TargetMethod = targetMethod;
        }

        public HookMethodAttribute(Type targetType, string targetMethod)
        {
            TargetType = targetType;
            TargetMethod = targetMethod;
        }
    }
}
