// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using CassetteListener = Celeste.Mod.GravityHelper.Components.CassetteListener;

namespace Celeste.Mod.GravityHelper.Entities.Controllers;

[CustomEntity("GravityHelper/CassetteGravityController")]
[Tracked]
public class CassetteGravityController : BaseGravityController<CassetteGravityController>
{
    public GravityType[] CassetteSequence { get; }
    public float MomentumMultiplier { get; }
    public bool InstantFlip { get; }

    private readonly CassetteListener _cassetteListener;

    // ReSharper disable once UnusedMember.Global
    public CassetteGravityController()
    {
        // ephemeral controller
        CassetteSequence = Array.Empty<GravityType>();
        MomentumMultiplier = 1f;
    }

    // ReSharper disable once UnusedMember.Global
    public CassetteGravityController(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        // cassette sequence is a pipe or comma separated list
        // type,type,type,type
        var cassetteSequenceString = data.Attr("cassetteSequence").Replace("|", ",");
        CassetteSequence = cassetteSequenceString
            .Split(',')
            .Select(s =>
            {
                if (!int.TryParse(s, out var value)) return default;
                var type = (GravityType)value;
                return type >= GravityType.None && type <= GravityType.Toggle ? type : default;
            })
            .ToArray();

        MomentumMultiplier = data.Float("momentumMultiplier", 1f);
        InstantFlip = data.Bool("instantFlip", false);

        if (CassetteSequence.Length > 0)
        {
            Add(_cassetteListener = new CassetteListener
            {
                Enabled = false,
                WillToggle = (index, activate) =>
                {
                    // index could be out of range if more cassette blocks exist than are defined in the sequence
                    if (index >= CassetteSequence.Length || !activate) return;
                    var type = CassetteSequence[index];
                    var manager = Scene?.Tracker.GetEntity<CassetteBlockManager>();
                    var tempoMult = manager?.tempoMult ?? 1f;
                    var indicators = Scene?.Tracker.GetEntitiesOrEmpty<GravityIndicator>();
                    foreach (GravityIndicator indicator in indicators)
                    {
                        if (!indicator.SyncToPlayer)
                        {
                            indicator.TurnTarget = type;
                            indicator.TurnTime = tempoMult * 10f / 60f;
                        }
                    }
                },
                DidBecomeActive = index =>
                {
                    // index could be out of range if more cassette blocks exist than are defined in the sequence
                    if (index >= CassetteSequence.Length) return;
                    var type = CassetteSequence[index];
                    if (type != GravityType.None &&
                        GravityHelperModule.PlayerComponent is { } playerComponent &&
                        playerComponent.Entity is Player player &&
                        !player.IsIntroState)
                    {
                        playerComponent.SetGravity(type, MomentumMultiplier, instant: InstantFlip);
                    }
                },
            });
        }
    }

    public override void Transitioned()
    {
        if (!Persistent || Scene is not Level level) return;

        var active = ActiveController;

        foreach (CassetteGravityController controller in level.Tracker.GetEntitiesOrEmpty<CassetteGravityController>())
        {
            if (controller._cassetteListener != null)
                controller._cassetteListener.Enabled = controller == active;
        }

        if (active?._cassetteListener == null) return;

        // if we already have cassette blocks, don't reduce the beats
        level.CassetteBlockBeats = Math.Max(level.CassetteBlockBeats, active.CassetteSequence.Length);
        level.HasCassetteBlocks = true;

        var cbm = level.Entities.Concat(level.Entities.ToAdd).OfType<CassetteBlockManager>().FirstOrDefault();
        if (cbm == null)
        {
            level.Add(cbm = new CassetteBlockManager());
        }
    }
}
