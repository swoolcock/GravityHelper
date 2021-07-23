// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SpringHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Spring)} hooks...");
            On.Celeste.Spring.OnCollide += Spring_OnCollide;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Spring)} hooks...");
            On.Celeste.Spring.OnCollide -= Spring_OnCollide;
        }

        private static void Spring_OnCollide(On.Celeste.Spring.orig_OnCollide orig, Spring self, Player player)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, player);
                return;
            }

            // check copied from orig
            if (player.StateMachine.State == Player.StDreamDash || !self.GetPlayerCanUse())
                return;

            // if we hit a floor spring while inverted, flip gravity back to normal
            if (self.Orientation == Spring.Orientations.Floor)
                GravityHelperModule.Instance.SetGravity(GravityType.Normal);

            orig(self, player);
        }
    }
}
