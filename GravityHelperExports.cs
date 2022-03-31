// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.GravityHelper {
    [ModExportName("GravityHelper")]
    public static class GravityHelperExports {
        public static string GravityTypeFromInt(int gravityType) => ((GravityType) gravityType).ToString();

        public static int GravityTypeToInt(string name) =>
            (int) (Enum.TryParse<GravityType>(name, out var value) ? value : GravityType.Normal);

        public static int GetPlayerGravity() =>
            (int) (GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

        public static void SetPlayerGravity(int gravityType, float momentumMultiplier) =>
            GravityHelperModule.PlayerComponent?.SetGravity((GravityType) gravityType, momentumMultiplier);

        public static bool IsPlayerInverted() => GravityHelperModule.ShouldInvertPlayer;

        public static Component CreateGravityComponent(
            Action<int, bool, float> updatePosition,
            Action<int, bool, float> updateColliders,
            Action<int, bool, float> updateSpeed,
            Action<int, bool, float> updateVisuals,
            Func<Vector2> getSpeed,
            Action<Vector2> setSpeed,
            Func<bool> checkInvert)
        {
            static Action<GravityChangeArgs> makeClosure(Action<int, bool, float> action) =>
                action == null ? null : args => action((int)args.NewValue, args.Changed, args.MomentumMultiplier);

            return new GravityComponent
            {
                UpdatePosition = makeClosure(updatePosition),
                UpdateColliders = makeClosure(updateColliders),
                UpdateSpeed = makeClosure(updateSpeed),
                UpdateVisuals = makeClosure(updateVisuals),
                CheckInvert = checkInvert,
                GetSpeed = getSpeed,
                SetSpeed = setSpeed,
            };
        }

        public static Component CreateEntityGravityListener(Entity entity, Action<Entity, int, bool, float> action) =>
            new GravityListener(entity)
            {
                GravityChanged = (e, args) => action(e, (int)args.NewValue, args.Changed, args.MomentumMultiplier),
            };

        public static Component CreateTypeGravityListener(Type type, Action<Entity, int, bool, float> action) =>
            new GravityListener(type)
            {
                GravityChanged = (e, args) => action(e, (int)args.NewValue, args.Changed, args.MomentumMultiplier),
            };

        public static Component CreatePlayerGravityListener(Action<Entity, int, bool, float> action) =>
            new PlayerGravityListener
            {
                GravityChanged = (e, args) => action(e, (int)args.NewValue, args.Changed, args.MomentumMultiplier),
            };
    }
}
