// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Entities;

public interface IConnectableField
{
    public Color FieldColor { get; }
    public bool ShouldDrawField { get; }
}