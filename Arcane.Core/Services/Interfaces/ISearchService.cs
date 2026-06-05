using Arcane.Core.Models.DTOs;

namespace Arcane.Core.Services.Interfaces;

public interface ISearchService
{
    Task<List<EntryDto>> SearchAsync(string query, byte[] masterKey);
    Task<List<EntryDto>> SearchInTagAsync(string query, Guid tagId, byte[] masterKey);
}
