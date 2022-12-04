// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    internal sealed class ILHookAttribute : BaseHookAttribute
    {
        public ILHookAttribute(
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            string[] before = null,
            string[] after = null,
            Type[] arguments = null)
            : base(targetMethodName, bindingFlags, before, after, arguments)
        {
        }

        public ILHookAttribute(
            Type targetType,
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            string[] before = null,
            string[] after = null,
            Type[] arguments = null)
            : base(targetType, targetMethodName, bindingFlags, before, after, arguments)
        {
        }

        public ILHookAttribute(
            string targetTypeName,
            string targetMethodName,
            BindingFlags bindingFlags = DEFAULT_FLAGS,
            string[] before = null,
            string[] after = null,
            Type[] arguments = null)
            : base(targetTypeName, targetMethodName, bindingFlags, before, after, arguments)
        {
        }

        protected override void DoLoad()
        {
            Detour = new ILHook(TargetMethod, manipulator);
        }

        private void manipulator(ILContext il) => HookUtils.SafeHook(() => HookMethod.Invoke(null, new object[] { il }));
    }
}
