using System;
using Arcane.App.Controls;
using Arcane.App.ViewModels.Entries;
using Arcane.Core.Models.Entities;
using Avalonia.Controls;

namespace Arcane.App.Views.Entries;

public partial class EntryEditorView : UserControl
{
    public EntryEditorView()
    {
        InitializeComponent();
    }

    private void OnTagRemoveRequested(object? sender, EventArgs e)
    {
        if (sender is not TagChip { TagData: Tag tag }) return;
        if (DataContext is not EntryEditorViewModel vm) return;

        vm.RemoveTagCommand.Execute(tag);
    }
}
