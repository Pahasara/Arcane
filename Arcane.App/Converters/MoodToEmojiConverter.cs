using System;
using System.Globalization;
using Arcane.Core.Models.Enums;
using Avalonia.Data.Converters;

namespace Arcane.App.Converters;

public sealed class MoodToEmojiConverter : IValueConverter
{
    public static readonly MoodToEmojiConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MoodLevel mood ? mood switch
        {
            MoodLevel.Awful   => "😞",
            MoodLevel.Bad     => "😕",
            MoodLevel.Neutral => "😐",
            MoodLevel.Good    => "🙂",
            MoodLevel.Great   => "😊",
            _                 => string.Empty
        } : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
