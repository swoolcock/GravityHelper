// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    internal struct PushRandomDisposable : IDisposable
    {
        public PushRandomDisposable(int seed)
        {
            Calc.PushRandom(seed);
        }

        public PushRandomDisposable(Scene scene)
        {
            Calc.PushRandom(AreaData.Get(scene ?? Engine.Scene)?.ID ?? 0);
        }

        public void Dispose()
        {
            Calc.PopRandom();
        }
    }
}
