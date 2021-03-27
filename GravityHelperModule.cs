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
#if DEBUG
            // force load hooks in debug builds
            activateHooks();
#endif
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
            ThirdPartyHooks.Load();
        }

        private static void deactivateHooks()
        {
            if (!hooksActive) return;
            hooksActive = false;

            PlayerHooks.Unload();
            MiscHooks.Unload();
            ThirdPartyHooks.Unload();
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

        [Command("gravity", "Changes the current gravity (0 = normal, 1 = inverted, 2 = toggle)")]
        private static void CmdSetGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Current gravity state: {Session.Gravity}");
                return;
            }

            if (gravityType < 0 || gravityType > 2) return;

            Session.Gravity = (GravityType) gravityType;
            Engine.Commands.Log($"Current gravity is now: {Session.Gravity}");
        }

        [Command("initial_gravity", "Changes the room entry/spawn gravity (0 = normal, 1 = inverted)")]
        private static void CmdSetInitialGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Initial gravity state: {Session.PreviousGravity}");
                return;
            }

            if (gravityType < 0 || gravityType > 1) return;

            Session.PreviousGravity = (GravityType) gravityType;
            Engine.Commands.Log($"Initial gravity is now: {Session.PreviousGravity}");
        }
    }
}