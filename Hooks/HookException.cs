// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.Hooks
{
    internal class HookException : Exception
    {
        public HookException(string message) : base(message)
        {
        }
    }
}
