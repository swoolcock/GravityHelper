// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace Celeste.Mod.GravityHelper.ThirdParty;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
internal class ThirdPartyModAttribute : Attribute
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
