// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    public class GravityHelperModuleSettings : EverestModuleSettings
    {
        public bool AllowInAllMaps { get; set; }

        public ControlSchemeSetting ControlScheme { get; set; } = ControlSchemeSetting.Absolute;
        public ControlSchemeSetting FeatherControlScheme { get; set; } = ControlSchemeSetting.Absolute;

        public VvvvvvSetting VvvvvvMode { get; set; }
        public VvvvvvSetting VvvvvvFlipSound { get; set; }
        public VvvvvvSetting VvvvvvAllowGrabbing { get; set; }
        public VvvvvvSetting VvvvvvAllowDashing { get; set; }

        public bool MHHUDJTCornerCorrection { get; set; } = true;

        public ButtonBinding ToggleInvertGravity { get; set; }

        public void CreateAllowInAllMapsEntry(TextMenu menu, bool inGame)
        {
            menu.AddWithDescription(new TextMenu.OnOff(Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS"), AllowInAllMaps)
            {
                Disabled = inGame,
                OnValueChange = value => AllowInAllMaps = value,
            }, Dialog.Clean("GRAVITYHELPER_MENU_ALLOW_IN_ALL_MAPS_SUBTEXT"));
        }

        public void CreateMHHUDJTCornerCorrectionEntry(TextMenu menu, bool inGame)
        {
            menu.AddWithDescription(new TextMenu.OnOff(Dialog.Clean("GRAVITYHELPER_MENU_MHH_UDJT_CORNER_CORRECTION"), MHHUDJTCornerCorrection)
            {
                Disabled = inGame,
                OnValueChange = value => MHHUDJTCornerCorrection = value,
            }, Dialog.Clean("GRAVITYHELPER_MENU_MHH_UDJT_CORNER_CORRECTION_SUBTEXT"));
        }

        public void CreateVvvvvvModeEntry(TextMenu menu, bool inGame)
        {
            if (inGame && GravityHelperModule.CurrentHookLevel != GravityHelperModule.HookLevel.Everything) return;
            menu.Add(new TextMenu.Option<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_MODE"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvMode,
                OnValueChange = value =>
                {
                    VvvvvvMode = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>(true)?.Transitioned();
                },
            });
        }

        public void CreateVvvvvvAllowGrabbingEntry(TextMenu menu, bool inGame)
        {
            if (inGame && GravityHelperModule.CurrentHookLevel != GravityHelperModule.HookLevel.Everything) return;
            menu.Add(new TextMenu.Option<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_GRABBING"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowGrabbing,
                OnValueChange = value =>
                {
                    VvvvvvAllowGrabbing = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            });
        }

        public void CreateVvvvvvAllowDashingEntry(TextMenu menu, bool inGame)
        {
            if (inGame && GravityHelperModule.CurrentHookLevel != GravityHelperModule.HookLevel.Everything) return;
            menu.Add(new TextMenu.Option<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_ALLOW_DASHING"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowDashing,
                OnValueChange = value =>
                {
                    VvvvvvAllowDashing = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            });
        }

        public void CreateVvvvvvFlipSoundEntry(TextMenu menu, bool inGame)
        {
            if (inGame && GravityHelperModule.CurrentHookLevel != GravityHelperModule.HookLevel.Everything) return;
            menu.Add(new TextMenu.Option<VvvvvvSetting>(Dialog.Clean("GRAVITYHELPER_MENU_VVVVVV_FLIP_SOUND"))
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvFlipSound,
                OnValueChange = value =>
                {
                    VvvvvvFlipSound = value;
                    (Engine.Scene as Level)?.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                },
            });
        }

        public void CreateControlSchemeEntry(TextMenu menu, bool inGame)
        {
            menu.AddWithDescription(new TextMenu.Option<ControlSchemeSetting>(Dialog.Clean("GRAVITYHELPER_MENU_CONTROL_SCHEME"))
            {
                Values = getEnumOptions<ControlSchemeSetting>().ToList(),
                Index = (int)ControlScheme,
                OnValueChange = value =>
                {
                    ControlScheme = value;
                    FeatherControlScheme = ControlScheme;
                    foreach (var item in menu.Items.OfType<TextMenu.Option<ControlSchemeSetting>>().Where(i => i.Index != (int)value))
                    {
                        item.Index = (int)value;
                    }
                },
            }, Dialog.Clean("GRAVITYHELPER_MENU_CONTROL_SCHEME_SUBTEXT"));
        }

        public void CreateFeatherControlSchemeEntry(TextMenu menu, bool inGame)
        {
            menu.AddWithDescription(new TextMenu.Option<ControlSchemeSetting>(Dialog.Clean("GRAVITYHELPER_MENU_FEATHER_CONTROL_SCHEME"))
            {
                Values = getEnumOptions<ControlSchemeSetting>().ToList(),
                Index = (int)FeatherControlScheme,
                OnValueChange = value => FeatherControlScheme = value,
            }, Dialog.Clean("GRAVITYHELPER_MENU_FEATHER_CONTROL_SCHEME_SUBTEXT"));
        }

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
    }
}
