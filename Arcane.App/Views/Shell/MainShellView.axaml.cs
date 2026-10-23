using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia;
using Arcane.App.ViewModels;
using Arcane.App.ViewModels.Tags;
using Arcane.App.Views.Tags;
using System;

namespace Arcane.App.Views.Shell;

public partial class MainShellView : UserControl
{
    public MainShellView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not MainShellViewModel vm) return;

        vm.TagManagerRequested = OpenTagManagerWindow;
        _ = vm.InitializeAsync();
    }

    private void OpenTagManagerWindow(TagManagerViewModel viewModel)
    {
        var window = new Window
        {
            Width = 500,
            Height = 620,
            MinWidth = 440,
            MinHeight = 520,
            Title = "Arcane · Tags",
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Arcane/Assets/Icons/icon.ico"))),
            Background = (IBrush?)Application.Current?.FindResource("ArcaneBackground"),
            Content = new TagManagerView { DataContext = viewModel },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        viewModel.CloseRequested = window.Close;
        window.Closed += (_, _) => viewModel.CloseRequested = null;
        window.Show();
    }
}
