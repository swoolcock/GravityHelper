// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

// ReSharper disable UnusedMember.Global

namespace Celeste.Mod.GravityHelper.Extensions;

internal static class BasicExtensions
{
    public static int ClampLower(this int self, int min) => Math.Max(self, min);
    public static int ClampUpper(this int self, int max) => Math.Min(self, max);
    public static int Clamp(this int self, int min, int max) => Calc.Clamp(self, min, max);
    public static int Mod(this int self, int divisor) => ((self % divisor) + divisor) % divisor;

    public static float ClampLower(this float self, float min) => Math.Max(self, min);
    public static float ClampUpper(this float self, float max) => Math.Min(self, max);
    public static float Clamp(this float self, float min, float max) => Calc.Clamp(self, min, max);
    public static float Clamp01(this float self) => Calc.Clamp(self, 0f, 1f);
    public static float Mod(this float self, float divisor) => ((self % divisor) + divisor) % divisor;

    public static TItem AddWithDescription<TItem>(this TextMenu self, TItem item, string description)
        where TItem : TextMenu.Item
    {
        self.Add(item);
        item.AddDescription(self, description);
        return item;
    }

    public static void AddSubHeader(this TextMenu self, string subHeader)
        => self.Add(new TextMenu.SubHeader(subHeader.DialogCleanOrNull() ?? subHeader, false));

    public static void PlayIfAvailable(this Sprite self, string id, bool restart = false, bool randomizeFrame = false)
    {
        if (self.Has(id)) self.Play(id, restart, randomizeFrame);
    }
}
