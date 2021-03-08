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
// using EventTrigger = On.Celeste.EventTrigger;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public class GravityHelperModule : EverestModule
    {
        #region Reflection Cache

        private static readonly FieldInfo normalHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
        private static readonly FieldInfo duckHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
        private static readonly FieldInfo normalHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
        private static readonly FieldInfo duckHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
        private static readonly MethodInfo m_VirtualJoystick_set_Value = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);

        #endregion

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
        }

        public override void Unload()
        {
            On.Celeste.Player.ctor -= Player_ctor;

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;

            On.Celeste.Player.Update -= Player_Update;
            hook_Player_orig_Update.Dispose();
            hook_Player_orig_UpdateSprite.Dispose();
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

            var normalHitbox = (Hitbox) normalHitboxFieldInfo.GetValue(player);
            var duckHitbox = (Hitbox) duckHitboxFieldInfo.GetValue(player);
            var normalHurtbox = (Hitbox) normalHurtboxFieldInfo.GetValue(player);
            var duckHurtbox = (Hitbox) duckHurtboxFieldInfo.GetValue(player);

            if (Gravity == GravityType.Inverted && normalHitbox.Top < -1 || Gravity == GravityType.Normal && normalHitbox.Bottom > 1)
            {
                var collider = player.Collider ?? normalHitbox;
                player.Position.Y = Gravity == GravityType.Inverted ? collider.AbsoluteTop : collider.AbsoluteBottom;
                player.Speed.Y *= -1;
                normalHitbox.Position.Y = -normalHitbox.Position.Y - normalHitbox.Height;
                duckHitbox.Position.Y = -duckHitbox.Position.Y - duckHitbox.Height;
                normalHurtbox.Position.Y = -normalHurtbox.Position.Y - normalHurtbox.Height;
                duckHurtbox.Position.Y = -duckHurtbox.Position.Y - duckHurtbox.Height;
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

            float aimY = Input.Aim.Value.Y;
            int moveY = Input.MoveY.Value;
            Input.MoveY.Value = -moveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, -aimY) });
            orig(self);
            Input.MoveY.Value = moveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, aimY) });
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
            cursor.GotoNext(MoveType.After,
                instr => instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Subtraction"));
            replaceSubtractionWithDelegate(cursor);
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction"));
            replaceSubtractionWithDelegate(cursor);
        }

        private static void Player_BeforeDownTransition(ILContext il) => replaceMaxWithDelegate(new ILCursor(il));

        private static void Player_BeforeUpTransition(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdcR4(-105f));
            cursor.EmitDelegate<Func<float>>(() => !ShouldInvert ? -105f : 105f);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
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