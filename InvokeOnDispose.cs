// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper
{
    internal struct InvokeOnDispose : IDisposable
    {
        private readonly Action _action;

        public InvokeOnDispose(Action action)
        {
            _action = action;
        }

        public void Dispose() => _action?.Invoke();
    }
}
