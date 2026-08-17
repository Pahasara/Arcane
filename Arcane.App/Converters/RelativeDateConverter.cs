using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

/// <summary>
/// UTC DateTime → human-readable relative string.
/// "Just now" / "5m ago" / "3h ago" / "Yesterday" / "3 days ago" / "Mar 2025"
/// </summary>
public sealed class RelativeDateConverter : IValueConverter
{
    public static readonly RelativeDateConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime utc) return string.Empty;

        var diff = DateTime.UtcNow - utc;

        return diff.TotalSeconds switch
        {
            < 60        => "Just now",
            < 3_600     => $"{(int)diff.TotalMinutes}m ago",
            < 86_400    => $"{(int)diff.TotalHours}h ago",
            < 172_800   => "Yesterday",
            < 604_800   => $"{(int)diff.TotalDays} days ago",
            < 2_592_000 => $"{(int)(diff.TotalDays / 7)} weeks ago",
            _           => utc.ToLocalTime().ToString("MMM yyyy", culture)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
