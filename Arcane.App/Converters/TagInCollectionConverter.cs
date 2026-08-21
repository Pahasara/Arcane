using System;
using System.Collections;
using System.Globalization;
using Arcane.Core.Models.Entities;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

/// <summary>
/// Checks whether the Tag passed as ConverterParameter exists in the
/// collection passed as the bound value (ActiveTagFilters).
/// Used to drive TagChip.IsToggleActive in the filter bar.
/// </summary>
public sealed class TagInCollectionConverter : IValueConverter
{
    public static readonly TagInCollectionConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable collection || parameter is not Tag targetTag)
            return false;

        foreach (var item in collection)
            if (item is Tag t && t.Id == targetTag.Id)
                return true;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
