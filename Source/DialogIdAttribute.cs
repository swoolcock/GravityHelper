// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;

namespace Celeste.Mod.GravityHelper;

[AttributeUsage(AttributeTargets.Field)]
public class DialogIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}

public static class DialogIdExtensions
{
    public static string ToDialogClean<TEnum>(this TEnum self) where TEnum : Enum
    {
        var caseName = self.ToString();
        var attr = typeof(TEnum).GetField(caseName)?
            .GetCustomAttributes(false)
            .OfType<DialogIdAttribute>()
            .FirstOrDefault();
        return attr != null ? Dialog.Clean(attr.Id) : caseName;
    }
}
