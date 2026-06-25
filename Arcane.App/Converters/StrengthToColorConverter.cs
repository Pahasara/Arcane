using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Arcane.App.Converters;

/// <summary>
/// Maps password strength label → foreground color for the strength TextBlock.
/// </summary>
public sealed class StrengthToColorConverter : IValueConverter
{
    public static readonly StrengthToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() switch
        {
            "Weak"      => new SolidColorBrush(Color.Parse("#EF4444")),
            "Fair"      => new SolidColorBrush(Color.Parse("#F97316")),
            "Good"      => new SolidColorBrush(Color.Parse("#EAB308")),
            "Strong"    => new SolidColorBrush(Color.Parse("#22C55E")),
            _           => new SolidColorBrush(Color.Parse("#8888AA")), // "Too short" or empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
