// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Components;
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
    public bool MHHUDJTCornerCorrection { get; set; } = true;
    public ButtonBinding ToggleInvertGravity { get; set; }

    // accessibility
    public ArrowSetting SpringArrowType { get; set; }
    public ArrowSetting FieldArrowType { get; set; }
    public bool FieldEdges { get; set; }
    public bool FieldParticles { get; set; }
    public int FieldOpacity { get; set; } = -1;
    public bool HighVisibilityLines { get; set; }
    public ColorSchemeSetting ColorSchemeType { get; set; }
    // public GravityColorScheme CustomColorScheme { get; set; }

    // default to zero to indicate this is the first execution since versioning was introduced
    public int SettingsVersion { get; set; }
    public const int LatestSettingsVersion = 1;

    private static IEnumerable<Tuple<string, TEnum>> getEnumOptions<TEnum>() where TEnum : Enum =>
        Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(v => new Tuple<string, TEnum>(v.ToDialogClean(), v));

    public enum VvvvvvSetting
    {
        [SettingsEnumCase("GRAVITYHELPER_ENUM_VVVVVV_DEFAULT")]
        Default,

        [SettingsEnumCase("GRAVITYHELPER_ENUM_VVVVVV_DISABLED")]
        Disabled,

        [SettingsEnumCase("GRAVITYHELPER_ENUM_VVVVVV_ENABLED")]
        Enabled,
    }

    public enum ForceBoolSetting
    {
        [SettingsEnumCase("GRAVITYHELPER_ENUM_FORCEBOOL_DEFAULT")]
        Default,

        [SettingsEnumCase("GRAVITYHELPER_ENUM_FORCEBOOL_DISABLED")]
        Disabled,

        [SettingsEnumCase("GRAVITYHELPER_ENUM_FORCEBOOL_ENABLED")]
        Enabled,
    }

    public enum ControlSchemeSetting
    {
        [SettingsEnumCase("GRAVITYHELPER_ENUM_CONTROL_SCHEME_ABSOLUTE")]
        Absolute,

        [SettingsEnumCase("GRAVITYHELPER_ENUM_CONTROL_SCHEME_RELATIVE")]
        Relative,
    }

    public enum ArrowSetting
    {
        [SettingsEnumCase("GRAVITYHELPER_ENUM_ARROWSETTING_DEFAULT")]
        Default,
        Small,
        Large,
        None,
    }

    public enum ColorSchemeSetting
    {
        [SettingsEnumCase("GRAVITYHELPER_ENUM_COLORSCHEMESETTING_DEFAULT")]
        Default,
        Classic,
        Colorblind,
        // Custom,
    }

    internal GravityColorScheme GetColorScheme() => ColorSchemeType switch
    {
        ColorSchemeSetting.Default => GravityColorScheme.Classic,
        ColorSchemeSetting.Classic => GravityColorScheme.Classic,
        ColorSchemeSetting.Colorblind => GravityColorScheme.Colorblind,
        // ColorSchemeSetting.Custom => CustomColorScheme,
        _ => throw new ArgumentOutOfRangeException()
    };

    public void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        // AllowInAllMaps
        menu.AddWithDescription(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS"), AllowInAllMaps)
        {
            Disabled = inGame,
            OnValueChange = value => AllowInAllMaps = value,
        }, Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS_SUBTEXT"));

        var ghActive = GravityHelperModule.CurrentHookLevel is GravityHelperModule.HookLevel.GravityHelperMap or GravityHelperModule.HookLevel.Forced;

        if (!inGame || ghActive)
        {
            // Fun Options Subheader
            menu.AddSubHeader("GRAVITYHELPER_MENU_SUBHEADER_FUN_OPTIONS");

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
        }

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

        // Accessibility Subheader
        menu.AddSubHeader("GRAVITYHELPER_MENU_SUBHEADER_ACCESSIBILITY");

        // Spring Arrows
        menu.Add(new ColorChangeTextMenuOption<ArrowSetting>(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_SPRING_ARROWS"))
        {
            Values = getEnumOptions<ArrowSetting>().ToList(),
            Index = (int)SpringArrowType,
            OnValueChange = value =>
            {
                SpringArrowType = value;
                NotifyAccessibilityChange();
            },
        });

        // Field Arrows
        menu.Add(new ColorChangeTextMenuOption<ArrowSetting>(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_FIELD_ARROWS"))
        {
            Values = getEnumOptions<ArrowSetting>().ToList(),
            Index = (int)FieldArrowType,
            OnValueChange = value =>
            {
                FieldArrowType = value;
                NotifyAccessibilityChange();
            },
        });

        // Field Edges
        menu.Add(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_FIELD_EDGES"), FieldEdges, true)
        {
            OnValueChange = value =>
            {
                FieldEdges = value;
                NotifyAccessibilityChange();
            },
        });

        // Field Particles
        menu.Add(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_FIELD_PARTICLES"), FieldParticles, true)
        {
            OnValueChange = value =>
            {
                FieldParticles = value;
                NotifyAccessibilityChange();
            },
        });

        // Field Opacity
        menu.Add(new ColorChangePercent(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_FIELD_OPACITY"), true, FieldOpacity, -1)
        {
            OnValueChange = value =>
            {
                FieldOpacity = value;
                NotifyAccessibilityChange();
            }
        });

        // High Vis Lines
        menu.Add(new ColorChangeOnOff(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_HIGH_VIS_LINES"), HighVisibilityLines, false)
        {
            OnValueChange = value =>
            {
                HighVisibilityLines = value;
                NotifyAccessibilityChange();
            }
        });

        // Colour Scheme
        menu.Add(new ColorChangeTextMenuOption<ColorSchemeSetting>(Dialog.Clean("GRAVITYHELPER_MENU_ACCESS_COLOR_SCHEME"))
        {
            Values = getEnumOptions<ColorSchemeSetting>().ToList(),
            Index = (int)ColorSchemeType,
            OnValueChange = value =>
            {
                ColorSchemeType = value;
                NotifyAccessibilityChange();
            },
        });
    }

    public void MigrateIfRequired()
    {
        // bail if no migration required
        if (SettingsVersion >= LatestSettingsVersion) return;

        // TODO: any required migration

        SettingsVersion = LatestSettingsVersion;
    }

    private static void NotifyAccessibilityChange()
    {
        foreach (AccessibilityListener listener in Engine.Scene.Tracker.GetComponents<AccessibilityListener>())
        {
            listener.OnAccessibilityChange?.Invoke();
        }
    }
}
