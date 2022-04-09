// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities.Controllers;

namespace Celeste.Mod.GravityHelper
{
    public class GravityHelperModuleSession : EverestModuleSession
    {
        public GravityType InitialGravity { get; set; } = GravityType.Normal;

        #region VVVVVV Controller

        public bool VvvvvvTrigger { get; set; }
        public VvvvvvMode VvvvvvMode { get; set; } = VvvvvvMode.TriggerBased;
        public string VvvvvvFlipSound { get; set; } = string.Empty;
        public bool VvvvvvDisableGrab { get; set; } = true;
        public bool VvvvvvDisableDash { get; set; } = true;

        public bool IsVvvvvv => VvvvvvMode == VvvvvvMode.TriggerBased && VvvvvvTrigger || VvvvvvMode == VvvvvvMode.On;

        #endregion

        #region Field Controller

        public float ArrowOpacity { get; set; }
        public float FieldOpacity { get; set; }
        public float ParticleOpacity { get; set; }

        #endregion

        #region Sound Controller

        public string NormalSound { get; set; } = string.Empty;
        public string InvertedSound { get; set; } = string.Empty;
        public string ToggleSound { get; set; } = string.Empty;
        public string MusicParam { get; set; } = string.Empty;

        #endregion
    }
}
