using Celeste.Mod;
using Monocle;
using System;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public partial class GravityHelperModule : EverestModule
    {
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