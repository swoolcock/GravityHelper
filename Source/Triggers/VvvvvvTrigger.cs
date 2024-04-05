// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Triggers;

[Tracked]
[CustomEntity("GravityHelper/VvvvvvTrigger")]
public class VvvvvvTrigger : Trigger
{
    public bool Enable { get; }
    public bool OnlyOnSpawn { get; }
    public string EnableFlag { get; }

    public VvvvvvTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Enable = data.Bool("enable", true);
        OnlyOnSpawn = data.Bool("onlyOnSpawn");
        EnableFlag = data.Attr("enableFlag");
    }

    protected bool CheckFlag() =>
        string.IsNullOrWhiteSpace(EnableFlag) ||
        SceneAs<Level>() is { } level && level.Session.GetFlag(EnableFlag);

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Collidable = CheckFlag();
    }

    public override void Update()
    {
        Collidable = CheckFlag();
        base.Update();
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (OnlyOnSpawn) return;

        GravityHelperModule.Session.VvvvvvTrigger = Enable;

        if (Scene.GetPersistentController<VvvvvvGravityController>() is { } vvvvvvGravityController)
            vvvvvvGravityController.Transitioned();
    }
}
