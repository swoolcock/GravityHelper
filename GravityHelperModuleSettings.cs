// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable UnusedMember.Global

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

        public VvvvvvSetting VvvvvvMode { get; set; }
        public VvvvvvSetting VvvvvvFlipSound { get; set; }
        public VvvvvvSetting VvvvvvAllowGrabbing { get; set; }
        public VvvvvvSetting VvvvvvAllowDashing { get; set; }

        public ButtonBinding ToggleInvertGravity { get; set; }

        public void CreateAllowInAllMapsEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.OnOff("Allow In All Maps", AllowInAllMaps)
            {
                Disabled = inGame,
                OnValueChange = value => AllowInAllMaps = value,
            });
        }

        public void CreateVvvvvvModeEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.Option<VvvvvvSetting>("Vvvvvv Mode")
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvMode,
                OnValueChange = value =>
                {
                    VvvvvvMode = value;
                    if (Engine.Scene is Level level)
                    {
                        var persistent = level.GetPersistentController<VvvvvvGravityController>();
                        if (persistent == null)
                            level.Add(persistent = new VvvvvvGravityController());
                        persistent.Transitioned();
                    }
                }
            });
        }

        public void CreateVvvvvvAllowGrabbingEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.Option<VvvvvvSetting>("Vvvvvv Allow Grabbing")
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowGrabbing,
                OnValueChange = value =>
                {
                    VvvvvvAllowGrabbing = value;
                    if (Engine.Scene is Level level)
                        level.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                }
            });
        }

        public void CreateVvvvvvAllowDashingEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.Option<VvvvvvSetting>("Vvvvvv Allow Dashing")
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvAllowDashing,
                OnValueChange = value =>
                {
                    VvvvvvAllowDashing = value;
                    if (Engine.Scene is Level level)
                        level.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                }
            });
        }

        public void CreateVvvvvvFlipSoundEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.Option<VvvvvvSetting>("Vvvvvv Flip Sound")
            {
                Values = getEnumOptions<VvvvvvSetting>().ToList(),
                Index = (int)VvvvvvFlipSound,
                OnValueChange = value =>
                {
                    VvvvvvFlipSound = value;
                    if (Engine.Scene is Level level)
                        level.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
                }
            });
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
    }
}
