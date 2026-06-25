using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

/// <summary>
/// true  → char(0)  = no masking, characters visible
/// false → '•'      = masked password input
/// Bind TextBox.PasswordChar to ShowPassword via this converter.
/// </summary>
public sealed class BoolToPasswordCharConverter : IValueConverter
{
    public static readonly BoolToPasswordCharConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? char.MinValue : '•';

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
