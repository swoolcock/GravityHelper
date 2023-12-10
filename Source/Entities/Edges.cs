// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.Entities;

[Flags]
public enum Edges
{
    None = 0,

    Top = 1 << 0,
    Bottom = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,

    Vertical = Top | Bottom,
    Horizontal = Left | Right,

    All = Vertical | Horizontal,
}