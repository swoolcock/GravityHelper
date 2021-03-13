using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Spikes = On.Celeste.Spikes;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public class GravityHelperModule : EverestModule
    {
        #region Reflection Cache

        private static readonly FieldInfo normalHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
        private static readonly FieldInfo normalHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
        private static readonly FieldInfo duckHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
        private static readonly FieldInfo duckHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
        private static readonly FieldInfo starFlyHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHitbox");
        private static readonly FieldInfo starFlyHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHurtbox");
        private static readonly FieldInfo varJumpTimerFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "varJumpTimer");
        private static readonly MethodInfo virtualJoystickSetValueMethodInfo = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);

        private static readonly object[] virtualJoystickParams = { Vector2.Zero };

        #endregion

        private static void setVirtualJoystickValue(Vector2 value)
        {
            virtualJoystickParams[0] = value;
            virtualJoystickSetValueMethodInfo.Invoke(Input.Aim, virtualJoystickParams);
        }

        public override Type SettingsType => typeof(GravityHelperModuleSettings);

        public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings) Instance._Settings;

        private static IDetour hook_Player_orig_Update;
        private static IDetour hook_Player_orig_UpdateSprite;

        public static GravityHelperModule Instance;

        public static event Action<GravityType> GravityChanged;

        public static bool ShouldInvert => Settings.Enabled && Gravity == GravityType.Inverted;

        private static bool transitioning;
        private static bool solidMoving;

        public static GravityType Gravity
        {
            get => Engine.Scene is Level level ? (GravityType)level.Session.GetCounter(Constants.CurrentGravityCounterKey) : GravityType.Normal;
            set
            {
                if (!(Engine.Scene is Level level)) return;

                var currentGravity = Gravity;
                if (value == currentGravity) return;

                var newValue = value == GravityType.Toggle ? currentGravity.Opposite() : value;

                level.Session.SetCounter(Constants.CurrentGravityCounterKey, (int)newValue);
                updateGravity();

                GravityChanged?.Invoke(newValue);

                var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>().ToArray();
                foreach (Component component in gravityListeners)
                    (component as GravityListener)?.GravityChanged(newValue);
            }
        }

        [Command("gravity", "[Gravity Helper] Sets the gravity:\n 0 -> Normal\n 1 -> Inverted\n 2 -> Toggle")]
        public static void CmdSetGravity(int type)
        {
            if (Engine.Scene is Level && type < 3)
                Gravity = (GravityType)type;
        }

        public GravityHelperModule()
        {
            Instance = this;
        }

        public override void Load()
        {
            On.Celeste.Player.ctor += Player_ctor;

            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
            On.Celeste.Player.Added += PlayerOnAdded;

            On.Celeste.Player.Update += Player_Update;
            hook_Player_orig_Update = new ILHook(typeof(Player).GetMethod(nameof(Player.orig_Update)), Player_orig_Update);

            var updateSpriteMethod = typeof(Player).GetRuntimeMethods().First(m => m.Name == "orig_UpdateSprite");
            hook_Player_orig_UpdateSprite = new ILHook(updateSpriteMethod, Player_orig_UpdateSprite);

            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.ClimbHopBlockedCheck += Player_ClimbHopBlockedCheck;

            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            //On.Celeste.Level.TransitionRoutine += Level_TransitionRoutine;
            IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
            //IL.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;

            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;

            On.Celeste.Level.Update += LevelOnUpdate;

            On.Celeste.Player.Render += Player_Render;
            IL.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
            // On.Celeste.PlayerHair.GetHairScale += PlayerHair_GetHairScale;

            IL.Celeste.Player.OnCollideV += PlayerOnOnCollideV;
            // IL.Celeste.Player.SlipCheck += PlayerOnSlipCheck;
            // IL.Celeste.Player.DreamDashCheck += PlayerOnDreamDashCheck;
            On.Celeste.Player.SlipCheck += PlayerOnSlipCheck;
            On.Celeste.Spikes.ctor_Vector2_int_Directions_string += SpikesOnctor_Vector2_int_Directions_string;
        }

        private void PlayerOnAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            // SpawnGravityTrigger is tracked to make this check faster on spawn
            if (self.Scene.Tracker.Entities.ContainsKey(typeof(SpawnGravityTrigger)))
            {
                SpawnGravityTrigger trigger = self.CollideFirst<SpawnGravityTrigger>();
                if (trigger != null)
                    Gravity = trigger.GravityType;
            }
        }

        private void SpikesOnctor_Vector2_int_Directions_string(Spikes.orig_ctor_Vector2_int_Directions_string orig, Celeste.Spikes self, Vector2 position, int size, Celeste.Spikes.Directions direction, string type)
        {
            orig(self, position, size, direction, type);
            self.Add(new GravityListener());
        }

        private bool PlayerOnSlipCheck(On.Celeste.Player.orig_SlipCheck orig, Player self, float addY)
        {
            if (!ShouldInvert)
                return orig(self, addY);

            Vector2 point = self.Facing != Facings.Right ? self.BottomLeft - Vector2.UnitX - Vector2.UnitY * (4f + addY) : self.BottomRight - Vector2.UnitY * (4f + addY);
            return !self.Scene.CollideCheck<Solid>(point) && !self.Scene.CollideCheck<Solid>(point - Vector2.UnitY * (addY - 4f));
        }

        private void PlayerOnDreamDashCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            // DreamBlock dreamBlock = this.CollideFirst<DreamBlock>(this.Position + dir);
            replaceAdditionWithDelegate(cursor);
            // if (this.CollideCheck<Solid, DreamBlock>(this.Position + dir))
            replaceAdditionWithDelegate(cursor);
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            replaceAdditionWithDelegate(cursor, 2);
            // this.Position = this.Position + vector2 * (float) index;
            replaceAdditionWithDelegate(cursor);
            // if (!this.CollideCheck<Solid, DreamBlock>(this.Position + dir + vector2 * (float) index))
            replaceAdditionWithDelegate(cursor, 2);
            // this.Position = this.Position + vector2 * (float) index;
            replaceAdditionWithDelegate(cursor);
        }

        private void PlayerOnOnCollideV(ILContext il)
        {
            var cursor = new ILCursor(il);

            // if (this.DashAttacking && (double) data.Direction.Y == (double) Math.Sign(this.DashDir.Y))
            replaceSignWithDelegate(cursor);
            // this.ReflectBounce(new Vector2(0.0f, (float) -Math.Sign(this.Speed.Y)));
            replaceSignWithDelegate(cursor);
            // if (this.DreamDashCheck(Vector2.UnitY * (float) Math.Sign(this.Speed.Y)))
            replaceSignWithDelegate(cursor);

            cursor.GotoNext(instr => instr.MatchCall<Entity>(nameof(Entity.CollideCheck)));
            cursor.Goto(cursor.Index - 2);
            replaceAdditionWithDelegate(cursor, 4);
        }

        public override void Unload()
        {
            On.Celeste.Player.ctor -= Player_ctor;

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;

            On.Celeste.Player.Update -= Player_Update;
            hook_Player_orig_Update?.Dispose();
            hook_Player_orig_UpdateSprite?.Dispose();
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.ClimbHopBlockedCheck -= Player_ClimbHopBlockedCheck;

            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            //On.Celeste.Level.TransitionRoutine -= Level_TransitionRoutine;
            IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
            //IL.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;

            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;

            On.Celeste.Level.Update -= LevelOnUpdate;

            On.Celeste.Player.Render -= Player_Render;
            IL.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
            // On.Celeste.PlayerHair.GetHairScale -= PlayerHair_GetHairScale;

            IL.Celeste.Player.OnCollideV -= PlayerOnOnCollideV;
        }

        private static void updateGravity(Player player = null)
        {
            if (!Settings.Enabled) return;

            player ??= Engine.Scene.Entities.FindFirst<Player>();
            if (player == null) return;

            void invertHitbox(Hitbox hitbox) => hitbox.Position.Y = -hitbox.Position.Y - hitbox.Height;

            var normalHitbox = (Hitbox) normalHitboxFieldInfo.GetValue(player);
            var collider = player.Collider ?? normalHitbox;

            if (Gravity == GravityType.Inverted && collider.Top < -1 || Gravity == GravityType.Normal && collider.Bottom > 1)
            {
                var normalHurtbox = (Hitbox) normalHurtboxFieldInfo.GetValue(player);
                var duckHitbox = (Hitbox) duckHitboxFieldInfo.GetValue(player);
                var duckHurtbox = (Hitbox) duckHurtboxFieldInfo.GetValue(player);
                var starFlyHitbox = (Hitbox) starFlyHitboxFieldInfo.GetValue(player);
                var starFlyHurtbox = (Hitbox) starFlyHurtboxFieldInfo.GetValue(player);

                player.Position.Y = Gravity == GravityType.Inverted ? collider.AbsoluteTop : collider.AbsoluteBottom;
                player.Speed.Y *= -1;
                player.DashDir.Y *= -1;
                varJumpTimerFieldInfo.SetValue(player, 0f);

                invertHitbox(normalHitbox);
                invertHitbox(normalHurtbox);
                invertHitbox(duckHitbox);
                invertHitbox(duckHurtbox);
                invertHitbox(starFlyHitbox);
                invertHitbox(starFlyHurtbox);
            }
        }

        #region On Hooks

        private void LevelOnUpdate(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (Settings.ToggleInvertGravity.Pressed)
            {
                Settings.ToggleInvertGravity.ConsumePress();
                if (Settings.Enabled)
                    Gravity = Gravity.Opposite();
            }

            orig(self);
        }

        private static void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            if (!ShouldInvert)
            {
                orig(self, move);
                return;
            }

            solidMoving = true;
            orig(self, move);
            solidMoving = false;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position,
            PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            updateGravity(self);

            self.Add(new TransitionListener
            {
                OnOutBegin = () => transitioning = true,
                OnInEnd = () => transitioning = false
            });
        }

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck) =>
            orig(self, ShouldInvert && self is Player ? -downCheck : downCheck);

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (!ShouldInvert)
            {
                orig(self);
                return;
            }

            var aimY = Input.Aim.Value.Y;
            var moveY = Input.MoveY.Value;

            setVirtualJoystickValue(new Vector2(Input.Aim.Value.X, -aimY));
            Input.MoveY.Value = -moveY;

            orig(self);

            Input.MoveY.Value = moveY;
            setVirtualJoystickValue(new Vector2(Input.Aim.Value.X, aimY));
        }

        private static IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self,
            LevelData next, Vector2 direction)
        {
            transitioning = true;
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext())
                yield return origEnum.Current;
            transitioning = false;
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target,
            Vector2 direction)
        {
            transitioning = true;
            bool val = orig(self, target, direction);
            transitioning = false;
            return val;
        }

        private void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            var scaleY = self.Sprite.Scale.Y;

            if (ShouldInvert)
                self.Sprite.Scale.Y = -scaleY;

            orig(self);

            if (ShouldInvert)
                self.Sprite.Scale.Y = scaleY;
        }

        private Vector2 PlayerHair_GetHairScale(On.Celeste.PlayerHair.orig_GetHairScale orig, PlayerHair self, int index)
        {
            if (self == null) return Vector2.One;

            var scale = orig(self, index);
            if (ShouldInvert)
                scale.Y *= -1;
            return scale;
        }

        #endregion

        #region IL Hooks

        private static void Actor_MoveVExact(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Actor, bool>>(a =>
                a is Player player
                && player.CurrentBooster == null
                && !solidMoving
                && !transitioning
                && ShouldInvert);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Starg, 1);
        }

        private static void Actor_IsRiding(ILContext il) => replaceAdditionWithDelegate(new ILCursor(il));

        private static void Player_orig_Update(ILContext il)
        {
            var cursor = new ILCursor(il);
            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor, 2);

            // Math.Min(base.Y, highestAirY) -> Math.Max(base.Y, highestAirY)
            replaceMaxWithDelegate(cursor);

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor, 2);
        }

        private static void Player_orig_UpdateSprite(ILContext il)
        {
            var cursor = new ILCursor(il);

            // fix dangling animation
            replaceAdditionWithDelegate(cursor);

            // skip push check
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("op_Addition"));

            // fix edge animation
            replaceAdditionWithDelegate(cursor, 3);

            // fix edgeBack animation
            replaceAdditionWithDelegate(cursor, 3);
        }

        private static void Player_ClimbUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            // if (this.CollideCheck<Solid>(this.Position - Vector2.UnitY) || this.ClimbHopBlockedCheck() && this.SlipCheck(-1f))
            cursor.GotoNext(MoveType.After,
                instr => instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Subtraction"));
            replaceSubtractionWithDelegate(cursor);

            // if (Input.MoveY.Value != 1 && (double) this.Speed.Y > 0.0 && !this.CollideCheck<Solid>(this.Position + new Vector2((float) this.Facing, 1f)))
            replaceAdditionWithDelegate(cursor);
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            replaceSubtractionWithDelegate(cursor);
        }

        private static void Player_BeforeDownTransition(ILContext il) => replaceMaxWithDelegate(new ILCursor(il));

        private static void Player_BeforeUpTransition(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdcR4(-105f));
            cursor.Remove();
            cursor.EmitDelegate<Func<float>>(() => !ShouldInvert ? -105f : 105f);
        }

        private static void Solid_GetPlayerOnTop(ILContext il) => replaceSubtractionWithDelegate(new ILCursor(il));

        private void PlayerHair_AfterUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            replaceAdditionWithDelegate(cursor, -1);
            cursor.Goto(0);
            replaceSubtractionWithDelegate(cursor, -1);
        }

        #endregion

        #region IL Helpers

        private delegate Vector2 VectorBinaryOperation(Vector2 lhs, Vector2 rhs);

        private delegate float FloatBinaryOperation(float a, float b);
        private delegate int FloatUnaryOperation(float a);

        private static void replaceAdditionWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall<Vector2>("op_Addition")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) =>
                    lhs + (ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs));
            }
        }

        private static void replaceSubtractionWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) =>
                    lhs - (ShouldInvert ? new Vector2(rhs.X, -rhs.Y) : rhs));
            }
        }

        private static void replaceMaxWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall("System.Math", "Max")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<FloatBinaryOperation>((a, b) =>
                    ShouldInvert ? Math.Min(a, b) : Math.Max(a, b));
            }
        }

        private static void replaceSignWithDelegate(ILCursor cursor, int count = 1)
        {
            while (count != 0 && cursor.TryGotoNext(instr => instr.MatchCall("System.Math", "Sign")))
            {
                if (count > 0) count--;
                cursor.Remove();
                cursor.EmitDelegate<FloatUnaryOperation>(a =>
                    ShouldInvert ? -Math.Sign(a) : Math.Sign(a));
            }
        }

        #endregion
    }
}