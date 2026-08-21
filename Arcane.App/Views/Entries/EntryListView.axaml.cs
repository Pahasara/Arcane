using System;
using Arcane.App.Controls;
using Arcane.App.ViewModels.Entries;
using Arcane.Core.Models.Entities;
using Arcane.Core.Models.Enums;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Arcane.App.Views.Entries;

public partial class EntryListView : UserControl
{
    public EntryListView()
    {
        InitializeComponent();
    }

    /// <summary>Wires TagChip.Clicked → ToggleTagFilterCommand once the chip is loaded.</summary>
    private void OnTagChipLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not TagChip { TagData: Tag tag } chip) return;
        if (DataContext is not EntryListViewModel vm) return;

        // Avoid double-subscription if Loaded fires more than once
        chip.Clicked -= ChipClickedHandler;
        chip.Clicked += ChipClickedHandler;

        void ChipClickedHandler(object? _, EventArgs __) =>
            vm.ToggleTagFilterCommand.Execute(tag);
    }

    /// <summary>Mood filter buttons use the standard Button.Click — parse Tag string back to enum.</summary>
    private void OnMoodFilterClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string moodName }) return;
        if (DataContext is not EntryListViewModel vm) return;

        if (Enum.TryParse<MoodLevel>(moodName, out var mood))
            vm.ToggleMoodFilterCommand.Execute(mood);
    }
}
