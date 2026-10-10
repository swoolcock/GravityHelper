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
        /// <summary>
        /// Blacklists a mod from being hooked by Gravity Helper.
        /// Call this from your own mod if Gravity Helper is being naughty and hooking you.
        /// Make sure to properly implement Gravity Helper support first!
        /// </summary>
        public static void RegisterModSupportBlacklist(string modName) =>
            ThirdPartyModSupport.BlacklistedMods.Add(modName);

        /// <summary>
        /// Returns the enum name for the specified gravity type.
        /// </summary>
        public static string GravityTypeFromInt(int gravityType) => ((GravityType)gravityType).ToString();

        /// <summary>
        /// Attempts to get the gravity type from the specified enum name.
        /// </summary>
        public static int GravityTypeToInt(string name) =>
            (int)(Enum.TryParse<GravityType>(name, out var value) ? value : GravityType.Normal);

        /// <summary>
        /// Returns the player's gravity type, where 0 == normal and 1 == inverted.
        /// </summary>
        public static int GetPlayerGravity() =>
            (int)(GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

        /// <summary>
        /// Returns the specified actor's gravity type.
        /// </summary>
        public static int GetActorGravity(Actor actor) => (int)(actor?.GetGravity() ?? GravityType.Normal);

        /// <summary>
        /// Sets the player's gravity to the specified type.
        /// The player's absolute speed after changing will be multiplied by <see cref="momentumMultiplier"/>,
        /// and you usually want to leave this as 1 so that "speedy-thing goes in, speedy-thing comes out".
        /// A multiplier of 0 will halt the player in midair, and a negative multiplier will make the player
        /// appear to ignore acceleration (you probably don't want this).
        /// </summary>
        public static void SetPlayerGravity(int gravityType, float momentumMultiplier) =>
            GravityHelperModule.PlayerComponent?.SetGravity((GravityType)gravityType, momentumMultiplier);

        /// <summary>
        /// Sets the actor's gravity to the specified type.
        /// See <see cref="SetPlayerGravity"/> for an explanation of <see cref="momentumMultiplier"/>.
        /// </summary>
        public static void SetActorGravity(Actor actor, int gravityType, float momentumMultiplier) =>
            actor?.SetGravity((GravityType)gravityType, momentumMultiplier);

        /// <summary>
        /// Returns true if the player is upside-down.
        /// Prefer this over checking the gravity type manually.
        /// </summary>
        public static bool IsPlayerInverted() => GravityHelperModule.ShouldInvertPlayer;

        /// <summary>
        /// Returns true if the provided actor is upside-down.
        /// </summary>
        public static bool IsActorInverted(Actor actor) => actor?.ShouldInvert() ?? false;

        /// <summary>
        /// Returns a vector that should be added to check something "above" the actor,
        /// relative to its current gravity state.
        /// </summary>
        public static Vector2 GetAboveVector(Actor actor) =>
            actor?.ShouldInvert() == true ? Vector2.UnitY : -Vector2.UnitY;

        /// <summary>
        /// Returns a vector that should be added to check something "below" the actor,
        /// relative to its current gravity state.
        /// </summary>
        public static Vector2 GetBelowVector(Actor actor) =>
            actor?.ShouldInvert() == true ? -Vector2.UnitY : Vector2.UnitY;

        /// <summary>
        /// Gets the bottom center position of the actor's collider if inverted, otherwise the top center.
        /// </summary>
        public static Vector2 GetTopCenter(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.BottomCenter : actor?.TopCenter ?? Vector2.Zero;

        /// <summary>
        /// Gets the top center position of the actor's collider if inverted, otherwise the bottom center.
        /// </summary>
        public static Vector2 GetBottomCenter(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.TopCenter : actor?.BottomCenter ?? Vector2.Zero;

        /// <summary>
        /// Gets the bottom left corner of the actor's collider if inverted, otherwise the top left.
        /// </summary>
        public static Vector2 GetTopLeft(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.BottomLeft : actor?.TopLeft ?? Vector2.Zero;

        /// <summary>
        /// Gets the top left corner of the actor's collider if inverted, otherwise the bottom left.
        /// </summary>
        public static Vector2 GetBottomLeft(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.TopLeft : actor?.BottomLeft ?? Vector2.Zero;

        /// <summary>
        /// Gets the bottom right corner of the actor's collider if inverted, otherwise the top right.
        /// </summary>
        public static Vector2 GetTopRight(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.BottomRight : actor?.TopRight ?? Vector2.Zero;

        /// <summary>
        /// Gets the top right corner of the actor's collider if inverted, otherwise the bottom right.
        /// </summary>
        public static Vector2 GetBottomRight(Actor actor) =>
            actor?.ShouldInvert() == true ? actor.TopRight : actor?.BottomRight ?? Vector2.Zero;

        /// <summary>
        /// Inverts the provided vector's Y component if the player is upside-down and their
        /// control scheme is set to absolute.
        /// </summary>
        public static Vector2 TransformVector(Vector2 vec)
        {
            if (!GravityHelperModule.ShouldInvertPlayer || IsControlSchemeRelative())
                return vec;
            return new Vector2(vec.X, -vec.Y);
        }

        /// <summary>
        /// Inverts the provided vector's Y component if the player is upside-down and their
        /// feather control scheme is set to absolute.
        /// </summary>
        public static Vector2 TransformFeatherVector(Vector2 vec)
        {
            if (!GravityHelperModule.ShouldInvertPlayer || IsFeatherControlSchemeRelative())
                return vec;
            return new Vector2(vec.X, -vec.Y);
        }

        /// <summary>
        /// Inverts the provided vector's Y component if the provided actor is upside-down.
        /// If the actor is the player, it will defer to <see cref="TransformVector"/> instead.
        /// </summary>
        public static Vector2 TransformVectorForActor(Vector2 vec, Actor actor)
        {
            if (actor is Player) return TransformVector(vec);
            return new Vector2(vec.X, actor.ShouldInvert() ? -vec.Y : vec.Y);
        }

        /// <summary>
        /// Returns true if the player has their control scheme set to relative.
        /// </summary>
        public static bool IsControlSchemeRelative() => GravityHelperModule.Settings.ControlScheme ==
                                                        GravityHelperModuleSettings.ControlSchemeSetting.Relative;

        /// <summary>
        /// Returns true if the player has their feather control scheme set to relative.
        /// </summary>
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

        /// <summary>
        /// Creates an instance of <see cref="UpsideDownTalkComponentUI"/> that can be added to
        /// an entity to display an upside down speech bubble.
        /// </summary>
        public static TalkComponent.TalkComponentUI CreateUpsideDownTalkComponentUI(TalkComponent talkComponent) =>
            new UpsideDownTalkComponentUI(talkComponent);

        /// <summary>
        /// Creates a <see cref="GravityListener"/> component that will call the specified action
        /// when gravity changes for the provided actor.
        /// The action arguments are:
        ///     #1 Entity: The entity that changed gravity.
        ///     #2 int: The new gravity type.
        ///     #3 float: The momentum multiplier provided.
        /// </summary>
        public static Component CreateGravityListener(Actor actor, Action<Entity, int, float> gravityChanged) =>
            new GravityListener(actor, (e, a) =>
                gravityChanged(e, (int)a.NewValue, a.MomentumMultiplier));

        /// <summary>
        /// Creates a <see cref="PlayerGravityListener"/> component that will call the specified action
        /// when gravity changes for the player.
        /// Prefer this for the player since the action accepts a Player argument instead of Entity.
        /// The action arguments are:
        ///     #1 Player: The player.
        ///     #2 int: The new gravity type.
        ///     #3 float: The momentum multiplier provided.
        /// </summary>
        public static Component CreatePlayerGravityListener(Action<Player, int, float> gravityChanged) =>
            new PlayerGravityListener((e, a) =>
                gravityChanged(e as Player, (int)a.NewValue, a.MomentumMultiplier));

        /// <summary>
        /// Increases the semaphore that forces the player to always be rendered upside down.
        /// Make sure you balance the call with <see cref="EndForceInvertPlayerRender"/> in the same frame.
        /// </summary>
        public static void BeginForceInvertPlayerRender() => GravityHelperModule.ForceInvertPlayerRenderSemaphore++;

        /// <summary>
        /// Decreases the semaphore that forces the player to always be rendered upside down.
        /// Call this the same number of times you call <see cref="BeginForceInvertPlayerRender"/> in the same frame.
        /// </summary>
        public static void EndForceInvertPlayerRender() => GravityHelperModule.ForceInvertPlayerRenderSemaphore--;

        /// <summary>
        /// Increases the semaphore that prevents calls to Player.MoveV from being inverted.
        /// Use this if you need to manually adjust the player's position regardless of their gravity setting.
        /// Example: A booster pulling the player towards its center should not care about the player's gravity.
        /// Make sure you balance the call with <see cref="EndOverride"/> in the same frame.
        /// </summary>
        public static void BeginOverride() => GravityHelperModule.OverrideSemaphore++;

        /// <summary>
        /// Decreases the semaphore that prevents calls to Player.MoveV from being inverted.
        /// Call this the same number of times you call <see cref="BeginOverride"/> in the same frame.
        /// </summary>
        public static void EndOverride() => GravityHelperModule.OverrideSemaphore--;

        /// <summary>
        /// Executes the passed action within an override block.
        /// </summary>
        public static void ExecuteOverride(Action action)
        {
            GravityHelperModule.OverrideSemaphore++;
            action?.Invoke();
            GravityHelperModule.OverrideSemaphore--;
        }

        /// <summary>
        /// Increments the override semaphore and returns a disposable that will
        /// automatically decrement on dispose.
        /// Note that the disposable will be autoboxed, and can be null if Gravity Helper is not loaded.
        ///
        /// Example:
        /// <code>
        ///     using(GravityHelperExports.WithOverride()) {
        ///         // some code that needs an override block
        ///     }
        /// </code>
        /// </summary>
        public static IDisposable WithOverride()
        {
            GravityHelperModule.OverrideSemaphore++;
            return new InvokeOnDispose(() => GravityHelperModule.OverrideSemaphore--);
        }

        /// <summary>
        /// Sets the amount of time a specific thrown holdable will wait before resetting its
        /// gravity state.
        /// </summary>
        public static void SetHoldableResetTime(Holdable holdable, float resetTime)
        {
            if (holdable?.Entity.Get<GravityHoldable>() is { } gravityHoldable)
            {
                gravityHoldable.ResetTime = resetTime;
            }
        }

        /// <summary>
        /// Sets the gravity type a specific thrown holdable will become after it resets.
        /// </summary>
        public static void SetHoldableResetType(Holdable holdable, int gravityType)
        {
            if (holdable?.Entity.Get<GravityHoldable>() is { } gravityHoldable)
            {
                gravityHoldable.ResetType = (GravityType)gravityType;
            }
        }

        /// <summary>
        /// Triggers a spring head bounce.
        /// Call this on a ceiling spring for normal gravity, or a floor spring for inverted gravity.
        /// </summary>
        public static void InvertedSuperBounce(Player player, float fromY) =>
            GravitySpring.InvertedSuperBounce(player, fromY);

        /// <summary>
        /// Create an <see cref="AccessibilityListener"/> with an action that will fire if
        /// any accessibility options are changed.
        /// After adding this component to your entity, you should immediately call the
        /// passed action/method group to ensure your entity displays correctly.
        /// </summary>
        public static Component CreateAccessibilityListener(Action onAccessibilityChange) =>
            new AccessibilityListener(onAccessibilityChange);

        /// <summary>
        /// Gets the current color for the specified gravity type, considering accessibility options.
        /// </summary>
        public static Color GetColor(int gravityType) =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic)[(GravityType)gravityType];

        /// <summary>
        /// Gets the current color for normal gravity, considering accessibility options.
        /// </summary>
        public static Color GetNormalColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).NormalColor;

        /// <summary>
        /// Gets the current color for inverted gravity, considering accessibility options.
        /// </summary>
        public static Color GetInvertedColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).InvertedColor;

        /// <summary>
        /// Gets the current color for toggling gravity, considering accessibility options.
        /// </summary>
        public static Color GetToggleColor() =>
            (GravityHelperModule.Settings.GetColorScheme() ?? GravityColorScheme.Classic).ToggleColor;

        /// <summary>
        /// Activates the accessibility shader if the color scheme is set to something other than default.
        /// Ensure you <see cref="EndCustomTintShader"/> once shaded rendering is finished.
        /// Regular rendering multiplies the source pixel by the tint color's RGB,
        /// but the accessibility shader instead:
        ///     Converts the source pixel to HSV
        ///     Converts the tint color to HSV
        ///     Calculates the shaded color as (tint.H, tint.S * src.S, tint.V * src.V)
        ///     Converts the shaded color back to RGB
        /// The final effect is that of tinting a greyscale texture, such that the source hue is ignored,
        /// while being able to be used on any texture.
        ///
        /// Example in an entity that has a sprite field:
        /// <code>
        ///     public void Render() {
        ///         if (BeginCustomTintShader(true)) {
        ///             this.sprite.Color = (the color you want it to be tinted)
        ///             base.Render();
        ///             this.sprite.Color = Color.White;
        ///             EndCustomTintShader();
        ///         } else {
        ///             this.sprite.Color = Color.White;
        ///             base.Render();
        ///         }
        ///     }
        /// </code>
        /// </summary>
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

            _customTintShaderActive = true;

            return true;
        }

        /// <summary>
        /// Ends the accessibility tint shader.
        /// </summary>
        public static void EndCustomTintShader()
        {
            _customTintShaderActive = false;
            Draw.SpriteBatch.End();
            GameplayRenderer.Begin();
        }

        /// <summary>
        /// Returns true if the accessibility shader is currently active.
        /// </summary>
        public static bool IsCustomTintShaderActive() => _customTintShaderActive;

        /// <summary>
        /// Activates the accessibility shader and returns a disposable that will
        /// deactivate the shader when disposed.
        /// Note that the disposable will be autoboxed, and can be null if Gravity Helper is not loaded.
        ///
        /// <code>
        ///     using (WithCustomTintShader(true)) {
        ///         // things that should be rendered with the accessibility shader
        ///         // note that the shader may not be active if it was not required
        ///         // check using <see cref="IsCustomTintShaderActive"/>
        ///     }
        /// </code>
        /// </summary>
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
    private static bool _customTintShaderActive;

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
