// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.Hooks.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReflectTypeAttribute : Attribute
    {
        public string ModName { get; }
        public string TypeName { get; }

        public ReflectTypeAttribute(string modName, string typeName)
        {
            ModName = modName;
            TypeName = typeName;
        }
    }
}
