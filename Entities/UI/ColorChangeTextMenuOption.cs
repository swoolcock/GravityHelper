// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Entities.UI;

public class ColorChangeTextMenuOption<T> : TextMenu.Option<T> where T : struct
{
    public T DefaultValue;
    public Color ChangedUnselectedColor = Color.Goldenrod;

    private int _lastFrameIndex;

    public ColorChangeTextMenuOption(string label, T defaultValue = default) : base(label)
    {
        DefaultValue = defaultValue;
    }

    public override void Render(Vector2 position, bool highlighted)
    {
        if (_lastFrameIndex != Index)
        {
            _lastFrameIndex = Index;
            UpdateUnselectedColor();
        }

        base.Render(position, highlighted);
    }

    protected virtual void UpdateUnselectedColor() => UnselectedColor = IsDefaultValue() ? Color.White : ChangedUnselectedColor;

    protected virtual bool IsDefaultValue() => EqualityComparer<T>.Default.Equals(Values[Index].Item2, DefaultValue);
}

public class ColorChangeOnOff : ColorChangeTextMenuOption<bool>
{
    public ColorChangeOnOff(string label, bool on, bool defaultValue = false) : base(label, defaultValue)
    {
        Add(Dialog.Clean("options_off"), false, !on);
        Add(Dialog.Clean("options_on"), true, on);
    }
}
