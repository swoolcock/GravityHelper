// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if DEBUG
#define FORCE_LOAD_HOOKS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Components;
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

        public static GravityComponent PlayerComponent { get; internal set; }
        public static bool ShouldInvertPlayer => PlayerComponent?.ShouldInvert ?? false;
        public static bool ShouldInvertPlayerChecked => PlayerComponent?.ShouldInvertChecked ?? false;

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

        public int GravityRefillCharges { get; set; }

        #region Hook Activation

        public override void Load()
        {
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
            GliderHooks.Load();
            HoldableHooks.Load();
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
            TheoCrystalHooks.Load();
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
            GliderHooks.Unload();
            HoldableHooks.Unload();
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
            TheoCrystalHooks.Unload();
            TrailManagerHooks.Unload();
        }

        private static void AssetReloadHelper_ReloadLevel(On.Celeste.Mod.AssetReloadHelper.orig_ReloadLevel orig)
        {
            Instance.GravityBeforeReload = PlayerComponent?.CurrentGravity;
            orig();
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
        internal static bool JumpThruMoving;

        public static void SaveState(Dictionary<string, object> state, Level level)
        {
            state[nameof(Transitioning)] = Transitioning;
            state[nameof(SolidMoving)] = SolidMoving;
            state[nameof(JumpThruMoving)] = JumpThruMoving;
            state[nameof(GravityRefillCharges)] = Instance.GravityRefillCharges;
        }

        public static void LoadState(Dictionary<string, object> state, Level level)
        {
            if (state[nameof(Transitioning)] is bool transitioning)
                Transitioning = transitioning;
            if (state[nameof(SolidMoving)] is bool solidMoving)
                SolidMoving = solidMoving;
            if (state[nameof(JumpThruMoving)] is bool jumpThruMoving)
                JumpThruMoving = jumpThruMoving;
            if (state[nameof(GravityRefillCharges)] is int gravityRefillCharges)
                Instance.GravityRefillCharges = gravityRefillCharges;

            // fix upside down jumpthru tracking
            foreach (var udjt in Engine.Scene.Entities.Where(e => e is UpsideDownJumpThru))
                ((UpsideDownJumpThru)udjt).EnsureCorrectTracking();
        }

        public static void InvalidateRun()
        {
            if (Engine.Scene is Level level)
                level.Session.StartedFromBeginning = false;
        }

        [Command("gravity", "Changes the current gravity (0 = normal, 1 = inverted, 2 = toggle)")]
        private static void CmdSetGravity(int gravityType = -1)
        {
            if (gravityType == -1)
            {
                Engine.Commands.Log($"Current gravity state: {PlayerComponent?.CurrentGravity ?? GravityType.Normal}");
                return;
            }

            if (gravityType < 0 || gravityType > 2) return;

            PlayerComponent?.SetGravity((GravityType) gravityType);
            InvalidateRun();

            Engine.Commands.Log($"Current gravity is now: {PlayerComponent?.CurrentGravity ?? GravityType.Normal}");
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
            InvalidateRun();

            Engine.Commands.Log($"Initial gravity is now: {Session.InitialGravity}");
        }
    }
}
