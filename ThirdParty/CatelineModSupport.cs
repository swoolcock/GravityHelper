// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Extensions;
using Celeste.Mod.GravityHelper.Hooks;
using Celeste.Mod.GravityHelper.Hooks.Attributes;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    [HookFixture("Cateline")]
    public class CatelineModSupport
    {
        private const string cateline_module_name = "Celeste.Mod.Cateline.CatelineModule";

        [ReflectType("Cateline", cateline_module_name)]
        public static Type CatelineModuleType;

        private static List<Vector2> _tailNodes;

        [HookMethod(cateline_module_name, "Hair_Render")]
        private static void CatelineModule_Hair_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // invert scale Y
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.4f), instr => instr.MatchLdcR4(0.4f)))
                throw new HookException("Couldn't invert scale");
            cursor.EmitInvertFloatDelegate();

            // invert tail scale
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld(CatelineModuleType, "tailScale")))
                throw new HookException("Couldn't invert tailScale");
            cursor.EmitInvertVectorDelegate();
        });

        [HookMethod(cateline_module_name, "Player_Update")]
        private static void CatelineModule_Player_Update(ILContext il) => HookUtils.SafeHook(() =>
        {
            // Cateline's tail implementation is very similar to PlayerHair.AfterUpdate,
            // so we can use pretty much the same hooks
            var cursor = new ILCursor(il);

            void invertAddition()
            {
                cursor.GotoNextAddition();
                cursor.EmitInvertVectorDelegate();
                cursor.Index += 2;
            }

            // this.tailNodes[0] = sprite.RenderPosition + new Vector2(0.0f, -2f * sprite.Scale.Y) + vector2_2;
            cursor.GotoNext(instr => instr.MatchLdfld(CatelineModuleType, "tailNodes"));
            invertAddition();
            invertAddition();

            // Vector2 target = this.tailNodes[0] + new Vector2((float) ((double) -(float) hair.Facing * (double) num3 * 2.0), (float) Math.Sin((double) hair.Wave) * num1) + vector2_1;
            cursor.GotoNext(instr => instr.MatchLdfld(CatelineModuleType, "tailNodes"));
            invertAddition();
            invertAddition();

            // target = this.tailNodes[index] + new Vector2(-(float) hair.Facing * num3, (float) Math.Sin((double) hair.Wave + (double) index * 0.800000011920929) * num1) + vector2_1;
            cursor.GotoNext(
                instr => instr.MatchCallvirt<PlayerHair>("get_Wave"),
                instr => instr.MatchLdloc(9),
                instr => instr.MatchConvR4());
            invertAddition();
            invertAddition();
        });

        [HookMethod(typeof(IL.Celeste.TrailManager), nameof(IL.Celeste.TrailManager.BeforeRender))]
        private static void TrailManager_BeforeRender(ILContext il) => HookUtils.SafeHook(() =>
        {
            // this hook actually fixes an issue with Cateline where the tail is not correctly offset when rendering the dash trail
            var cursor = new ILCursor(il);
            var variable = il.Body.Variables.FirstOrDefault(v => v.VariableType.FullName == typeof(Vector2).FullName);

            var instanceFieldInfo = CatelineModuleType?.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
            var tailNodesFieldInfo = CatelineModuleType?.GetField("tailNodes", BindingFlags.Instance | BindingFlags.NonPublic);

            cursor.GotoNext(instr => instr.MatchCallvirt<Component>(nameof(Component.Render)));
            cursor.Emit(OpCodes.Ldloc, variable);
            cursor.EmitDelegate<Action<Vector2>>(v =>
            {
                if (_tailNodes == null)
                {
                    var instance = instanceFieldInfo.GetValue(null);
                    _tailNodes = (List<Vector2>)tailNodesFieldInfo.GetValue(instance);
                }

                for (int i = 0; i < _tailNodes.Count; i++)
                    _tailNodes[i] += v;
            });

            cursor.Index++;
            cursor.Emit(OpCodes.Ldloc, variable);
            cursor.EmitDelegate<Action<Vector2>>(v =>
            {
                for (int i = 0; i < _tailNodes.Count; i++)
                    _tailNodes[i] -= v;
            });
        });
    }
}
