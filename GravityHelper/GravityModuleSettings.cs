using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GravityHelper {
    public class GravityModuleSettings : EverestModuleSettings {
        public bool GravityEnabled { get; set; }

        [DefaultButtonBinding(0, Keys.NumPad1)]
        public ButtonBinding ToggleInvertGravity { get; set; }
    }
}
