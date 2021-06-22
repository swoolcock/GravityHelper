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
