namespace Arcane.Core.Models.Enums;

public enum MoodLevel
{
    Awful   = 1,
    Bad     = 2,
    Neutral = 3,
    Good    = 4,
    Great   = 5
}

public enum ExportFormat
{
    Pdf,
    Markdown,
    PlainText
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}

public enum SortOrder
{
    NewestFirst,
    OldestFirst,
    Alphabetical,
    LastModified
}
