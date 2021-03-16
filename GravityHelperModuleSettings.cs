using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GravityHelper
{
    public class GravityHelperModuleSettings : EverestModuleSettings
    {
        public bool AllowInAllMaps { get; set; } = false;

        [DefaultButtonBinding(0, Keys.NumPad1)]
        public ButtonBinding ToggleInvertGravity { get; set; }
    }
}