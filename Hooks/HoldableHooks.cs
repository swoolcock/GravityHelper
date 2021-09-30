// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class HoldableHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(HoldableHooks)} hooks...");

            On.Celeste.Holdable.Added += Holdable_Added;
            IL.Celeste.Holdable.Pickup += Holdable_Pickup;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(HoldableHooks)} hooks...");

            On.Celeste.Holdable.Added -= Holdable_Added;
            IL.Celeste.Holdable.Pickup -= Holdable_Pickup;
        }

        private static void Holdable_Pickup(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchCallvirt<Holdable>("set_Holder")))
                throw new HookException("Couldn't find set_Holder.");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Holdable>>(self =>
            {
                if (self.Entity.Get<GravityHoldable>() is { } gravityHoldable)
                    gravityHoldable.Inverted = GravityHelperModule.ShouldInvert;
            });
        });

        private static void Holdable_Added(On.Celeste.Holdable.orig_Added orig, Holdable self, Entity entity)
        {
            orig(self, entity);

            if (entity.Get<GravityHoldable>() != null) return;

            switch (entity)
            {
                case TheoCrystal theoCrystal:
                {
                    var data = new DynData<TheoCrystal>(theoCrystal);
                    entity.Add(new GravityHoldable
                    {
                        UpdateEntityVisuals = inverted =>
                        {
                            var sprite = data.Get<Sprite>("sprite");
                            sprite.Scale.Y = inverted ? -1 : 1;
                        },
                    });
                    break;
                }

                default:
                    entity.Add(new GravityHoldable());
                    break;
            }
        }
    }
}
