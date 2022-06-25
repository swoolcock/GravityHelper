// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable UnusedMember.Global

namespace Celeste.Mod.GravityHelper
{
    public class GravityHelperModuleSettings : EverestModuleSettings
    {
        public bool AllowInAllMaps { get; set; }

        public ButtonBinding ToggleInvertGravity { get; set; }

        public void CreateAllowInAllMapsEntry(TextMenu menu, bool inGame)
        {
            menu.Add(new TextMenu.OnOff("Allow In All Maps", AllowInAllMaps)
            {
                Disabled = inGame,
                OnValueChange = value => AllowInAllMaps = value,
            });
        }
    }
}
