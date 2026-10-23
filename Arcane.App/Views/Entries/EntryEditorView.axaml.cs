using System;
using Arcane.App.Controls;
using Arcane.App.ViewModels.Entries;
using Arcane.Core.Models.Entities;
using Avalonia.Controls;
using Avalonia.Input;

namespace Arcane.App.Views.Entries;

public partial class EntryEditorView : UserControl
{
    public EntryEditorView()
    {
        InitializeComponent();
        TitleBox.KeyDown += OnTitleKeyDown;
    }

    private void OnTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not EntryEditorViewModel vm) return;

        vm.SaveNowCommand.Execute(null);
        e.Handled = true;
    }

    private void OnTagRemoveRequested(object? sender, EventArgs e)
    {
        if (sender is not TagChip { TagData: Tag tag }) return;
        if (DataContext is not EntryEditorViewModel vm) return;

        vm.RemoveTagCommand.Execute(tag);
    }
}
