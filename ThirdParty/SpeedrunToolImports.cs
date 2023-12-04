// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using MonoMod.ModInterop;

namespace Celeste.Mod.GravityHelper.ThirdParty;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class SpeedrunToolImports
{
    public static Func<
        Action<Dictionary<Type, Dictionary<string, object>>, Level>, // saveState
        Action<Dictionary<Type, Dictionary<string, object>>, Level>, // loadState
        Action, // clearState
        Action<Level>, // beforeSaveState
        Action<Level>, // beforeLoadState
        Action, // preCloneEntities
        object> RegisterSaveLoadAction;

    public static Action<object> Unregister;
}
