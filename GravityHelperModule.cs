using Celeste.Mod;
using Monocle;
using System;
using Microsoft.Xna.Framework;
using On.Celeste;
using HiresSnow = Celeste.HiresSnow;
using Overworld = Celeste.Overworld;
using Session = Celeste.Session;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public class GravityHelperModule : EverestModule
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

        #region Hook Activation

        public override void Load()
        {
            activateHooks(); // TODO: remove when not testing IL changes
            On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor += OverworldLoader_ctor;
        }

        public override void Unload()
        {
            On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor -= OverworldLoader_ctor;

            deactivateHooks();
        }

        private static bool hooksActive;

        private static void activateHooks()
        {
            if (hooksActive) return;
            hooksActive = true;

            PlayerHooks.Load();
            MiscHooks.Load();
        }

        private static void deactivateHooks()
        {
            if (!hooksActive) return;
            hooksActive = false;

            PlayerHooks.Unload();
            MiscHooks.Unload();
        }

        private void OverworldLoader_ctor(OverworldLoader.orig_ctor orig, Celeste.OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow)
        {
            orig(self, startmode, snow);

            if (startmode != (Overworld.StartMode)(-1))
                deactivateHooks();
        }

        private void LevelLoader_ctor(LevelLoader.orig_ctor orig, Celeste.LevelLoader self, Session session, Vector2? startposition)
        {
            orig(self, session, startposition);

            if (Settings.AllowInAllMaps || session.UsesGravityHelper())
                activateHooks();
            else
                deactivateHooks();
        }

        #endregion

        internal static bool Transitioning;
        internal static bool SolidMoving;

        public void TriggerGravityListeners()
        {
            var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>().ToArray();
            var gravity = Session.Gravity;
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.GravityChanged(gravity);
        }

        public static bool ShouldInvert => Session.Gravity == GravityType.Inverted;

        public static bool ShouldInvertActor(Celeste.Actor actor) => actor is Celeste.Player player
                                                                     && player.StateMachine.State != Celeste.Player.StDreamDash
                                                                     && player.CurrentBooster == null
                                                                     && !SolidMoving && !Transitioning
                                                                     && ShouldInvert;
    }
}