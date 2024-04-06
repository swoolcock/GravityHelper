// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers;

[Tracked]
[CustomEntity("GravityHelper/DisableUpTransitionController")]
public class DisableUpTransitionController : Entity
{
    public DisableUpTransitionController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
    }
}
