// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class BasicExtensions
    {
        public static int ClampLower(this int self, int min) => Math.Max(self, min);
        public static int ClampUpper(this int self, int max) => Math.Min(self, max);
        public static int Clamp(this int self, int min, int max) => Calc.Clamp(self, min, max);

        public static float ClampLower(this float self, float min) => Math.Max(self, min);
        public static float ClampUpper(this float self, float max) => Math.Min(self, max);
        public static float Clamp(this float self, float min, float max) => Calc.Clamp(self, min, max);
    }
}
