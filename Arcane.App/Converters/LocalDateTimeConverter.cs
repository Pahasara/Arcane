using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

public sealed class LocalDateTimeConverter : IValueConverter
{
    public static readonly LocalDateTimeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return string.Empty;

        var utc = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return utc.ToLocalTime();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
