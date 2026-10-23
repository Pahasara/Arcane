using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

/// <summary>Displays a Nerd Font outlined or filled favorite glyph.</summary>
public sealed class BoolToStarConverter : IValueConverter
{
    public static readonly BoolToStarConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "" : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
