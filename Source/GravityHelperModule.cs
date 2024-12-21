// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.ThirdParty;
using Celeste.Mod.GravityHelper.ThirdParty.CelesteNet;
using FMOD.Studio;
using JetBrains.Annotations;
using Monocle;
using MonoMod.ModInterop;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper;

// ReSharper disable InconsistentNaming
public class GravityHelperModule : EverestModule
{
    public override Type SettingsType => typeof(GravityHelperModuleSettings);
    public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(GravityHelperModuleSession);
    public static GravityHelperModuleSession Session => (GravityHelperModuleSession) Instance._Session;

    public static GravityHelperModule Instance { get; private set; }

    internal static PlayerGravityComponent PlayerComponent { get; set; }
    public static bool ShouldInvertPlayer => PlayerComponent?.ShouldInvert ?? false;
    internal static bool ShouldInvertPlayerChecked => PlayerComponent?.ShouldInvertChecked ?? false;
    internal static int OverrideSemaphore;

    private static object _speedrunToolSaveLoadAction;

    internal static bool RequiresHooksForSession(Session session, out bool forceLoad)
    {
        bool requiresHooks(EntityData data)
        {
            if (_ignoreHooks.Contains(data.Name)) return false;
            return data.Name.StartsWith("GravityHelper") || data.Has("_gravityHelper");
        }

        var entityData = session.MapData.Levels.SelectMany(l => l.Entities).FirstOrDefault(requiresHooks);
        forceLoad = entityData?.Name == "GravityHelper/ForceLoadGravityController";
        return entityData != null || session.MapData.Levels.SelectMany(l => l.Triggers).Any(requiresHooks);
    }

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

    internal GravityType? GravityBeforeReload;

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        Settings.CreateModMenuSection(menu, inGame, snapshot);
        CreateModMenuSectionKeyBindings(menu, inGame, snapshot);
    }

    #region Hook Activation

    public override void Load()
    {
        Settings.MigrateIfRequired();

        typeof(GravityHelperExports).ModInterop();
        typeof(SpeedrunToolImports).ModInterop();

        registerSpeedrunTool();

        Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Loading bootstrap hooks...");
        On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
        On.Celeste.OverworldLoader.ctor += OverworldLoader_ctor;
        LevelEnterHooks.Load();

        // always try CelesteNet
        ThirdPartyHooks.ForceLoadType(typeof(CelesteNetModSupport), HookLevel.Forced);
    }

    public override void Unload()
    {
        Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Unloading bootstrap hooks...");
        On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
        On.Celeste.OverworldLoader.ctor -= OverworldLoader_ctor;
        LevelEnterHooks.Unload();

        updateHooks(HookLevel.None);

        // always try CelesteNet
        ThirdPartyHooks.ForceUnloadType(typeof(CelesteNetModSupport));

        unregisterSpeedrunTool();
    }

    internal static HookLevel CurrentHookLevel = HookLevel.None;

    private static void updateHooks(HookLevel requiredHookLevel)
    {
        // if we're already at the right hook level, bail
        if (requiredHookLevel == CurrentHookLevel)
        {
            if (requiredHookLevel is not HookLevel.None)
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Required hooks ({requiredHookLevel}) already applied.");
            return;
        }

        // unload render
        if (CurrentHookLevel is HookLevel.RenderOnly)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Unloading render-only hooks...");
            ForceLoadGravityController.Unload();
        }
        // or unload everything
        else if (CurrentHookLevel is HookLevel.GravityHelperMap or HookLevel.Forced)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Unloading all hooks...");
            ThirdPartyHooks.Unload();

            Everest.Events.AssetReload.OnBeforeReload -= AssetReload_OnBeforeReload;

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
            DashSwitchHooks.Unload();
            DreamBlockHooks.Unload();
            FinalBossHooks.Unload();
            FireBarrierHooks.Unload();
            FloatySpaceBlockHooks.Unload();
            FlyFeatherHooks.Unload();
            GliderHooks.Unload();
            HeartGemHooks.Unload();
            HoldableHooks.Unload();
            IceBlockHooks.Unload();
            InputHooks.Unload();
            JumpThruHooks.Unload();
            LevelHooks.Unload();
            MoveBlockHooks.Unload();
            PlatformHooks.Unload();
            PlayerDeadBodyHooks.Unload();
            PlayerHairHooks.Unload();
            PlayerHooks.Unload();
            PlayerSpriteHooks.Unload();
            PufferHooks.Unload();
            RisingLavaHooks.Unload();
            SandwichLavaHooks.Unload();
            SeekerHooks.Unload();
            SnowballHooks.Unload();
            SolidHooks.Unload();
            SolidTilesHooks.Unload();
            SpikesHooks.Unload();
            SpringHooks.Unload();
            StarJumpBlockHooks.Unload();
            TheoCrystalHooks.Unload();
        }

        CurrentHookLevel = requiredHookLevel;

        // load render
        if (requiredHookLevel is HookLevel.RenderOnly)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Loading render-only hooks...");
            ForceLoadGravityController.Load();
        }
        // or load everything
        else if (requiredHookLevel is HookLevel.GravityHelperMap or HookLevel.Forced)
        {
            Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Loading all hooks...");
            ThirdPartyHooks.Load(requiredHookLevel);

            Everest.Events.AssetReload.OnBeforeReload += AssetReload_OnBeforeReload;

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
            DashSwitchHooks.Load();
            DreamBlockHooks.Load();
            FinalBossHooks.Load();
            FireBarrierHooks.Load();
            FloatySpaceBlockHooks.Load();
            FlyFeatherHooks.Load();
            GliderHooks.Load();
            HeartGemHooks.Load();
            HoldableHooks.Load();
            IceBlockHooks.Load();
            InputHooks.Load();
            JumpThruHooks.Load();
            LevelHooks.Load();
            MoveBlockHooks.Load();
            PlatformHooks.Load();
            PlayerDeadBodyHooks.Load();
            PlayerHairHooks.Load();
            PlayerHooks.Load();
            PlayerSpriteHooks.Load();
            PufferHooks.Load();
            RisingLavaHooks.Load();
            SandwichLavaHooks.Load();
            SeekerHooks.Load();
            SnowballHooks.Load();
            SolidHooks.Load();
            SolidTilesHooks.Load();
            SpikesHooks.Load();
            SpringHooks.Load();
            StarJumpBlockHooks.Load();
            TheoCrystalHooks.Load();
        }
    }

    private static void AssetReload_OnBeforeReload(bool silent)
    {
        Instance.GravityBeforeReload = PlayerComponent?.CurrentGravity;
    }

    private static void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow)
    {
        orig(self, startmode, snow);

        if (startmode != (Overworld.StartMode)(-1))
            updateHooks(HookLevel.None);
    }

    private static void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition)
    {
        ClearStatics();

        orig(self, session, startposition);

        // find out whether the map actually needs hooks
        var requiresHooks = RequiresHooksForSession(session, out var renderOnly);

        // if the player is forcing hooks on
        if (Settings.AllowInAllMaps)
            // enable hooks, but set the hook level based on whether the map actually needed it
            updateHooks(requiresHooks ? HookLevel.GravityHelperMap : HookLevel.Forced);

        // if the player isn't forcing hooks but the map needs it
        else if (requiresHooks)
            // enable hooks, honouring the "render only" request
            updateHooks(renderOnly ? HookLevel.RenderOnly : HookLevel.GravityHelperMap);

        // we don't want hooks
        else
            // turn them off if they're on
            updateHooks(HookLevel.None);
    }

    #endregion

    private static void registerSpeedrunTool()
    {
        if (SpeedrunToolImports.RegisterSaveLoadAction == null) return;

        Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Registering Speedrun Tool actions");

        _speedrunToolSaveLoadAction = SpeedrunToolImports.RegisterSaveLoadAction?.Invoke(
            SaveState,
            LoadState,
            ClearState,
            null,
            null,
            null
        );
    }

    private static void unregisterSpeedrunTool()
    {
        if (_speedrunToolSaveLoadAction == null) return;

        Logger.Log(LogLevel.Info, nameof(GravityHelperModule), "Unregistering Speedrun Tool actions");

        SpeedrunToolImports.Unregister?.Invoke(_speedrunToolSaveLoadAction);
    }

    internal static void SaveState(Dictionary<Type, Dictionary<string, object>> dictionary, Level level)
    {
    }

    internal static void LoadState(Dictionary<Type, Dictionary<string, object>> dictionary, Level level)
    {
        // fix player component
        PlayerComponent = level.Tracker.GetEntity<Player>()?.Get<PlayerGravityComponent>();
    }

    internal static void ClearState()
    {
    }

    [UsedImplicitly]
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

        Engine.Commands.Log($"Current gravity is now: {PlayerComponent?.CurrentGravity ?? GravityType.Normal}");
    }

    [UsedImplicitly]
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

    internal enum HookLevel
    {
        /// <summary>
        /// We don't want any hooks other than bootstrap.
        /// This is the case when no gravity helper entities are present, and AllowAllMaps is false.
        /// </summary>
        None,

        /// <summary>
        /// Just the bare minimum required for upside down rendering.
        /// This is the case when a <see cref="ForceLoadGravityController"/> is present,
        /// no other gravity helper entities are present, and AllowAllMaps is false.
        /// </summary>
        RenderOnly,

        /// <summary>
        /// Load everything.
        /// This is the case when gravity helper entities other than <see cref="ForceLoadGravityController"/>
        /// are present.  AllowAllMaps is irrelevant since this is a gravity helper map.
        /// </summary>
        GravityHelperMap,

        /// <summary>
        /// Load everything, with some exceptions so as to not break maps in specific situations.
        /// This is the case when no gravity helper entities are present, but AllowAllMaps is true.
        /// </summary>
        Forced,
    }

    /// <summary>
    /// List of entity names that are ignored when determining whether we need hooks.
    /// </summary>
    internal static List<string> _ignoreHooks = [
        "GravityHelper/MomentumSpring",
        "GravityHelper/CeilingMomentumSpring",
    ];
}
