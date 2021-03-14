using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;
using System.Reflection;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule : EverestModule
    {
        #region Reflection

        private static readonly FieldInfo normalHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
        private static readonly FieldInfo normalHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
        private static readonly FieldInfo duckHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
        private static readonly FieldInfo duckHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
        private static readonly FieldInfo starFlyHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHitbox");
        private static readonly FieldInfo starFlyHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHurtbox");
        private static readonly FieldInfo varJumpTimerFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "varJumpTimer");
        private static readonly MethodInfo virtualJoystickSetValueMethodInfo = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);
        private static readonly MethodInfo updateSpriteMethodInfo = typeof(Player).GetRuntimeMethods().First(m => m.Name == "orig_UpdateSprite");
        private static readonly MethodInfo playerOrigUpdateMethodInfo = typeof(Player).GetMethod(nameof(Player.orig_Update));

        private static readonly object[] virtualJoystickParams = { Vector2.Zero };

        private static void setVirtualJoystickValue(Vector2 value)
        {
            virtualJoystickParams[0] = value;
            virtualJoystickSetValueMethodInfo.Invoke(Input.Aim, virtualJoystickParams);
        }

        #endregion

        #region EverestModule

        public override Type SettingsType => typeof(GravityHelperModuleSettings);

        public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings) Instance._Settings;

        public static GravityHelperModule Instance { get; private set; }

        public GravityHelperModule()
        {
            Instance = this;
        }

        public override void Load()
        {
            loadOnHooks();
            loadILHooks();
        }

        public override void Unload()
        {
            unloadOnHooks();
            unloadILHooks();
        }

        [Command("gravity", "[Gravity Helper] Sets the gravity:\n 0 -> Normal\n 1 -> Inverted\n 2 -> Toggle")]
        public static void CmdSetGravity(int type)
        {
            if (Engine.Scene is Level && type < 3)
                Gravity = (GravityType)type;
        }

        #endregion

        #region Gravity Handling

        private static bool transitioning;
        private static bool solidMoving;

        private static GravityType? gravity;
        public static GravityType Gravity
        {
            get => Engine.Scene is Level level && Settings.Enabled ? gravity ??= (GravityType)level.Session.GetCounter(Constants.CurrentGravityCounterKey) : GravityType.Normal;
            set => SetGravity(value);
        }

        internal static void SetGravity(GravityType gravityType, Scene scene = null, bool forceTrigger = false)
        {
            scene ??= Engine.Scene;

            if (!(scene is Level level) || !Settings.Enabled) return;

            var currentGravity = (GravityType)level.Session.GetCounter(Constants.CurrentGravityCounterKey);

            if (gravityType == currentGravity && !forceTrigger) return;

            var newValue = gravityType == GravityType.Toggle ? currentGravity.Opposite() : gravityType;
            gravity = newValue;

            level.Session.SetCounter(Constants.CurrentGravityCounterKey, (int)newValue);

            updateGravity();

            GravityChanged?.Invoke(currentGravity);

            var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>().ToArray();
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.GravityChanged(currentGravity);
        }

        public static GravityType PreviousGravity
        {
            get => Engine.Scene is Level level ? (GravityType)level.Session.GetCounter(Constants.PreviousGravityCounterKey) : GravityType.Normal;
            set => (Engine.Scene as Level)?.Session.SetCounter(Constants.PreviousGravityCounterKey, (int)value);
        }

        public static event Action<GravityType> GravityChanged;

        public static bool ShouldInvert => Settings.Enabled && Gravity == GravityType.Inverted;

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

        #endregion
    }
}