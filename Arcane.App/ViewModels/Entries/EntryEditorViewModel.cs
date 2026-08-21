using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Entities;
using Arcane.Core.Models.Enums;
using Arcane.Core.Services.Interfaces;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Entries;

/// <summary>
/// Manages a single diary entry in the editor, including tag assignment.
///
/// Auto-save: title/content/mood/tags change -> ResetAutoSaveTimer() -> 2s idle -> SaveAsync().
///
/// Callbacks (set by MainShellViewModel):
///   OnEntrySaved   — notifies the list to update the entry card
///   OnEntryDeleted — notifies shell to clear the editor slot
/// </summary>
public partial class EntryEditorViewModel : ViewModelBase
{
    private readonly IEntryService _entryService;
    private readonly ITagService   _tagService;
    private readonly IVaultService _vault;

    private Guid             _entryId;
    private DispatcherTimer? _autoSaveTimer;
    private bool             _isLoading;

    public Action<EntryDto>? OnEntrySaved;
    public Action<Guid>?     OnEntryDeleted;

    [ObservableProperty] private string     _title             = string.Empty;
    [ObservableProperty] private string     _content           = string.Empty;
    [ObservableProperty] private MoodLevel? _selectedMood;
    [ObservableProperty] private bool       _isFavorite;
    [ObservableProperty] private DateTime   _createdAt;
    [ObservableProperty] private string     _saveStatus        = string.Empty;
    [ObservableProperty] private bool       _isBusy;
    [ObservableProperty] private bool       _showDeleteConfirm;
    [ObservableProperty] private string?    _errorMessage;

    /// <summary>Tags currently assigned to this entry.</summary>
    [ObservableProperty]
    private ObservableCollection<Tag> _assignedTags = [];

    /// <summary>All tags in the vault, for the "add tag" picker popup.</summary>
    [ObservableProperty]
    private ObservableCollection<Tag> _allTags = [];

    /// <summary>Controls visibility of the add-tag picker popup.</summary>
    [ObservableProperty]
    private bool _showTagPicker;

    /// <summary>Tags not yet assigned — computed for the picker list.</summary>
    public IEnumerable<Tag> UnassignedTags =>
        AllTags.Where(t => AssignedTags.All(a => a.Id != t.Id));

    public EntryEditorViewModel(
        IEntryService entryService,
        ITagService   tagService,
        IVaultService vault)
    {
        _entryService = entryService;
        _tagService   = tagService;
        _vault        = vault;
    }

    // Load
    public async Task LoadAsync(Guid entryId)
    {
        _entryId = entryId;
        IsBusy   = true;

        try
        {
            var entry = await _entryService.GetByIdAsync(entryId, _vault.GetKey());

            _isLoading    = true;
            Title         = entry.Title;
            Content       = entry.Content;
            SelectedMood  = entry.Mood;
            IsFavorite    = entry.IsFavorite;
            CreatedAt     = entry.CreatedAt;
            AssignedTags  = new ObservableCollection<Tag>(entry.Tags);
            _isLoading    = false;

            var allTags = await _tagService.GetAllAsync();
            AllTags = new ObservableCollection<Tag>(allTags);

            InitAutoSaveTimer();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Auto-save
    partial void OnTitleChanged(string value)        { if (!_isLoading) ResetAutoSaveTimer(); }
    partial void OnContentChanged(string value)       { if (!_isLoading) ResetAutoSaveTimer(); }
    partial void OnSelectedMoodChanged(MoodLevel? value) { if (!_isLoading) ResetAutoSaveTimer(); }

    private void InitAutoSaveTimer()
    {
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            _autoSaveTimer.Stop();
            await SaveAsync();
        };
    }

    private void ResetAutoSaveTimer()
    {
        SaveStatus = string.Empty;
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    // Save
    private async Task SaveAsync()
    {
        if (_entryId == Guid.Empty) return;

        SaveStatus = "Saving…";
        try
        {
            var request = new UpdateEntryRequest(
                Title, Content, SelectedMood,
                AssignedTags.Select(t => t.Id).ToList(),
                IsFavorite);

            var updated = await _entryService.UpdateAsync(
                _entryId, request, _vault.GetKey());

            SaveStatus = "Saved ✓";
            OnEntrySaved?.Invoke(updated);
        }
        catch (Exception ex)
        {
            SaveStatus   = "Save failed";
            ErrorMessage = ex.Message;
        }
    }

    // Tag assignment
    [RelayCommand]
    private void OpenTagPicker() => ShowTagPicker = true;

    [RelayCommand]
    private void CloseTagPicker() => ShowTagPicker = false;

    [RelayCommand]
    private void AddTag(Tag tag)
    {
        if (AssignedTags.Any(t => t.Id == tag.Id)) return;

        AssignedTags.Add(tag);
        OnPropertyChanged(nameof(UnassignedTags));
        ShowTagPicker = false;
        ResetAutoSaveTimer(); // persist immediately via the normal auto-save flow
    }

    [RelayCommand]
    private void RemoveTag(Tag tag)
    {
        var existing = AssignedTags.FirstOrDefault(t => t.Id == tag.Id);
        if (existing is null) return;

        AssignedTags.Remove(existing);
        OnPropertyChanged(nameof(UnassignedTags));
        ResetAutoSaveTimer();
    }

    // Favorite toggle
    [RelayCommand]
    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        ResetAutoSaveTimer();
    }

    // Delete
    [RelayCommand]
    private void RequestDelete() => ShowDeleteConfirm = true;

    [RelayCommand]
    private void CancelDelete() => ShowDeleteConfirm = false;

    [RelayCommand]
    private async Task ConfirmDelete()
    {
        IsBusy = true;
        try
        {
            _autoSaveTimer?.Stop();
            await _entryService.DeleteAsync(_entryId);
            OnEntryDeleted?.Invoke(_entryId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not delete: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Cleanup
    public void Dispose()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = null;
    }
}
