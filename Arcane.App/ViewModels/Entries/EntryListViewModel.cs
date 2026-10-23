using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Models.Entities;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Entries;

/// <summary>
/// Manages the sidebar entry list, including tag filtering.
///
/// _allEntries is the unfiltered master list, loaded once via LoadAsync().
/// Entries is the filtered, bound collection the ListBox displays.
/// Whenever ActiveTagFilters changes, ApplyFilters()
/// re-derives Entries from _allEntries — the master list is never mutated
/// by filtering, only by explicit UpsertEntry/RemoveEntry calls.
/// </summary>
public partial class EntryListViewModel : ViewModelBase
{
    private readonly IEntryService _entryService;
    private readonly ITagService   _tagService;
    private readonly IVaultService _vault;

    private List<EntryDto> _allEntries = [];
    private Guid? _selectedEntryId;

    // Wired by MainShellViewModel
    public Func<Guid, Task>? EntrySelected;
    public Func<Task>?       NewEntryRequested;

    [ObservableProperty]
    private ObservableCollection<EntryDto> _entries = [];

    [ObservableProperty]
    private EntryDto? _selectedEntry;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>All tags available for the filter bar — loaded alongside entries.</summary>
    [ObservableProperty]
    private ObservableCollection<Tag> _availableTags = [];

    /// <summary>Tags currently active as filters. AND logic — an entry must have all of them.</summary>
    [ObservableProperty]
    private ObservableCollection<Tag> _activeTagFilters = [];

    public EntryListViewModel(
        IEntryService entryService,
        ITagService   tagService,
        IVaultService vault)
    {
        _entryService = entryService;
        _tagService   = tagService;
        _vault        = vault;

        // Re-filter whenever the tag filter collection is mutated (add/remove)
        ActiveTagFilters.CollectionChanged += (_, _) => ApplyFilters();
    }

    // Loading
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allEntries = await _entryService.GetAllAsync(_vault.GetKey());

            var tags = await _tagService.GetAllAsync();
            AvailableTags = new ObservableCollection<Tag>(tags);

            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Reloads just the tag list — call after creating/deleting a tag elsewhere.</summary>
    public async Task RefreshTagsAsync()
    {
        var tags = await _tagService.GetAllAsync();
        AvailableTags = new ObservableCollection<Tag>(tags);
    }

    // Filtering
    /// <summary>Toggles a tag filter on/off. Bound to TagChip.Clicked in the filter bar.</summary>
    [RelayCommand]
    private void ToggleTagFilter(Tag tag)
    {
        var existing = ActiveTagFilters.FirstOrDefault(t => t.Id == tag.Id);
        if (existing is not null)
            ActiveTagFilters.Remove(existing);
        else
            ActiveTagFilters.Add(tag);
        // CollectionChanged handler (wired in constructor) triggers ApplyFilters()
    }

    [RelayCommand]
    private void ClearFilters()
    {
        ActiveTagFilters.Clear();
    }

    private void ApplyFilters()
    {
        IEnumerable<EntryDto> filtered = _allEntries;

        if (ActiveTagFilters.Count > 0)
        {
            var filterIds = ActiveTagFilters.Select(t => t.Id).ToHashSet();
            // AND logic: entry must contain every active filter tag
            filtered = filtered.Where(e =>
                filterIds.All(fid => e.Tags.Any(t => t.Id == fid)));
        }

        Entries = new ObservableCollection<EntryDto>(
            filtered.OrderByDescending(e => e.CreatedAt));
    }

    // Selection
    partial void OnSelectedEntryChanged(EntryDto? value)
    {
        if (value is null)
        {
            _selectedEntryId = null;
            return;
        }

        if (_selectedEntryId == value.Id)
            return;

        _selectedEntryId = value.Id;
        if (EntrySelected is not null)
            _ = EntrySelected(value.Id);
    }

    // New entry
    [RelayCommand]
    private async Task NewEntry()
    {
        if (NewEntryRequested is not null)
            await NewEntryRequested();
    }

    // List mutation helpers
    // Called by MainShellViewModel after save/delete. These mutate _allEntries
    // (the master list) and then re-apply filters so Entries stays in sync.
    public void UpsertEntry(EntryDto updated)
    {
        var wasSelected = _selectedEntryId == updated.Id;
        var index = _allEntries.FindIndex(e => e.Id == updated.Id);
        if (index >= 0) _allEntries[index] = updated;
        else            _allEntries.Insert(0, updated);

        ApplyFilters();

        if (wasSelected)
            SelectedEntry = Entries.FirstOrDefault(e => e.Id == updated.Id);
    }

    public void RemoveEntry(Guid id)
    {
        _allEntries.RemoveAll(e => e.Id == id);
        ApplyFilters();
    }

    public void SetSelected(Guid id)
    {
        SelectedEntry = Entries.FirstOrDefault(e => e.Id == id);
    }
}
