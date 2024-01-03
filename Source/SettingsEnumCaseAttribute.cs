// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

namespace Celeste.Mod.GravityHelper;

[AttributeUsage(AttributeTargets.Field)]
public class SettingsEnumCaseAttribute(string dialogId) : Attribute
{
    public string DialogId { get; } = dialogId;
}

public static class SettingsEnumCaseExtensions
{
    public static string ToDialogClean<TEnum>(this TEnum self) where TEnum : Enum
    {
        var caseName = self.ToString();
        var attr = typeof(TEnum).GetField(caseName)?
            .GetCustomAttributes(false)
            .OfType<SettingsEnumCaseAttribute>()
            .FirstOrDefault();
        return attr != null ? Dialog.Clean(attr.DialogId) : caseName;
    }
}
