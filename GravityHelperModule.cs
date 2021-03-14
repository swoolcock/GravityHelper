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
        private static readonly MethodInfo virtualJoystickSetValueMethodInfo = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);
        private static readonly MethodInfo updateSpriteMethodInfo = typeof(Player).GetRuntimeMethods().First(m => m.Name == "orig_UpdateSprite");
        private static readonly MethodInfo playerOrigUpdateMethodInfo = typeof(Player).GetMethod(nameof(Player.orig_Update));

        private static readonly object[] virtualJoystickParams = { Vector2.Zero };

        private static void setVirtualJoystickValue(Vector2 value)
        {
            virtualJoystickParams[0] = value;
            virtualJoystickSetValueMethodInfo.Invoke(Input.Aim, virtualJoystickParams);
        }

        public override Type SettingsType => typeof(GravityHelperModuleSettings);
        public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(GravityHelperModuleSession);
        public static GravityHelperModuleSession Session => (GravityHelperModuleSession) Instance._Session;

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

        private static bool transitioning;
        private static bool solidMoving;

        public void TriggerGravityListeners()
        {
            var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>().ToArray();
            var gravity = Session.Gravity;
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.GravityChanged(gravity);
        }

        public static bool ShouldInvert => Settings.Enabled && Session.Gravity == GravityType.Inverted;
   }
}