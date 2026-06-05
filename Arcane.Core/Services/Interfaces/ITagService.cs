using Arcane.Core.Models.Entities;

namespace Arcane.Core.Services.Interfaces;

public interface ITagService
{
    Task<List<Tag>> GetAllAsync();
    Task<Tag>       CreateAsync(string name, string colorHex);
    Task            RenameAsync(Guid id, string newName);
    Task            DeleteAsync(Guid id);
    Task            AssignTagAsync(Guid entryId, Guid tagId);
    Task            RemoveTagAsync(Guid entryId, Guid tagId);
}
