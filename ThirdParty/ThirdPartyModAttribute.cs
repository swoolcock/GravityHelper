// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ThirdPartyModAttribute : Attribute
    {
        public string Name { get; }
        public string MinimumVersion { get; }
        public string MaximumVersion { get; }

        public ThirdPartyModAttribute(string name, string minimumVersion = null, string maximumVersion = null)
        {
            Name = name;
            MinimumVersion = minimumVersion;
            MaximumVersion = maximumVersion;
        }
    }
}
