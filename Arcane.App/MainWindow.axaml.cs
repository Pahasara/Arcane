using Avalonia.Controls;
using Avalonia;
using System;
using System.ComponentModel;
using Arcane.App.ViewModels;
using Arcane.App.ViewModels.Auth;

namespace Arcane.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateWindowSize();
        Opened += (_, _) => CenterOnScreen();
        UpdateWindowSize();
    }

    private void UpdateWindowSize()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        vm.PropertyChanged -= OnViewModelPropertyChanged;
        vm.PropertyChanged += OnViewModelPropertyChanged;
        ApplyWindowSize(vm.CurrentViewModel);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentViewModel) &&
            sender is MainWindowViewModel vm)
            ApplyWindowSize(vm.CurrentViewModel);
    }

    private void ApplyWindowSize(ViewModelBase? viewModel)
    {
        var (width, height, minWidth, minHeight) = viewModel switch
        {
            SetupViewModel => (430, 600, 400, 560),
            UnlockViewModel => (430, 420, 400, 400),
            _ => (1120, 720, 820, 560)
        };

        Width = width;
        Height = height;
        MinWidth = minWidth;
        MinHeight = minHeight;

        if (IsVisible)
            CenterOnScreen();
    }

    private void CenterOnScreen()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        var scale = RenderScaling;
        var width = (int)(ClientSize.Width * scale);
        var height = (int)(ClientSize.Height * scale);
        var area = screen.WorkingArea;

        Position = new PixelPoint(
            area.X + Math.Max(0, (area.Width - width) / 2),
            area.Y + Math.Max(0, (area.Height - height) / 2));
    }
}