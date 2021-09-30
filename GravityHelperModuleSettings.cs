// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.GravityHelper
{
    public class GravityHelperModuleSettings : EverestModuleSettings
    {
        [SettingInGame(false)]
        public bool AllowInAllMaps { get; set; } = false;

        [DefaultButtonBinding(0, Keys.NumPad1)]
        public ButtonBinding ToggleInvertGravity { get; set; }
    }
}
