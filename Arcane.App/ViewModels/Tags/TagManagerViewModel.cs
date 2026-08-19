using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Arcane.Core.Models.Entities;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Tags;

/// <summary>
/// Manages the full tag list: create, rename, delete.
/// Also exposes a fixed palette of 12 pastel presets for the color picker,
/// plus a free-text hex input for custom colors.
/// </summary>
public partial class TagManagerViewModel(ITagService tagService) : ViewModelBase
{
    /// <summary>12 preset pastel colors shown as swatches when creating a tag.</summary>
    public static readonly string[] ColorPresets =
    [
        "#7C3AED", "#2563EB", "#0891B2", "#16A34A",
        "#EAB308", "#F97316", "#EF4444", "#DB2777",
        "#8B5CF6", "#06B6D4", "#10B981", "#F59E0B"
    ];

    [ObservableProperty]
    private ObservableCollection<Tag> _tags = [];

    [ObservableProperty]
    private string _newTagName = string.Empty;

    [ObservableProperty]
    private string _newTagColor = ColorPresets[0];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    // Inline rename state — set when the user double-clicks a tag
    [ObservableProperty]
    private Tag? _editingTag;

    [ObservableProperty]
    private string _editingName = string.Empty;

    // Load
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var all = await tagService.GetAllAsync();
            Tags = new ObservableCollection<Tag>(all);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Create
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateTag()
    {
        ErrorMessage = null;
        try
        {
            var tag = await tagService.CreateAsync(NewTagName, NewTagColor);
            Tags.Add(tag);

            // Reset the input row
            NewTagName  = string.Empty;
            NewTagColor = ColorPresets[0];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(NewTagName);

    partial void OnNewTagNameChanged(string value) => CreateTagCommand.NotifyCanExecuteChanged();

    // Rename (inline)
    [RelayCommand]
    private void StartRename(Tag tag)
    {
        EditingTag  = tag;
        EditingName = tag.Name;
    }

    [RelayCommand]
    private async Task ConfirmRename()
    {
        if (EditingTag is null || string.IsNullOrWhiteSpace(EditingName)) return;

        try
        {
            await tagService.RenameAsync(EditingTag.Id, EditingName);

            // Update the in-memory copy so the UI reflects the change immediately
            var index = Tags.IndexOf(EditingTag);
            if (index >= 0)
            {
                EditingTag.Name = EditingName.Trim();
                Tags[index]     = EditingTag; // force refresh of the bound ItemTemplate
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            EditingTag  = null;
            EditingName = string.Empty;
        }
    }

    [RelayCommand]
    private void CancelRename()
    {
        EditingTag  = null;
        EditingName = string.Empty;
    }
    
    [RelayCommand]
    private void SelectColor(string colorHex) => NewTagColor = colorHex;

    // Delete
    [RelayCommand]
    private async Task DeleteTag(Tag tag)
    {
        try
        {
            await tagService.DeleteAsync(tag.Id);
            Tags.Remove(tag);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
