// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if DEBUG
#define FORCE_LOAD_HOOKS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.ThirdParty;
using Monocle;
using MonoMod.ModInterop;

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

        public static PlayerGravityComponent PlayerComponent { get; internal set; }
        public static bool ShouldInvertPlayer => PlayerComponent?.ShouldInvert ?? false;
        public static bool ShouldInvertPlayerChecked => PlayerComponent?.ShouldInvertChecked ?? false;
        internal static int OverrideSemaphore = 0;

        public static bool AreHooksRequiredForSession(Session session) =>
            session.MapData?.Levels?
                .Any(level => (level.Triggers?.Any(IsHookRequiredForEntityData) ?? false) ||
                    (level.Entities?.Any(IsHookRequiredForEntityData) ?? false)) ?? false;

        public static bool IsHookRequiredForEntityData(EntityData data) => data.Name.StartsWith("GravityHelper");

        internal static void ClearStatics()
        {
            // make sure we clear some static things
            OverrideSemaphore = 0;
            PlayerComponent = null;
        }

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

        #region Hook Activation

        public override void Load()
        {
            typeof(GravityHelperExports).ModInterop();

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
#if FORCE_LOAD_HOOKS
            ThirdPartyHooks.Load();
#endif
        }

        internal static bool HooksActive;

        private static void activateHooks()
        {
            if (HooksActive) return;
            HooksActive = true;

#if !FORCE_LOAD_HOOKS
            ThirdPartyHooks.Load();
#endif

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel += AssetReloadHelper_ReloadLevel;

            BaseGravityController.LoadHooks();

            ActorHooks.Load();
            AngryOshiroHooks.Load();
            BadelineBoostHooks.Load();
            BadelineDummyHooks.Load();
            BadelineOldsiteHooks.Load();
            BoosterHooks.Load();
            BounceBlockHooks.Load();
            BumperHooks.Load();
            CassetteBlockManagerHooks.Load();
            CrushBlockHooks.Load();
            DreamBlockHooks.Load();
            FinalBossHooks.Load();
            FloatySpaceBlockHooks.Load();
            FlyFeatherHooks.Load();
            GliderHooks.Load();
            HeartGemHooks.Load();
            HoldableHooks.Load();
            InputHooks.Load();
            JumpThruHooks.Load();
            LevelHooks.Load();
            LevelEnterHooks.Load();
            MoveBlockHooks.Load();
            PlayerDeadBodyHooks.Load();
            PlayerHairHooks.Load();
            PlayerHooks.Load();
            PlayerSpriteHooks.Load();
            PufferHooks.Load();
            SeekerHooks.Load();
            SnowballHooks.Load();
            SolidHooks.Load();
            SolidTilesHooks.Load();
            SpikesHooks.Load();
            SpringHooks.Load();
            StarJumpBlockHooks.Load();
            TheoCrystalHooks.Load();
        }

        private static void deactivateHooks()
        {
            if (!HooksActive) return;
            HooksActive = false;

#if !FORCE_LOAD_HOOKS
            ThirdPartyHooks.Unload();
#endif

            On.Celeste.Mod.AssetReloadHelper.ReloadLevel -= AssetReloadHelper_ReloadLevel;

            BaseGravityController.UnloadHooks();

            ActorHooks.Unload();
            AngryOshiroHooks.Unload();
            BadelineBoostHooks.Unload();
            BadelineDummyHooks.Unload();
            BadelineOldsiteHooks.Unload();
            BoosterHooks.Unload();
            BounceBlockHooks.Unload();
            BumperHooks.Unload();
            CassetteBlockManagerHooks.Unload();
            CrushBlockHooks.Unload();
            DreamBlockHooks.Unload();
            FinalBossHooks.Unload();
            FloatySpaceBlockHooks.Unload();
            FlyFeatherHooks.Unload();
            GliderHooks.Unload();
            HeartGemHooks.Unload();
            HoldableHooks.Unload();
            InputHooks.Unload();
            JumpThruHooks.Unload();
            LevelHooks.Unload();
            LevelEnterHooks.Unload();
            MoveBlockHooks.Unload();
            PlayerDeadBodyHooks.Unload();
            PlayerHairHooks.Unload();
            PlayerHooks.Unload();
            PlayerSpriteHooks.Unload();
            PufferHooks.Unload();
            SeekerHooks.Unload();
            SnowballHooks.Unload();
            SolidHooks.Unload();
            SolidTilesHooks.Unload();
            SpikesHooks.Unload();
            SpringHooks.Unload();
            StarJumpBlockHooks.Unload();
            TheoCrystalHooks.Unload();
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
            ClearStatics();

            orig(self, session, startposition);

            if (Settings.AllowInAllMaps || AreHooksRequiredForSession(session))
                activateHooks();
            else
                deactivateHooks();
        }
#endif

        #endregion

        public static void SaveState(Dictionary<string, object> state, Level level)
        {
        }

        public static void LoadState(Dictionary<string, object> state, Level level)
        {
            // fix player component
            PlayerComponent = level.Tracker.GetEntity<Player>()?.Get<PlayerGravityComponent>();
        }

        public static void InvalidateRun()
        {
            // NOTE: getting rid of this for now, if people submit invalid runs that's on them

            // if (Engine.Scene is Level level)
            //     level.Session.StartedFromBeginning = false;
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
