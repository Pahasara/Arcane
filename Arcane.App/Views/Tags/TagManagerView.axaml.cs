using System;
using Arcane.App.ViewModels.Tags;
using Arcane.Core.Models.Entities;
using Avalonia.Controls;
using Avalonia.Input;

namespace Arcane.App.Views.Tags;

public partial class TagManagerView : UserControl
{
    public TagManagerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TagManagerViewModel vm)
            _ = vm.LoadAsync();
    }

    /// <summary>Double-clicking a tag name starts inline rename mode.</summary>
    private void OnTagNameDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not TagManagerViewModel vm) return;
        if (sender is not Control { DataContext: Tag tag }) return;

        vm.StartRenameCommand.Execute(tag);
    }
}
