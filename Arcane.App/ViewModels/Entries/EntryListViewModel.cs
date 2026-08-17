using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Entries;

/// <summary>
/// Manages the sidebar entry list.
/// Callbacks are set by MainShellViewModel after construction so the two
/// ViewModels stay decoupled — EntryListViewModel doesn't know MainShellViewModel exists.
/// </summary>
public partial class EntryListViewModel(IEntryService entryService, IVaultService vault) : ViewModelBase
{
    // Wired by MainShellViewModel
    public Func<Guid, Task>? EntrySelected;
    public Func<Task>?       NewEntryRequested;

    [ObservableProperty]
    private ObservableCollection<EntryDto> _entries = [];

    [ObservableProperty]
    private EntryDto? _selectedEntry;

    [ObservableProperty]
    private bool _isLoading;

    // Loading
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var all = await entryService.GetAllAsync(vault.GetKey());
            Entries = new ObservableCollection<EntryDto>(all);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Selection
    partial void OnSelectedEntryChanged(EntryDto? value)
    {
        if (value is not null && EntrySelected is not null)
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
    // Called by MainShellViewModel after save/delete to keep the list in sync
    public void UpsertEntry(EntryDto updated)
    {
        var index = IndexOf(updated.Id);
        if (index >= 0)
        {
            Entries[index] = updated;
            if (index != 0)
            {
                Entries.RemoveAt(index);
                Entries.Insert(0, updated);
            }
        }
        else
        {
            Entries.Insert(0, updated);
        }
    }

    public void RemoveEntry(Guid id)
    {
        var index = IndexOf(id);
        if (index >= 0) Entries.RemoveAt(index);
    }

    public void SetSelected(Guid id)
    {
        SelectedEntry = Entries.FirstOrDefault(e => e.Id == id);
    }

    private int IndexOf(Guid id)
    {
        for (int i = 0; i < Entries.Count; i++)
            if (Entries[i].Id == id) return i;
        return -1;
    }
}
