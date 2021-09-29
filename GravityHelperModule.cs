#if DEBUG
#define FORCE_LOAD_HOOKS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Hooks;
using Monocle;

#if !FORCE_LOAD_HOOKS
using Microsoft.Xna.Framework;
#endif

namespace Celeste.Mod.GravityHelper
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
#if DEBUG
            Logger.SetLogLevel(nameof(GravityHelperModule), LogLevel.Verbose);
#else
            Logger.SetLogLevel(nameof(GravityHelperModule), LogLevel.Info);
#endif
        }

        public GravityType? GravityBeforeReload;

        public GravityType Gravity { get; private set; }

        public int GravityRefillCharges { get; set; }

        private static bool showMHHPostcard;
        private static Postcard maxHelpingHandPostcard;

        #region Hook Activation

        public override void Load()
        {
            Logger.Log(nameof(GravityHelperModule), "Loading compatibility check hooks...");
            On.Celeste.LevelEnter.Go += LevelEnter_Go;
            On.Celeste.LevelEnter.Routine += LevelEnter_Routine;
            On.Celeste.LevelEnter.BeforeRender += LevelEnter_BeforeRender;

#if FORCE_LOAD_HOOKS
            // force load hooks in debug builds
            Logger.Log(nameof(GravityHelperModule), "Force loading hooks due to debug build...");
            activateHooks();
#else
            Logger.Log(nameof(GravityHelperModule), $"Loading bootstrap hooks...");
            On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor += OverworldLoader_ctor;
#endif
        }

        public override void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), "Unloading compatibility check hooks...");
            On.Celeste.LevelEnter.Go -= LevelEnter_Go;
            On.Celeste.LevelEnter.Routine -= LevelEnter_Routine;
            On.Celeste.LevelEnter.BeforeRender -= LevelEnter_BeforeRender;

#if !FORCE_LOAD_HOOKS
            Logger.Log(nameof(GravityHelperModule), $"Unloading bootstrap hooks...");
            On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor -= OverworldLoader_ctor;
#endif
            deactivateHooks();
        }

        public override void Initialize()
        {
            base.Initialize();
            ThirdPartyHooks.Load();
        }

        private static bool hooksActive;

        private static void activateHooks()
        {
            if (hooksActive) return;
            hooksActive = true;

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel += AssetReloadHelper_ReloadLevel;

            ActorHooks.Load();
            BounceBlockHooks.Load();
            BumperHooks.Load();
            FlyFeatherHooks.Load();
            JumpThruHooks.Load();
            LevelHooks.Load();
            PlayerDeadBodyHooks.Load();
            PlayerHairHooks.Load();
            PlayerHooks.Load();
            PufferHooks.Load();
            SeekerHooks.Load();
            SolidHooks.Load();
            SolidTilesHooks.Load();
            SpikesHooks.Load();
            SpringHooks.Load();
            TrailManagerHooks.Load();
        }

        private static void deactivateHooks()
        {
            if (!hooksActive) return;
            hooksActive = false;

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel -= AssetReloadHelper_ReloadLevel;

            ActorHooks.Unload();
            BounceBlockHooks.Unload();
            BumperHooks.Unload();
            FlyFeatherHooks.Unload();
            JumpThruHooks.Unload();
            LevelHooks.Unload();
            PlayerDeadBodyHooks.Unload();
            PlayerHairHooks.Unload();
            PlayerHooks.Unload();
            PufferHooks.Unload();
            SeekerHooks.Unload();
            SolidHooks.Unload();
            SolidTilesHooks.Unload();
            SpikesHooks.Unload();
            SpringHooks.Unload();
            TrailManagerHooks.Unload();
        }

        private static void LevelEnter_Go(On.Celeste.LevelEnter.orig_Go orig, Session session, bool fromsavedata)
        {
            checkConflicts(session);
            orig(session, fromsavedata);
        }

        private static void AssetReloadHelper_ReloadLevel(On.Celeste.Mod.AssetReloadHelper.orig_ReloadLevel orig)
        {
            Instance.GravityBeforeReload = Instance.Gravity;
            orig();
        }

        private static void LevelEnter_BeforeRender(On.Celeste.LevelEnter.orig_BeforeRender orig, LevelEnter self)
        {
            orig(self);
            maxHelpingHandPostcard?.BeforeRender();
        }

        private static IEnumerator LevelEnter_Routine(On.Celeste.LevelEnter.orig_Routine orig, LevelEnter self)
        {
            if (showMHHPostcard)
            {
                showMHHPostcard = false;
                self.Add(maxHelpingHandPostcard = new Postcard(Dialog.Get("POSTCARD_GRAVITYHELPER_MHH_UDJT"),
                    "event:/ui/main/postcard_csides_in", "event:/ui/main/postcard_csides_out"));
                yield return maxHelpingHandPostcard.DisplayRoutine();
                maxHelpingHandPostcard = null;
            }

            yield return new SwapImmediately(orig(self));
        }

#if !FORCE_LOAD_HOOKS
        private void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow)
        {
            orig(self, startmode, snow);

            if (startmode != (Overworld.StartMode)(-1))
                deactivateHooks();
        }

        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition)
        {
            orig(self, session, startposition);

            if (Settings.AllowInAllMaps || EntityHookChecker.IsHookRequiredForSession(session))
                activateHooks();
            else
                deactivateHooks();
        }
#endif

        #endregion

        internal static bool Transitioning;
        internal static bool SolidMoving;

        public static void SaveState(Dictionary<string, object> state)
        {
            state[nameof(Transitioning)] = Transitioning;
            state[nameof(SolidMoving)] = SolidMoving;
            state[nameof(GravityRefillCharges)] = Instance.GravityRefillCharges;
            state[nameof(Gravity)] = Instance.Gravity;
        }

        public static void LoadState(Dictionary<string, object> state)
        {
            if (state[nameof(Transitioning)] is bool transitioning)
                Transitioning = transitioning;
            if (state[nameof(SolidMoving)] is bool solidMoving)
                SolidMoving = solidMoving;
            if (state[nameof(GravityRefillCharges)] is int gravityRefillCharges)
                Instance.GravityRefillCharges = gravityRefillCharges;
            if (state[nameof(Gravity)] is GravityType gravity)
                Instance.SetGravity(new GravityChangeArgs(gravity, playerTriggered: false));

            // fix upside down jumpthru tracking
            foreach (var udjt in Engine.Scene.Entities.Where(e => e is UpsideDownJumpThru))
                ((UpsideDownJumpThru)udjt).EnsureCorrectTracking();
        }

        public void SetGravity(GravityType newValue, float momentumMultiplier = 1f, bool playerTriggered = true) =>
            SetGravity(new GravityChangeArgs(newValue, momentumMultiplier, playerTriggered));

        public void SetGravity(GravityChangeArgs args)
        {
            if (args.SourceValue == GravityType.None)
                return;

            args.OldValue = Gravity;
            args.NewValue = args.SourceValue == GravityType.Toggle ? args.OldValue.Opposite() : args.SourceValue;
            Gravity = args.NewValue;
            TriggerGravityListeners(args);
        }

        public void TriggerGravityListeners(GravityChangeArgs args)
        {
            var gravityListeners = Engine.Scene.Tracker.GetComponents<GravityListener>().ToArray();
            foreach (Component component in gravityListeners)
                (component as GravityListener)?.OnGravityChanged(args);
        }

        public static bool ShouldInvert => Instance.Gravity == GravityType.Inverted;

        public static bool ShouldInvertActor(Actor actor)
        {
            if (SolidMoving || Transitioning)
                return false;

            if (actor is Player player)
                return player.StateMachine.State != Player.StDreamDash &&
                       player.CurrentBooster == null &&
                       ShouldInvert;

            return actor.IsInverted();
        }

        [Command("gravity", "Changes the current gravity (0 = normal, 1 = inverted, 2 = toggle)")]
        private static void CmdSetGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Current gravity state: {Instance.Gravity}");
                return;
            }

            if (gravityType < 0 || gravityType > 2) return;

            Instance.SetGravity((GravityType) gravityType);
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

        private static void checkConflicts(Session session)
        {
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata {Name = "MaxHelpingHand"}))
                checkMHHConflicts(session);
        }

        private static void checkMHHConflicts(Session session)
        {
            if (AreaData.Areas.Count <= session.Area.ID ||
                AreaData.Areas[session.Area.ID].Mode.Length <= (int)session.Area.Mode ||
                AreaData.Areas[session.Area.ID].Mode[(int)session.Area.Mode] == null)
                return;

            showMHHPostcard = session.MapData.Levels.Exists(levelData =>
                levelData.Entities.Exists(e => e.Name == "MaxHelpingHand/UpsideDownJumpThru"));
        }
    }
}
