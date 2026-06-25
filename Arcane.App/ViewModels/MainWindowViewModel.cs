using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcane.App.ViewModels;

/// <summary>
/// Holds the currently displayed ViewModel for the main window.
/// The ContentControl in MainWindow.axaml binds to CurrentViewModel.
/// Avalonia resolves the matching View via DataTemplates in App.axaml.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentViewModel;
}
