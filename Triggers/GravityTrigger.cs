// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Triggers;

[CustomEntity("GravityHelper/GravityTrigger")]
public class GravityTrigger : Trigger
{
    private const float audio_muffle_seconds = 0.2f;

    public bool AffectsPlayer { get; }
    public bool AffectsHoldableActors { get; }
    public bool AffectsOtherActors { get; }

    public GravityType GravityType
    {
        get => _gravityType;
        protected set
        {
            _gravityType = value;
            if (Get<GravityTriggerComponent>() is { } gravityTriggerComponent)
                gravityTriggerComponent.GravityType = value;
        }
    }

    public GravityType ExitGravityType { get; }
    public float MomentumMultiplier { get; }
    public virtual bool ShouldAffectPlayer => true;
    protected bool AffectsNothing => !AffectsPlayer && !AffectsHoldableActors && !AffectsOtherActors;

    // ReSharper disable NotAccessedField.Local
    private readonly VersionInfo _modVersion;
    private readonly VersionInfo _pluginVersion;
    // ReSharper restore NotAccessedField.Local

    private readonly bool _defaultToController;
    private string _normalSound;
    private string _invertedSound;
    private string _toggleSound;
    private readonly string _forceSound;
    private string _exitSound;
    private float _audioMuffleSecondsRemaining;
    private GravityType _gravityType;

    private string sound => !string.IsNullOrWhiteSpace(_forceSound)
        ? _forceSound
        : GravityType switch
        {
            GravityType.Normal => _normalSound,
            GravityType.Inverted => _invertedSound,
            GravityType.Toggle => _toggleSound,
            _ => string.Empty,
        };

    public GravityTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        _modVersion = data.ModVersion();
        _pluginVersion = data.PluginVersion();

        AffectsPlayer = data.Bool("affectsPlayer", true);
        AffectsHoldableActors = data.Bool("affectsHoldableActors");
        AffectsOtherActors = data.Bool("affectsOtherActors");
        GravityType = (GravityType)data.Int("gravityType");
        ExitGravityType = (GravityType)data.Int("exitGravityType", (int)GravityType.None);
        MomentumMultiplier = data.Float("momentumMultiplier", 1f);

        _defaultToController = data.Bool("defaultToController", true);
        _forceSound = data.Attr("sound", string.Empty);

        TriggeredEntityTypes types = TriggeredEntityTypes.None;
        if (AffectsHoldableActors) types |= TriggeredEntityTypes.HoldableActors;
        if (AffectsOtherActors) types |= TriggeredEntityTypes.NonHoldableActors;

        if (GravityType != GravityType.None && (AffectsHoldableActors || AffectsOtherActors))
        {
            Add(new GravityTriggerComponent(types)
            {
                GravityType = GravityType,
                MomentumMultiplier = MomentumMultiplier,
            });
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (_defaultToController && Scene.GetActiveController<SoundGravityController>() is { } soundController)
        {
            if (GravityType == GravityType.Normal)
                _normalSound = soundController.NormalSound;
            else if (GravityType == GravityType.Inverted)
                _invertedSound = soundController.InvertedSound;
            else if (GravityType == GravityType.Toggle)
                _toggleSound = soundController.ToggleSound;

            if (ExitGravityType == GravityType.Normal)
                _exitSound = soundController.NormalSound;
            else if (ExitGravityType == GravityType.Inverted)
                _exitSound = soundController.InvertedSound;
            else if (ExitGravityType == GravityType.Toggle)
                _exitSound = soundController.ToggleSound;
        }
    }

    public override void Update()
    {
        base.Update();

        if (_audioMuffleSecondsRemaining > 0)
            _audioMuffleSecondsRemaining -= Engine.DeltaTime;
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        HandleOnEnter(player);
    }

    protected virtual void HandleOnEnter(Player player)
    {
        if (GravityType == GravityType.None || !AffectsPlayer || !ShouldAffectPlayer)
            return;

        if (GravityHelperModule.PlayerComponent is { } playerComponent)
        {
            var previousGravity = playerComponent.CurrentGravity;
            playerComponent.SetGravity(GravityType, MomentumMultiplier);
            if (!string.IsNullOrWhiteSpace(sound) && playerComponent.CurrentGravity != previousGravity && _audioMuffleSecondsRemaining <= 0)
            {
                Audio.Play(sound);
                _audioMuffleSecondsRemaining = audio_muffle_seconds;
            }
        }
    }

    public override void OnStay(Player player)
    {
        base.OnEnter(player);
        HandleOnStay(player);
    }

    protected virtual void HandleOnStay(Player player)
    {
        base.OnStay(player);

        if (!AffectsPlayer || !ShouldAffectPlayer ||
            GravityType == GravityType.None ||
            GravityType == GravityType.Toggle ||
            GravityType == GravityHelperModule.PlayerComponent?.CurrentGravity)
            return;

        GravityHelperModule.PlayerComponent?.SetGravity(GravityType, MomentumMultiplier);
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        HandleOnLeave(player);
    }

    protected virtual void HandleOnLeave(Player player)
    {
        if (ExitGravityType == GravityType.None || !AffectsPlayer || !ShouldAffectPlayer)
            return;

        if (GravityHelperModule.PlayerComponent is { } playerComponent)
        {
            var previousGravity = playerComponent.CurrentGravity;
            playerComponent.SetGravity(ExitGravityType, MomentumMultiplier);
            if (!string.IsNullOrWhiteSpace(_exitSound) && playerComponent.CurrentGravity != previousGravity && _audioMuffleSecondsRemaining <= 0)
            {
                Audio.Play(_exitSound);
                _audioMuffleSecondsRemaining = audio_muffle_seconds;
            }
        }
    }
}
