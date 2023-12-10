// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Entities.UI;
using Celeste.Mod.GravityHelper.Extensions;
using FMOD.Studio;
using Monocle;

namespace Celeste.Mod.GravityHelper;

public class GravityHelperModuleSettings : EverestModuleSettings
{
    public bool AllowInAllMaps { get; set; }
    public ControlSchemeSetting ControlScheme { get; set; } = ControlSchemeSetting.Absolute;
    public ControlSchemeSetting FeatherControlScheme { get; set; } = ControlSchemeSetting.Absolute;
    public VvvvvvSetting VvvvvvMode { get; set; }
    public VvvvvvSetting VvvvvvFlipSound { get; set; }
    public VvvvvvSetting VvvvvvAllowGrabbing { get; set; }
    public VvvvvvSetting VvvvvvAllowDashing { get; set; }
    public bool ReplaceRefills { get; set; }
    public bool MHHUDJTCornerCorrection { get; set; }
    public ButtonBinding ToggleInvertGravity { get; set; }

    // default to zero to indicate this is the first execution since versioning was introduced
    public int SettingsVersion { get; set; }
    public const int LatestSettingsVersion = 1;

    private static IEnumerable<Tuple<string, TEnum>> getEnumOptions<TEnum>() where TEnum : Enum =>
        Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(v => new Tuple<string, TEnum>(v.ToString(), v));

    public enum VvvvvvSetting
    {
        Default,
        Disabled,
        Enabled,
    }

    public enum ControlSchemeSetting
    {
        Absolute,
        Relative,
    }

    public void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        // AllowInAllMaps
        var a = menu.AddWithDescription(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS"), AllowInAllMaps)
        {
            Disabled = inGame,
            OnValueChange = value => AllowInAllMaps = value,
        }, Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS_SUBTEXT"));

        // Fun Options Subheader
        menu.AddSubHeader("GRAVITYHELPER_MENU_SUBHEADER_FUN_OPTIONS");

        // VVVVVV
        var vvvvvvAllowed = !inGame || GravityHelperModule.CurrentHookLevel == GravityHelperModule.HookLevel.Everything;
        if (vvvvvvAllowed)
        {
            // VvvvvvMode
            menu.AddWithDescription(new ColorChangeTextMenuOption<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_MODE"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvMode,
                OnValueChange = value =>
                {
                    VvvvvvMode = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>(true)?.Transitioned();
                },
            }, Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_MODE_SUBTEXT"));

            // VvvvvvAllowGrabbing
            menu.AddWithDescription(new ColorChangeTextMenuOption<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_GRABBING"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowGrabbing,
                OnValueChange = value =>
                {
                    VvvvvvAllowGrabbing = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            }, Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_GRABBING_SUBTEXT"));

            // VvvvvvAllowDashing
            menu.AddWithDescription(new ColorChangeTextMenuOption<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_DASHING"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowDashing,
                OnValueChange = value =>
                {
                    VvvvvvAllowDashing = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            }, Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_DASHING_SUBTEXT"));

            // VvvvvvFlipSound
            menu.AddWithDescription(new ColorChangeTextMenuOption<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_FLIP_SOUND"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvFlipSound,
                OnValueChange = value =>
                {
                    VvvvvvFlipSound = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            }, Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_FLIP_SOUND_SUBTEXT"));
        }

        // ReplaceRefills
        menu.AddWithDescription(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_REPLACE_REFILLS"), ReplaceRefills)
        {
            Disabled = inGame,
            OnValueChange = value => ReplaceRefills = value,
        }, Dialog.Clean("GRAVITYHELPER_MENU_REPLACE_REFILLS_SUBTEXT"));

        // Advanced Subheader
        menu.AddSubHeader("GRAVITYHELPER_MENU_SUBHEADER_ADVANCED");

        // MHHUDJTCornerCorrection
        menu.AddWithDescription(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_MHH_UDJT_CORNER_CORRECTION"), MHHUDJTCornerCorrection, true)
        {
            Disabled = inGame,
            OnValueChange = value => MHHUDJTCornerCorrection = value,
        }, Dialog.Clean("GRAVITYHELPER_MENU_MHH_UDJT_CORNER_CORRECTION_SUBTEXT"));

        // Controls Subheader
        menu.AddSubHeader("GRAVITYHELPER_MENU_SUBHEADER_CONTROLS");

        // ControlScheme
        menu.AddWithDescription(new ColorChangeTextMenuOption<ControlSchemeSetting>(Dialog.Clean("GRAVITYHELPER_MENU_CONTROL_SCHEME"))
        {
            Values = getEnumOptions<ControlSchemeSetting>().ToList(),
            Index = (int)ControlScheme,
            OnValueChange = value =>
            {
                ControlScheme = value;
                FeatherControlScheme = ControlScheme;
                foreach (var item in menu.Items.OfType<ColorChangeTextMenuOption<ControlSchemeSetting>>().Where(i => i.Index != (int)value))
                {
                    item.Index = (int)value;
                }
            },
        }, Dialog.Clean("GRAVITYHELPER_MENU_CONTROL_SCHEME_SUBTEXT"));

        // FeatherControlScheme
        menu.AddWithDescription(new ColorChangeTextMenuOption<ControlSchemeSetting>(Dialog.Clean("GRAVITYHELPER_MENU_FEATHER_CONTROL_SCHEME"))
        {
            Values = getEnumOptions<ControlSchemeSetting>().ToList(),
            Index = (int)FeatherControlScheme,
            OnValueChange = value => FeatherControlScheme = value,
        }, Dialog.Clean("GRAVITYHELPER_MENU_FEATHER_CONTROL_SCHEME_SUBTEXT"));
    }

    public void MigrateIfRequired()
    {
        // bail if no migration required
        if (SettingsVersion >= LatestSettingsVersion) return;

        if (SettingsVersion == 0)
        {
            // version 0 requires us to reset MHH corner correction to the new default of false
            MHHUDJTCornerCorrection = false;
        }

        SettingsVersion = LatestSettingsVersion;
    }
}
