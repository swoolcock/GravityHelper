// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;
using MonoMod.ModInterop;

#pragma warning disable CA2211

namespace Celeste.Mod.GravityHelper.ThirdParty;

[ModImportName("MotionSmoothing")]
public static class MotionSmoothingImports
{
    // [1.5.6+] Makes an entity that draws itself relative to Madeline's update-time position
    // follow her smoothed (and subpixel) rendered position instead.
    public static Action<Entity> TieToPlayer;
}
