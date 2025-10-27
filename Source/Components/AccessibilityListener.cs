// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components;

[Tracked]
public class AccessibilityListener : Component
{
    public Action OnAccessibilityChange { get; set; }

    public AccessibilityListener(Action onAccessibilityChange) : base(false, false)
    {
        OnAccessibilityChange = onAccessibilityChange;
    }
}
