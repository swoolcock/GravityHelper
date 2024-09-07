// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers;

[CustomEntity("GravityHelper/VvvvvvGravityController")]
[Tracked]
public class VvvvvvGravityController : BaseGravityController<VvvvvvGravityController>
{
    public VvvvvvMode Mode { get; set; } = VvvvvvMode.Off;
    public string FlipSound { get; } = default_flip_sound;
    public bool DisableGrab { get; } = true;
    public bool DisableDash { get; } = true;
    public bool DisableWallJump { get; } = true;
    public VvvvvvJumpBehavior SolidTilesBehavior { get; } = VvvvvvJumpBehavior.Flip;
    public VvvvvvJumpBehavior OtherPlatformBehavior { get; } = VvvvvvJumpBehavior.Flip;

    public bool IsVvvvvv => GravityHelperModule.Settings.VvvvvvMode == GravityHelperModuleSettings.VvvvvvSetting.Default
        ? Mode == VvvvvvMode.TriggerBased && GravityHelperModule.Session.VvvvvvTrigger || Mode == VvvvvvMode.On
        : GravityHelperModule.Settings.VvvvvvMode == GravityHelperModuleSettings.VvvvvvSetting.Enabled;

    public bool IsDisableDash => GravityHelperModule.Settings.VvvvvvAllowDashing == GravityHelperModuleSettings.VvvvvvSetting.Default
        ? DisableDash
        : GravityHelperModule.Settings.VvvvvvAllowDashing != GravityHelperModuleSettings.VvvvvvSetting.Enabled;

    private bool _dashDisabled;
    private float _bufferTimeRemaining;

    private const float flip_buffer_seconds = 0.1f;
    private const string default_flip_sound = "event:/gravityhelper/toggle";

    // ReSharper disable once UnusedMember.Global
    public VvvvvvGravityController()
    {
        // ephemeral controller
    }

    // ReSharper disable once UnusedMember.Global
    public VvvvvvGravityController(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Mode = data.Enum("mode", Mode);
        FlipSound = data.Attr("flipSound", FlipSound);
        DisableGrab = data.Bool("disableGrab", DisableGrab);
        DisableDash = data.Bool("disableDash", DisableDash);
        DisableWallJump = data.Bool("disableWallJump", DisableWallJump);
        SolidTilesBehavior = data.Enum("solidTilesBehavior", SolidTilesBehavior);
        OtherPlatformBehavior = data.Enum("otherPlatformBehavior", OtherPlatformBehavior);
    }

    public override void Transitioned()
    {
        if (!Persistent) return;

        var active = ActiveController;
        var session = GravityHelperModule.Session;
        var settings = GravityHelperModule.Settings;

        session.VvvvvvMode = settings.VvvvvvMode switch
        {
            GravityHelperModuleSettings.VvvvvvSetting.Enabled => VvvvvvMode.On,
            GravityHelperModuleSettings.VvvvvvSetting.Disabled => VvvvvvMode.Off,
            _ => active.Mode,
        };

        session.DisableGrab = settings.VvvvvvAllowGrabbing switch
        {
            GravityHelperModuleSettings.VvvvvvSetting.Enabled => false,
            GravityHelperModuleSettings.VvvvvvSetting.Disabled => active.IsVvvvvv,
            _ => active.DisableGrab,
        };
    }

    public void CheckJump(Player player)
    {
        if (!Persistent) return;

        var active = ActiveController;
        if (!active.IsVvvvvv) return;

        // set jump buffer, we'll check it afterwards
        if (Input.Jump.Pressed)
            _bufferTimeRemaining = flip_buffer_seconds;

        // if not on the ground and disable wall jump is true, consume the jump
        if (active.DisableWallJump && !player.OnGround())
            Input.Jump.ConsumePress();
    }

    public void TryFlip(Player player)
    {
        // bail if no jump has been buffered or if we're not on the ground
        if (_bufferTimeRemaining <= 0 || !player.OnGround()) return;

        // bail if no active controller somehow
        if (ActiveController is not { } active) return;

        // if both behaviours are the same then we don't need to check the ground type
        VvvvvvJumpBehavior behavior = active.SolidTilesBehavior;
        if (active.SolidTilesBehavior != active.OtherPlatformBehavior)
        {
            // find out what type of ground we're on
            var checkPos = player.Position + (GravityHelperModule.ShouldInvertPlayer ? -Vector2.UnitY : Vector2.UnitY);
            var onSolidTiles = player.CollideCheck<SolidTiles>(checkPos);
            var onOtherPlatform = player.CollideCheck<Platform, SolidTiles>(checkPos);

            // priority is Flip > Jump > None
            if (onSolidTiles && active.SolidTilesBehavior == VvvvvvJumpBehavior.Flip ||
                onOtherPlatform && active.OtherPlatformBehavior == VvvvvvJumpBehavior.Flip)
                behavior = VvvvvvJumpBehavior.Flip;
            else if (onSolidTiles && active.SolidTilesBehavior == VvvvvvJumpBehavior.Jump ||
                     onOtherPlatform && active.OtherPlatformBehavior == VvvvvvJumpBehavior.Jump)
                behavior = VvvvvvJumpBehavior.Jump;
            else
                behavior = VvvvvvJumpBehavior.None;
        }

        // jump should bail here
        if (behavior == VvvvvvJumpBehavior.Jump) return;

        // prevent other jumps from happening
        Input.Jump.ConsumePress();

        // no jump or flip should occur so just bail
        if (behavior == VvvvvvJumpBehavior.None) return;

        // for now we'll only allow normal state
        if (player.StateMachine != Player.StNormal) return;

        // never allow from dream jumps
        if (player.dreamJump) return;

        // on ground or within coyote frames
        var onGround = player.onGround || player.jumpGraceTimer > 0;

        // if we're not on ground, see if we have any Extended Variant Jumps(TM)
        int extVarJumps = 0;
        if (!onGround && ReflectionCache.ExtendedVariantsJumpCountGetJumpBufferMethodInfo != null)
            extVarJumps = (int)ReflectionCache.ExtendedVariantsJumpCountGetJumpBufferMethodInfo.Invoke(null, Array.Empty<object>());

        // consume an Extended Variant Jump(TM) if we can
        if (extVarJumps > 0)
            ReflectionCache.ExtendedVariantsJumpCountSetJumpCountMethodInfo?.Invoke(null, new object[] { extVarJumps - 1, false });
        // otherwise bail if we're not on ground
        else if (!onGround)
            return;

        // reset autoflip
        _bufferTimeRemaining = -1f;

        // flip the player
        GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Toggle, instant: true);

        // play a sound if we should
        var flipSound = active.FlipSound;
        if (GravityHelperModule.Settings.VvvvvvFlipSound == GravityHelperModuleSettings.VvvvvvSetting.Enabled)
            flipSound = string.IsNullOrWhiteSpace(flipSound) ? default_flip_sound : flipSound;
        else if (GravityHelperModule.Settings.VvvvvvFlipSound == GravityHelperModuleSettings.VvvvvvSetting.Disabled)
            flipSound = string.Empty;
        if (!string.IsNullOrEmpty(flipSound))
            Audio.Play(flipSound);
    }

    public override void Update()
    {
        base.Update();
        if (!Persistent) return;

        var active = ActiveController;
        var isVvvvvv = active.IsVvvvvv;
        var disableDash = active.IsDisableDash;

        if (_dashDisabled != (isVvvvvv && disableDash))
        {
            _dashDisabled = isVvvvvv && disableDash;
            updateInventory();
        }

        if (_bufferTimeRemaining > 0)
            _bufferTimeRemaining -= Engine.DeltaTime;
    }

    private void updateInventory()
    {
        if (Scene is not Level level || level.Tracker.GetEntity<Player>() is not { } player)
            return;

        if (_dashDisabled)
        {
            level.Session.Inventory = new PlayerInventory(0);
            player.Dashes = 0;
        }
        else
        {
            var inv = PlayerInventory.Default;
            var invName = level.Session.MapData?.Meta?.Inventory ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(invName) && typeof(PlayerInventory).GetField(invName, BindingFlags.Public | BindingFlags.Static) is { } fieldInfo)
                inv = (PlayerInventory)fieldInfo.GetValue(null);

            level.Session.Inventory = inv;
            player.Dashes = player.MaxDashes;
        }
    }

    public enum VvvvvvJumpBehavior
    {
        None,
        Jump,
        Flip,
    }
}
