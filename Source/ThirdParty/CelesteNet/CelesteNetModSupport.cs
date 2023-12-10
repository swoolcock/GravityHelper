// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.GravityHelper.ThirdParty.CelesteNet;

[ThirdPartyMod("CelesteNet.Client")]
internal class CelesteNetModSupport : ThirdPartyModSupport
{
    public const int PROTOCOL_VERSION = 1;

    private const float name_tag_offset = 4f;
    private const float emote_offset = name_tag_offset + 6;

    private CelesteNetGravityComponent _gravityComponent;

    // ReSharper disable InconsistentNaming
    private IDetour hook_GhostNameTag_Render;
    private IDetour hook_GhostEmote_Update;
    // ReSharper restore InconsistentNaming

    protected override void Load(GravityHelperModule.HookLevel hookLevel)
    {
        Celeste.Instance.Components.Add(_gravityComponent = new CelesteNetGravityComponent(Celeste.Instance));
        On.Celeste.Player.Update += Player_Update;

        var cngntt = ReflectionCache.CelesteNetGhostNameTagType;
        var cngntRenderMethod = cngntt?.GetMethod("Render", BindingFlags.Instance | BindingFlags.Public);
        if (cngntRenderMethod != null)
        {
            hook_GhostNameTag_Render = new ILHook(cngntRenderMethod, GhostNameTag_Render);
        }

        var cnget = ReflectionCache.CelesteNetGhostEmoteType;
        var cngeUpdateMethod = cnget?.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
        if (cngeUpdateMethod != null)
        {
            hook_GhostEmote_Update = new ILHook(cngeUpdateMethod, GhostEmote_Update);
        }
    }

    protected override void Unload()
    {
        Celeste.Instance.Components.Remove(_gravityComponent);
        _gravityComponent.Dispose();
        _gravityComponent = null;

        On.Celeste.Player.Update -= Player_Update;

        hook_GhostNameTag_Render?.Dispose();
        hook_GhostNameTag_Render = null;
        hook_GhostEmote_Update?.Dispose();
        hook_GhostEmote_Update = null;
    }

    private void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);
        _gravityComponent?.SendPlayerGravity(GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);
    }

    private void GhostNameTag_Render(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(16)))
            throw new HookException("Couldn't find 16");

        var trackingFieldInfo = ReflectionCache.CelesteNetGhostNameTagType.GetField("Tracking", BindingFlags.Public | BindingFlags.Instance);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, Entity, float>>((v, e) =>
        {
            var tracking = trackingFieldInfo.GetValue(e) as Entity;
            return tracking.ShouldInvert() ? name_tag_offset : v;
        });
    });

    private void GhostEmote_Update(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        cursor.Goto(cursor.Instrs.Count);

        if (!cursor.TryGotoPrev(instr => instr.MatchSub()))
            throw new HookException("Couldn't find Position.Y -=");

        var trackingFieldInfo = ReflectionCache.CelesteNetGhostEmoteType.GetField("Tracking", BindingFlags.Public | BindingFlags.Instance);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, Entity, float>>((v, e) =>
        {
            var tracking = trackingFieldInfo.GetValue(e) as Entity;
            return tracking.ShouldInvert() ? emote_offset : v;
        });
    });
}