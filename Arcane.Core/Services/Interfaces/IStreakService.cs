namespace Arcane.Core.Services.Interfaces;

public interface IStreakService
{
    Task<int>                         GetCurrentStreakAsync();
    Task<int>                         GetLongestStreakAsync();
    Task<int>                         GetTotalEntriesAsync();
    Task<Dictionary<DateOnly, int>>   GetHeatmapDataAsync(int year);
}
