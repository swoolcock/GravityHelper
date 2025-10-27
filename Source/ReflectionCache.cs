// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper;

// ReSharper disable InconsistentNaming
internal static class ReflectionCache
{
    private static List<Type> _loadableTypes;
    public static IEnumerable<Type> LoadableTypes => _loadableTypes ??= typeof(ReflectionCache).Assembly.GetTypesSafe().ToList();

    // ReSharper disable once UnusedMember.Global
    // public static Type GetTypeByName(string name) =>
    //     AppDomain.CurrentDomain.GetAssemblies().Reverse().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

    public static Type GetModdedTypeByName(string module, string name)
    {
        var mod = Everest.Modules.FirstOrDefault(m => m.Metadata.Name == module);
        return mod?.GetType().Assembly.GetType(name);
    }

    #region Player

    public static readonly MethodInfo Player_CanUnDuck = typeof(Player).GetMethod("get_CanUnDuck", BindingFlags.Instance | BindingFlags.Public);
    public static readonly MethodInfo Player_CameraTarget = typeof(Player).GetMethod("get_CameraTarget", BindingFlags.Instance | BindingFlags.Public);
    public static readonly MethodInfo Player_DashCoroutine = typeof(Player).GetMethod("DashCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Player_IntroJumpCoroutine = typeof(Player).GetMethod("IntroJumpCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Player_OrigUpdateSprite = typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Player_OrigWallJump = typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Player_OrigUpdate = typeof(Player).GetMethod(nameof(Player.orig_Update));
    public static readonly MethodInfo Player_PickupCoroutine = typeof(Player).GetMethod("PickupCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);

    #endregion

    #region Misc

    public static readonly MethodInfo FinalBoss_MoveSequence = typeof(FinalBoss).GetMethod("MoveSequence", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo FlyFeather_CollectRoutine = typeof(FlyFeather).GetMethod("CollectRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Booster_BoostRoutine = typeof(Booster).GetMethod("BoostRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo BadelineBoost_BoostRoutine = typeof(BadelineBoost).GetMethod("BoostRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Level_OrigTransitionRoutine = typeof(Level).GetMethod("orig_TransitionRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo PlayerDeadBody_DeathRoutine = typeof(PlayerDeadBody).GetMethod("DeathRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly MethodInfo Seeker_RegenerateCoroutine = typeof(Seeker).GetMethod("RegenerateCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);

    #endregion

    #region Optional Dependencies

    public static Type MaddieHelpingHandUpsideDownJumpThruType { get; private set; }
    public static Type MaddieHelpingHandGroupedTriggerSpikesType { get; private set; }
    public static Type FancyFallingBlockType { get; private set; }
    public static MethodInfo FancyFallingBlock_SurfaceSoundIndexAt { get; private set; }
    public static Type MaddyCrownModuleType { get; private set; }
    public static Type FrostHelperCustomSpringType { get; private set; }
    public static Type OutbackHelperPortalType { get; private set; }
    public static Type CommunalHelperConnectedSolidType { get; private set; }
    public static Type CommunalHelperTimedTriggerSpikesType { get; private set; }
    public static Type ExtendedVariantsDashTrailAllTheTimeType { get; private set; }
    public static Type ExtendedVariantsJumpIndicatorType { get; private set; }
    public static Type ExtendedVariantsDashCountIndicatorType { get; private set; }
    public static Type ExtendedVariantsJumpCountType { get; private set; }
    public static MethodInfo ExtendedVariantsJumpCountGetJumpBufferMethodInfo { get; private set; }
    public static MethodInfo ExtendedVariantsJumpCountSetJumpCountMethodInfo { get; private set; }
    public static Type StaminaMeterSmallStaminaMeterDisplayType { get; private set; }
    public static Type CelesteNetGhostType { get; private set; }
    public static Type CelesteNetGhostNameTagType { get; private set; }
    public static Type CelesteNetGhostEmoteType { get; private set; }
    public static Type BounceHelperModuleType { get; private set; }
    public static MethodInfo JackalHelperCardinalBumper_CardinalLaunch { get; private set; }
    public static Type JackalHelperCardinalBumperType { get; private set; }

    public static void LoadThirdPartyTypes()
    {
        FancyFallingBlockType = GetModdedTypeByName("FancyTileEntities", "Celeste.Mod.FancyTileEntities.FancyFallingBlock");
        FancyFallingBlock_SurfaceSoundIndexAt = FancyFallingBlockType?.GetMethod("SurfaceSoundIndexAt", BindingFlags.Instance | BindingFlags.NonPublic);
        MaddyCrownModuleType = GetModdedTypeByName("MaddyCrown", "Celeste.Mod.MaddyCrown.MaddyCrownModule");
        MaddieHelpingHandUpsideDownJumpThruType = GetModdedTypeByName("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru");
        MaddieHelpingHandGroupedTriggerSpikesType = GetModdedTypeByName("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.GroupedTriggerSpikes");
        FrostHelperCustomSpringType = GetModdedTypeByName("FrostHelper", "FrostHelper.CustomSpring");
        OutbackHelperPortalType = GetModdedTypeByName("OutbackHelper", "Celeste.Mod.OutbackHelper.Portal");
        CommunalHelperConnectedSolidType = GetModdedTypeByName("CommunalHelper", "Celeste.Mod.CommunalHelper.ConnectedSolid");
        CommunalHelperTimedTriggerSpikesType = GetModdedTypeByName("CommunalHelper", "Celeste.Mod.CommunalHelper.Entities.TimedTriggerSpikes");
        ExtendedVariantsDashTrailAllTheTimeType = GetModdedTypeByName("ExtendedVariantMode", "ExtendedVariants.Variants.DashTrailAllTheTime");
        ExtendedVariantsJumpIndicatorType = GetModdedTypeByName("ExtendedVariantMode", "ExtendedVariants.Entities.JumpIndicator");
        ExtendedVariantsJumpIndicatorType = GetModdedTypeByName("ExtendedVariantMode", "ExtendedVariants.Entities.DashCountIndicator");
        ExtendedVariantsJumpCountType = GetModdedTypeByName("ExtendedVariantMode", "ExtendedVariants.Variants.JumpCount");
        ExtendedVariantsJumpCountGetJumpBufferMethodInfo = ExtendedVariantsJumpCountType?.GetMethod("GetJumpBuffer", BindingFlags.Public | BindingFlags.Static);
        ExtendedVariantsJumpCountSetJumpCountMethodInfo = ExtendedVariantsJumpCountType?.GetMethod("SetJumpCount", BindingFlags.Public | BindingFlags.Static);
        StaminaMeterSmallStaminaMeterDisplayType = GetModdedTypeByName("StaminaMeter", "Celeste.Mod.StaminaMeter.SmallStaminaMeterDisplay");
        CelesteNetGhostType = GetModdedTypeByName("CelesteNet.Client", "Celeste.Mod.CelesteNet.Client.Entities.Ghost");
        CelesteNetGhostNameTagType = GetModdedTypeByName("CelesteNet.Client", "Celeste.Mod.CelesteNet.Client.Entities.GhostNameTag");
        CelesteNetGhostEmoteType = GetModdedTypeByName("CelesteNet.Client", "Celeste.Mod.CelesteNet.Client.Entities.GhostEmote");
        BounceHelperModuleType = GetModdedTypeByName("BounceHelper", "Celeste.Mod.BounceHelper.BounceHelperModule");
        JackalHelperCardinalBumperType = GetModdedTypeByName("JackalHelper", "Celeste.Mod.JackalHelper.Entities.CardinalBumper");
        JackalHelperCardinalBumper_CardinalLaunch = JackalHelperCardinalBumperType?.GetMethod("CardinalLaunch", BindingFlags.Instance | BindingFlags.Public);
    }

    #endregion

    #region Reflection Extensions

    public static int CallFancyFallingBlockSurfaceSoundIndexAt(this FallingBlock fallingBlock, Vector2 readPosition)
    {
        if (FancyFallingBlock_SurfaceSoundIndexAt == null) return -1;
        return (int) FancyFallingBlock_SurfaceSoundIndexAt.Invoke(fallingBlock, [readPosition]);
    }

    #endregion
}
