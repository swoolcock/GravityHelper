// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.ThirdParty;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

// ReSharper disable UnusedMember.Global

namespace Celeste.Mod.GravityHelper;

[ModExportName("GravityHelper")]
public static class GravityHelperExports
{
    public static void RegisterModSupportBlacklist(string modName) => ThirdPartyModSupport.BlacklistedMods.Add(modName);

    public static string GravityTypeFromInt(int gravityType) => ((GravityType)gravityType).ToString();

    public static int GravityTypeToInt(string name) =>
        (int)(Enum.TryParse<GravityType>(name, out var value) ? value : GravityType.Normal);

    public static int GetPlayerGravity() =>
        (int)(GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

    public static int GetActorGravity(Actor actor) => (int)(actor?.GetGravity() ?? GravityType.Normal);

    // TODO: make this support instant
    public static void SetPlayerGravity(int gravityType, float momentumMultiplier) =>
        GravityHelperModule.PlayerComponent?.SetGravity((GravityType)gravityType, momentumMultiplier);

    // TODO: make this support instant
    public static void SetActorGravity(Actor actor, int gravityType, float momentumMultiplier) =>
        actor?.SetGravity((GravityType)gravityType, momentumMultiplier);

    public static bool IsPlayerInverted() => GravityHelperModule.ShouldInvertPlayer;

    public static bool IsActorInverted(Actor actor) => actor?.ShouldInvert() ?? false;

    public static Vector2 GetAboveVector(Actor actor) =>
        actor?.ShouldInvert() == true ? Vector2.UnitY : -Vector2.UnitY;

    public static Vector2 GetBelowVector(Actor actor) =>
        actor?.ShouldInvert() == true ? -Vector2.UnitY : Vector2.UnitY;

    public static Vector2 GetTopCenter(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.BottomCenter : actor?.TopCenter ?? Vector2.Zero;

    public static Vector2 GetBottomCenter(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.TopCenter : actor?.BottomCenter ?? Vector2.Zero;

    public static Vector2 GetTopLeft(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.BottomLeft : actor?.TopLeft ?? Vector2.Zero;

    public static Vector2 GetBottomLeft(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.TopLeft : actor?.BottomLeft ?? Vector2.Zero;

    public static Vector2 GetTopRight(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.BottomRight : actor?.TopRight ?? Vector2.Zero;

    public static Vector2 GetBottomRight(Actor actor) =>
        actor?.ShouldInvert() == true ? actor.TopRight : actor?.BottomRight ?? Vector2.Zero;

    // TODO: make this support instant
    public static Component CreateGravityListener(Actor actor, Action<Entity, int, float> gravityChanged) =>
        new GravityListener(actor, (e, a) =>
            gravityChanged(e, (int)a.NewValue, a.MomentumMultiplier));

    // TODO: make this support instant
    public static Component CreatePlayerGravityListener(Action<Player, int, float> gravityChanged) =>
        new PlayerGravityListener((e, a) =>
            gravityChanged(e as Player, (int)a.NewValue, a.MomentumMultiplier));

    public static void BeginOverride() => GravityHelperModule.OverrideSemaphore++;

    public static void EndOverride() => GravityHelperModule.OverrideSemaphore--;

    public static void ExecuteOverride(Action action)
    {
        GravityHelperModule.OverrideSemaphore++;
        action?.Invoke();
        GravityHelperModule.OverrideSemaphore--;
    }

    public static IDisposable WithOverride()
    {
        GravityHelperModule.OverrideSemaphore++;
        return new InvokeOnDispose(() => GravityHelperModule.OverrideSemaphore--);
    }
}
