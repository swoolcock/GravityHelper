// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class GliderHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Glider)} hooks...");

            IL.Celeste.Glider.Update += Glider_Update;
            On.Celeste.Glider.Added += Glider_Added;
            On.Celeste.Glider.Render += Glider_Render;
            On.Celeste.Glider.Update += Glider_Update;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Glider)} hooks...");

            IL.Celeste.Glider.Update -= Glider_Update;
            On.Celeste.Glider.Added -= Glider_Added;
            On.Celeste.Glider.Render -= Glider_Render;
            On.Celeste.Glider.Update -= Glider_Update;
        }

        private static void Glider_Added(On.Celeste.Glider.orig_Added orig, Glider self, Scene scene)
        {
            orig(self, scene);
            if (self.Get<GravityComponent>() != null) return;
            self.Add(new GravityComponent
            {
                UpdateSpeed = args =>
                {
                    if (!args.Changed || self.Scene == null) return;
                    if (args.Instant)
                        self.Speed.Y = 160f * (self.SceneAs<Level>().InSpace ? 0.6f : 1f);
                    else
                        self.Speed.Y *= -args.MomentumMultiplier;
                },
                GetSpeed = () => self.Speed,
                SetSpeed = value => self.Speed = value,
            });
        }

        private static void Glider_Render(On.Celeste.Glider.orig_Render orig, Glider self)
        {
            if (!self.ShouldInvert())
            {
                orig(self);
                return;
            }

            var sprite = self.sprite;
            var oldScale = sprite.Scale;
            var oldRotation = sprite.Rotation;
            sprite.Scale = new Vector2(oldScale.X, -oldScale.Y);
            sprite.Rotation = -oldRotation;
            orig(self);
            sprite.Scale = oldScale;
            sprite.Rotation = oldRotation;
        }

        private static void Glider_Update(On.Celeste.Glider.orig_Update orig, Glider self)
        {
            var value = Input.GliderMoveY.Value;
            if (GravityHelperModule.ShouldInvertPlayer && GravityHelperModule.Settings.ControlScheme == GravityHelperModuleSettings.ControlSchemeSetting.Absolute)
                Input.GliderMoveY.Value = -value;
            orig(self);
            Input.GliderMoveY.Value = value;
        }

        private static void Glider_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveV))) ||
                !cursor.TryGotoNext(instr => instr.MatchLdarg(0),
                    instr => instr.MatchCall<Entity>("get_Left"),
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdfld<Glider>("level")))
                throw new HookException("Couldn't find start of bounds check");

            var cursor2 = cursor.Clone();
            if (!cursor2.TryGotoNext(instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<Glider>(nameof(Glider.Hold)),
                instr => instr.MatchCallvirt<Holdable>(nameof(Holdable.CheckAgainstColliders))))
                throw new HookException("Couldn't find end of bounds check");

            // replace with custom checks
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Glider, bool>>(self => self.ShouldInvert());
            cursor.Emit(OpCodes.Brfalse, cursor.Next);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Glider, bool>>(self =>
            {
                var level = self.SceneAs<Level>();
                var bounds = level.Bounds;

                if (self.Left < bounds.Left)
                {
                    self.Left = bounds.Left;
                    self.OnCollideH(new CollisionData { Direction = -Vector2.UnitX });
                }
                else if (self.Right > bounds.Right)
                {
                    self.Right = bounds.Right;
                    self.OnCollideH(new CollisionData { Direction = Vector2.UnitX });
                }

                if (self.Bottom > bounds.Bottom)
                {
                    self.Bottom = bounds.Bottom;
                    self.OnCollideV(new CollisionData { Direction = Vector2.UnitY });
                }
                else if (self.Bottom < bounds.Top - 16)
                {
                    self.RemoveSelf();
                    return true;
                }

                return false;
            });
            cursor.Emit(OpCodes.Brfalse_S, cursor2.Next);
            cursor.Emit(OpCodes.Ret);
        });
    }
}
