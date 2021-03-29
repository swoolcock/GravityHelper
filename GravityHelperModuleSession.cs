using Celeste.Mod;

namespace GravityHelper
{
    public class GravityHelperModuleSession : EverestModuleSession
    {
        public GravityType InitialGravity { get; set; } = GravityType.Normal;
    }
}