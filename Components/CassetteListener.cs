// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components
{
    [Tracked]
    public class CassetteListener : Component
    {
        public bool Enabled { get; set; } = true;

        public Action<int, bool> WillToggle { get; set; }
        public Action<int> DidBecomeActive { get; set; }
        public Action DidStop { get; set; }

        public CassetteListener() : base(false, false) {
        }
    }
}
