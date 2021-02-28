using System;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace GravityHelper
{
    public static class InteractionHooks
    {
        private static GravityHelperModule.GravityTypes gravity
        {
            get => GravityHelperModule.Instance.Gravity;
            set => GravityHelperModule.Instance.Gravity = value;
        }

        public static void Load()
        {
            // Springs
            On.Celeste.Spring.ctor_Vector2_Orientations_bool += Spring_ctor_Vector2_Orientations_bool;
            On.Celeste.Spring.OnCollide += Spring_OnCollide;
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;

            // Feathers
            IL.Celeste.Player.StarFlyUpdate += starFlyPatchAim;
            starFlyCoroutineHook = EasierILHook.HookCoroutine("Celeste.Player", "StarFlyCoroutine", starFlyPatchAim);
        }

        private static ILHook starFlyCoroutineHook;

        private static void starFlyPatchAim(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldsfld))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<Vector2>>(getFeatherAim);
                cursor.Remove();
                break;
            }

            Vector2 getFeatherAim()
            {
                Vector2 aim = Input.Aim;

                if (gravity == GravityHelperModule.GravityTypes.Inverted)
                    aim.Y = -aim.Y;

                return aim;
            }
        }

        public static void Unload()
        {
            // Springs
            On.Celeste.Spring.ctor_Vector2_Orientations_bool -= Spring_ctor_Vector2_Orientations_bool;
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            // Feathers
            IL.Celeste.Player.StarFlyUpdate -= starFlyPatchAim;
            starFlyCoroutineHook?.Dispose();
        }

        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
        {
            switch (entityData.Name)
            {
                case "falls/springDown":
                    level.Add(new Spring(entityData, offset, (Spring.Orientations)3));
                    return true;

                case "falls/SpringUp":
                    level.Add(new Spring(entityData, offset, (Spring.Orientations)4));
                    return true;

                case "falls/watchtower":
                    Lookout watchtower = new Lookout(entityData, offset);
                    Sprite spr = watchtower.Get<Sprite>();
                    // ReSharper disable once InvokeAsExtensionMethod
                    spr.Rotation = Calc.ToRad(180f);
                    spr.Position -= Vector2.UnitY * 16f;
                    level.Add(watchtower);
                    return true;

                default:
                    return false;
            }
        }

        private static void Player_SuperBounce(On.Celeste.Player.orig_SuperBounce orig, Player self, float fromY)
        {
            orig(self, fromY);
            if (gravity != GravityHelperModule.GravityTypes.Inverted) return;

            self.Speed.Y = 185f;

            FieldInfo varJumpSpeed = typeof(Player).GetField("varJumpSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            varJumpSpeed?.SetValue(self, 185f);
        }

        private static void Spring_OnCollide(On.Celeste.Spring.orig_OnCollide orig, Spring self, Player player)
        {
            if (player.StateMachine.State != Player.StDreamDash)
            {
                if (self.Orientation == (Spring.Orientations)3)
                {
                    bool flag3 = player.Speed.Y <= 0f;

                    if (flag3)
                    {
                        // REEEEEEEE
                        //self.BounceAnimate();
                        MethodInfo bounceAnimate = self.GetType().GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);
                        bounceAnimate?.Invoke(self, null);
                        player.SuperBounce(self.Bottom);
                        gravity = GravityHelperModule.GravityTypes.Inverted;
                    }
                }
                // Normal spring, but changes gravity
                else if (self.Orientation == (Spring.Orientations)4)
                {
                    gravity = GravityHelperModule.GravityTypes.Normal;
                    MethodInfo bounceAnimate = self.GetType().GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);
                    bounceAnimate?.Invoke(self, null);
                    player.SuperBounce(self.Top);
                }
                else
                {
                    orig(self, player);
                }
            }
        }

        private static void Spring_ctor_Vector2_Orientations_bool(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 position, Spring.Orientations orientation, bool playerCanUse)
        {
            bool bottom = false;
            bool topSetGrav = false;

            if (orientation == (Spring.Orientations)3)
            {
                orientation = 0;
                bottom = true;
            }
            else if (orientation == (Spring.Orientations)4)
            {
                orientation = 0;
                topSetGrav = true;
            }

            orig(self, position, orientation, playerCanUse);

            if (bottom)
            {
                if (typeof(Spring).GetField("staticMover", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(self) is StaticMover staticMover)
                {
                    staticMover.SolidChecker = s => self.CollideCheck(s, self.Position + Vector2.UnitY);
                    staticMover.JumpThruChecker = jt => self.CollideCheck(jt, self.Position + Vector2.UnitY);
                    //staticMover.OnEnable = new Action(this.OnEnable);
                    //staticMover.OnDisable = new Action(self.OnDisable);
                    self.Add(staticMover);
                }

                self.Orientation = (Spring.Orientations)3;
                self.Collider = new Hitbox(16f, 6f, -8f);

                if (typeof(Spring).GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(self) is Sprite sprite)
                {
                    sprite.Rotation = 180f.ToRad();
                    sprite.Color = Color.Silver;
                }

                self.Add(new SpringParticles(Color.Yellow));
            }
            else if (topSetGrav)
            {
                self.Orientation = (Spring.Orientations)4;
                if (typeof(Spring).GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(self) is Sprite sprite)
                    sprite.Color = Color.Silver;

                self.Add(new SpringParticles(Color.Blue));
            }
        }
    }
}
