// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.ThirdParty;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.ModInterop;

// ReSharper disable UnusedMember.Global

namespace Celeste.Mod.GravityHelper;

internal static class GravityHelperAPI
{
    [ModExportName("GravityHelper")]
    internal static class Exports
    {
        public static void RegisterModSupportBlacklist(string modName) =>
            ThirdPartyModSupport.BlacklistedMods.Add(modName);

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

        public static Vector2 TransformVector(Vector2 vec)
        {
            if (!GravityHelperModule.ShouldInvertPlayer || IsControlSchemeRelative())
                return vec;
            return new Vector2(vec.X, -vec.Y);
        }

        public static Vector2 TransformFeatherVector(Vector2 vec)
        {
            if (!GravityHelperModule.ShouldInvertPlayer || IsFeatherControlSchemeRelative())
                return vec;
            return new Vector2(vec.X, -vec.Y);
        }

        public static Vector2 TransformVectorForActor(Vector2 vec, Actor actor)
        {
            if (actor is Player) return TransformVector(vec);
            return new Vector2(vec.X, actor.ShouldInvert() ? -vec.Y : vec.Y);
        }

        public static bool IsControlSchemeRelative() => GravityHelperModule.Settings.ControlScheme ==
                                                        GravityHelperModuleSettings.ControlSchemeSetting.Relative;

        public static bool IsFeatherControlSchemeRelative() => GravityHelperModule.Settings.FeatherControlScheme ==
                                                               GravityHelperModuleSettings.ControlSchemeSetting.Relative;

        /// <summary>
        /// Returns the current number of GravityRefill charges.
        /// </summary>
        public static int GetGravityCharges() => GravityHelperModule.PlayerComponent?.GravityCharges ?? 0;

        /// <summary>
        /// Sets the GravityRefill charge count to the provided amount,
        /// and prevents consuming charges for the rest of the frame.
        /// This is so that dashing over the top of an entity that provides charges will refund the charge.
        /// </summary>
        public static void RefillGravityCharges(int charges) => GravityHelperModule.PlayerComponent?.RefillGravityCharges(charges);

        /// <summary>
        /// Adds the specified number of charges (or removes if negative value passed).
        /// Clamped to [0, int.MaxValue] and ignores the "refilled this frame" check.
        /// </summary>
        public static void AdjustGravityCharges(int chargesToGive) =>
            GravityHelperModule.PlayerComponent?.AdjustGravityCharges(chargesToGive);

        /// <summary>
        /// Decreases the GravityRefill charge count by the specified amount, to a minimum of 0.
        /// Does nothing if <see cref="RefillGravityCharges"/> was called previously this frame
        /// (either by the exported API, or internally in Gravity Helper).
        /// </summary>
        public static void ConsumeGravityCharges(int chargesToConsume) =>
            GravityHelperModule.PlayerComponent?.ConsumeGravityCharges(chargesToConsume);

        public static TalkComponent.TalkComponentUI CreateUpsideDownTalkComponentUI(TalkComponent talkComponent) =>
            new UpsideDownTalkComponentUI(talkComponent);

        // TODO: make this support instant
        public static Component CreateGravityListener(Actor actor, Action<Entity, int, float> gravityChanged) =>
            new GravityListener(actor, (e, a) =>
                gravityChanged(e, (int)a.NewValue, a.MomentumMultiplier));

        // TODO: make this support instant
        public static Component CreatePlayerGravityListener(Action<Player, int, float> gravityChanged) =>
            new PlayerGravityListener((e, a) =>
                gravityChanged(e as Player, (int)a.NewValue, a.MomentumMultiplier));

        public static void BeginForceInvertPlayerRender() => GravityHelperModule.ForceInvertPlayerRenderSemaphore++;
        public static void EndForceInvertPlayerRender() => GravityHelperModule.ForceInvertPlayerRenderSemaphore--;

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

        public static void SetHoldableResetTime(Holdable holdable, float resetTime)
        {
            if (holdable?.Entity.Get<GravityHoldable>() is { } gravityHoldable)
            {
                gravityHoldable.ResetTime = resetTime;
            }
        }

        public static void SetHoldableResetType(Holdable holdable, int gravityType)
        {
            if (holdable?.Entity.Get<GravityHoldable>() is { } gravityHoldable)
            {
                gravityHoldable.ResetType = (GravityType)gravityType;
            }
        }

        public static void InvertedSuperBounce(Player player, float fromY) =>
            GravitySpring.InvertedSuperBounce(player, fromY);

        public static Component CreateAccessibilityListener(Action onAccessibilityChange) =>
            new AccessibilityListener(onAccessibilityChange);

        public static Color GetColor(int gravityType) =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic)[(GravityType)gravityType];

        public static Color GetNormalColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).NormalColor;

        public static Color GetInvertedColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).InvertedColor;

        public static Color GetToggleColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).ToggleColor;

        public static bool BeginCustomTintShader(bool onlyForAccessibility = true)
        {
            if (onlyForAccessibility && GravityHelperModule.Settings.ColorSchemeType ==
                GravityHelperModuleSettings.ColorSchemeSetting.Default)
                return false;

            if (!_effectLoaded)
            {
                _effectLoaded = true;
                if (Everest.Content.TryGet("Effects/GravityHelper/CustomTintShader.cso", out var metadata))
                {
                    _tintEffect = new Effect(Engine.Graphics.GraphicsDevice, metadata.Data);
                }
                else
                {
                    _tintEffect = null;
                    Logger.Warn(nameof(GravityHelperModule), "Couldn't find custom tint shader");
                }
            }

            if (_tintEffect == null) return false;

            GameplayRenderer.End();

            ApplyStandardParameters(_tintEffect);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, _tintEffect, GameplayRenderer.instance.Camera.Matrix);

            return true;
        }

        public static void EndCustomTintShader()
        {
            Draw.SpriteBatch.End();
            GameplayRenderer.Begin();
        }

        public static IDisposable WithCustomTintShader(bool onlyForAccessibility = true) =>
            InternalCustomTintShader(onlyForAccessibility);
    }

    internal static void ClearTintEffect()
    {
        _tintEffect = null;
        _effectLoaded = false;
    }

    private static Effect _tintEffect;
    private static bool _effectLoaded;

    internal static Effect ApplyStandardParameters(this Effect effect, Camera camera = null)
        => ApplyStandardParameters(effect, camera?.Matrix);

    internal static Effect ApplyStandardParameters(this Effect effect, Matrix? camera)
    {
        if (Engine.Scene is not Level level) return null;

        var parameters = effect.Parameters;
        parameters["DeltaTime"]?.SetValue(Engine.DeltaTime);
        parameters["Time"]?.SetValue(Engine.Scene.TimeActive);
        parameters["Dimensions"]
            ?.SetValue(new Vector2(GameplayBuffers.Gameplay.Width, GameplayBuffers.Gameplay.Height));
        parameters["CamPos"]?.SetValue(level.Camera.Position);
        parameters["ColdCoreMode"]?.SetValue(level.CoreMode == Session.CoreModes.Cold);

        Viewport viewport = Engine.Graphics.GraphicsDevice.Viewport;

        Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
        parameters["TransformMatrix"]?.SetValue(projection);

        parameters["ViewMatrix"]?.SetValue(camera ?? Matrix.Identity);
        parameters["Photosensitive"]?.SetValue(Settings.Instance.DisableFlashes);

        return effect;
    }

    internal static InvokeOnDispose InternalCustomTintShader(bool onlyForAccessibility = true) =>
        Exports.BeginCustomTintShader(onlyForAccessibility)
            ? new InvokeOnDispose(Exports.EndCustomTintShader)
            : new InvokeOnDispose();
}
