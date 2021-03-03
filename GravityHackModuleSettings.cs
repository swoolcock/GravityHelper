using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.GravityHack
{
    public class GravityHackModuleSettings : EverestModuleSettings
    {
        public bool Enabled { get; set; } = true;

        [DefaultButtonBinding(0, Keys.NumPad1)]
        public ButtonBinding ToggleInvertGravity { get; set; }
    }
}