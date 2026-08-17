using System;
using System.Threading.Tasks;
using Arcane.App.ViewModels.Entries;
using Arcane.Core.Models.DTOs;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Arcane.App.ViewModels;

/// <summary>
/// Top-level shell orchestrator. Owns the sidebar (EntryListViewModel) and
/// the active editor slot (CurrentEditor).
///
/// Responsibility split:
///   EntryListViewModel   — displays the list, notifies shell of selection
///   EntryEditorViewModel — edits one entry, notifies shell after save/delete
///   MainShellViewModel   — creates entries, wires callbacks, syncs both sides
/// </summary>
public partial class MainShellViewModel : ViewModelBase
{
    private readonly IVaultService    _vault;
    private readonly IEntryService    _entryService;
    private readonly IServiceProvider _services;

    /// <summary>Sidebar entry list — always visible. Bound in MainShellView.axaml.</summary>
    public EntryListViewModel EntryList { get; }

    /// <summary>
    /// Currently open editor. null = empty state.
    /// ContentControl in MainShellView resolves EntryEditorView via DataTemplate.
    /// </summary>
    [ObservableProperty]
    private EntryEditorViewModel? _currentEditor;

    public MainShellViewModel(
        IVaultService      vault,
        IEntryService      entryService,
        IServiceProvider   services,
        EntryListViewModel entryList) // DI-resolved, Transient
    {
        _vault        = vault;
        _entryService = entryService;
        _services     = services;
        EntryList     = entryList;

        EntryList.EntrySelected     = OpenEntryAsync;
        EntryList.NewEntryRequested = CreateNewEntryAsync;
    }

    // Initialization
    /// <summary>Called from MainShellView.OnDataContextChanged to populate the list.</summary>
    public async Task InitializeAsync()
    {
        await EntryList.LoadAsync();
    }

    // Open existing entry
    private async Task OpenEntryAsync(Guid entryId)
    {
        CurrentEditor?.Dispose(); // stop previous auto-save timer

        var editor = _services.GetRequiredService<EntryEditorViewModel>();
        editor.OnEntrySaved   = OnEntrySaved;
        editor.OnEntryDeleted = OnEntryDeleted;

        CurrentEditor = editor;
        await editor.LoadAsync(entryId);

        EntryList.SetSelected(entryId);
    }

    // Create new entry
    [RelayCommand]
    private async Task CreateNewEntryAsync()
    {
        var blank = await _entryService.CreateAsync(
            new CreateEntryRequest(
                Title:   "New entry",
                Content: string.Empty,
                Mood:    null,
                TagIds:  []),
            _vault.GetKey());

        EntryList.UpsertEntry(blank);
        await OpenEntryAsync(blank.Id);
    }

    // Callbacks from editor
    private void OnEntrySaved(EntryDto updated)
    {
        EntryList.UpsertEntry(updated);
    }

    private void OnEntryDeleted(Guid id)
    {
        EntryList.RemoveEntry(id);
        CurrentEditor?.Dispose();
        CurrentEditor = null;
    }

    // Lock
    [RelayCommand]
    private void Lock()
    {
        CurrentEditor?.Dispose();
        CurrentEditor = null;
        _vault.Lock();
    }
}
