// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.GravityHelper.Entities.Controllers;

namespace Celeste.Mod.GravityHelper;

public class GravityHelperModuleSession : EverestModuleSession
{
    public GravityType InitialGravity { get; set; } = GravityType.Normal;

    #region VVVVVV Controller

    public bool VvvvvvTrigger { get; set; }
    public bool DisableGrab { get; set; }
    public VvvvvvMode VvvvvvMode { get; set; }

    #endregion
}