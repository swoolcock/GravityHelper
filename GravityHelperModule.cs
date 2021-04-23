using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;

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

        public GravityType? GravityBeforeReload;

        private GravityType gravity = GravityType.Normal;
        public GravityType Gravity
        {
            get => gravity;
            set
            {
                if (value == GravityType.None) return;
                gravity = value == GravityType.Toggle ? gravity.Opposite() : value;
                TriggerGravityListeners();
            }
        }

        public int GravityRefillCharges { get; set; }

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

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel += AssetReloadHelper_ReloadLevel;

            PlayerHooks.Load();
            MiscHooks.Load();
            ThirdPartyHooks.Load();
        }

        private static void deactivateHooks()
        {
            if (!hooksActive) return;
            hooksActive = false;

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel -= AssetReloadHelper_ReloadLevel;

            PlayerHooks.Unload();
            MiscHooks.Unload();
            ThirdPartyHooks.Unload();
        }

        private static void AssetReloadHelper_ReloadLevel(On.Celeste.Mod.AssetReloadHelper.orig_ReloadLevel orig)
        {
            Instance.GravityBeforeReload = Instance.Gravity;
            orig();
        }

        private void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow)
        {
            orig(self, startmode, snow);

            if (startmode != (Overworld.StartMode)(-1))
                deactivateHooks();
        }

        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition)
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
            var gravity = Instance.Gravity;
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.GravityChanged(gravity);
        }

        public static bool ShouldInvert => Instance.Gravity == GravityType.Inverted;

        public static bool ShouldInvertActor(Actor actor) => actor is Player player
                                                             && player.StateMachine.State != Player.StDreamDash
                                                             && player.CurrentBooster == null
                                                             && !SolidMoving && !Transitioning
                                                             && ShouldInvert;

        [Command("gravity", "Changes the current gravity (0 = normal, 1 = inverted, 2 = toggle)")]
        private static void CmdSetGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Current gravity state: {Instance.Gravity}");
                return;
            }

            if (gravityType < 0 || gravityType > 2) return;

            Instance.Gravity = (GravityType) gravityType;
            Engine.Commands.Log($"Current gravity is now: {Instance.Gravity}");
        }

        [Command("initial_gravity", "Changes the room entry/spawn gravity (0 = normal, 1 = inverted)")]
        private static void CmdSetInitialGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Initial gravity state: {Session.InitialGravity}");
                return;
            }

            if (gravityType < 0 || gravityType > 1) return;

            Session.InitialGravity = (GravityType) gravityType;
            Engine.Commands.Log($"Initial gravity is now: {Session.InitialGravity}");
        }
    }
}