using Celeste.Mod;

namespace GravityHelper
{
    public class GravityHelperModuleSession : EverestModuleSession
    {
        private GravityType gravity = GravityType.Normal;
        public GravityType Gravity
        {
            get => gravity;
            set
            {
                gravity = value == GravityType.Toggle ? gravity.Opposite() : value;
                GravityHelperModule.Instance.TriggerGravityListeners();
            }
        }

        public GravityType PreviousGravity { get; set; } = GravityType.Normal;
    }
}