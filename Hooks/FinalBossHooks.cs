// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Extensions;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class FinalBossHooks
{
    private static ILHook hook_FinalBoss_MoveSequence;

    public static void Load()
    {
        hook_FinalBoss_MoveSequence = new ILHook(ReflectionCache.FinalBoss_MoveSequence.GetStateMachineTarget(), FinalBoss_MoveSequence);
    }

    public static void Unload()
    {
        hook_FinalBoss_MoveSequence?.Dispose();
        hook_FinalBoss_MoveSequence = null;
    }

    private static void FinalBoss_MoveSequence(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        // attract Madeline above Center rather than below Center
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4f)))
            throw new HookException("Couldn't find 4f");
        cursor.EmitInvertFloatDelegate();
    });
}