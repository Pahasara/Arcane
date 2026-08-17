using System;
using System.Threading.Tasks;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Enums;
using Arcane.Core.Services.Interfaces;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Entries;

/// <summary>
/// Manages a single diary entry in the editor.
///
/// Auto-save: title/content/mood change -> ResetAutoSaveTimer() -> 2s idle -> SaveAsync().
/// DispatcherTimer fires on the UI thread, so no threading concerns.
///
/// Callbacks (set by MainShellViewModel):
///   OnEntrySaved   — notifies the list to update the entry card
///   OnEntryDeleted — notifies shell to clear the editor slot
/// </summary>
public partial class EntryEditorViewModel(IEntryService entryService, IVaultService vault) : ViewModelBase
{
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

    // Load
    /// <summary>Loads an existing entry. Call immediately after resolving from DI.</summary>
    public async Task LoadAsync(Guid entryId)
    {
        _entryId = entryId;
        IsBusy   = true;

        try
        {
            var entry = await entryService.GetByIdAsync(entryId, vault.GetKey());

            _isLoading   = true; // suppress auto-save while populating fields
            Title        = entry.Title;
            Content      = entry.Content;
            SelectedMood = entry.Mood;
            IsFavorite   = entry.IsFavorite;
            CreatedAt    = entry.CreatedAt;
            _isLoading   = false;

            InitAutoSaveTimer();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Auto-save
    partial void OnTitleChanged(string value)
    {
        if (!_isLoading) ResetAutoSaveTimer();
    }

    partial void OnContentChanged(string value)
    {
        if (!_isLoading) ResetAutoSaveTimer();
    }

    partial void OnSelectedMoodChanged(MoodLevel? value)
    {
        if (!_isLoading) ResetAutoSaveTimer();
    }

    private void InitAutoSaveTimer()
    {
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
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
                Title, Content, SelectedMood, [], IsFavorite);

            var updated = await entryService.UpdateAsync(
                _entryId, request, vault.GetKey());

            SaveStatus = "Saved ✓";
            OnEntrySaved?.Invoke(updated);
        }
        catch (Exception ex)
        {
            SaveStatus   = "Save failed";
            ErrorMessage = ex.Message;
        }
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
            await entryService.DeleteAsync(_entryId);
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
    /// <summary>Stop the auto-save timer when the editor is closed/replaced.</summary>
    public void Dispose()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = null;
    }
}
