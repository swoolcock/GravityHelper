using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GravityHelper {
    public class GravityModuleSettings : EverestModuleSettings
    {
        public bool Enabled { get; set; } = true;

        [DefaultButtonBinding(0, Keys.NumPad1)]
        public ButtonBinding ToggleInvertGravity { get; set; }
    }
}
