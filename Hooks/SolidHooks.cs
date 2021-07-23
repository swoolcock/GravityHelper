using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class SolidHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Solid)} hooks...");

            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Solid)} hooks...");

            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
        }

        private static void Solid_GetPlayerOnTop(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNextSubtraction();
            cursor.EmitInvertVectorDelegate();
        });


        private static void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            if (!GravityHelperModule.ShouldInvert)
            {
                orig(self, move);
                return;
            }

            GravityHelperModule.SolidMoving = true;
            orig(self, move);
            GravityHelperModule.SolidMoving = false;
        }
    }
}