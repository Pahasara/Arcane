using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Enums;

namespace Arcane.Core.Services.Interfaces;

public interface IEntryService
{
    Task<List<EntryDto>> GetAllAsync(byte[] masterKey, SortOrder sort = SortOrder.NewestFirst);
    Task<EntryDto>       GetByIdAsync(Guid id, byte[] masterKey);
    Task<EntryDto>       CreateAsync(CreateEntryRequest request, byte[] masterKey);
    Task<EntryDto>       UpdateAsync(Guid id, UpdateEntryRequest request, byte[] masterKey);
    Task                 DeleteAsync(Guid id);
    Task                 ToggleFavoriteAsync(Guid id);
}
