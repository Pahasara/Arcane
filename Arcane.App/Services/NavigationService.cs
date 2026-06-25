using System;
using Arcane.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Arcane.App.Services;

/// <summary>
/// Sets the active ViewModel on MainWindowViewModel.
/// ViewModels are resolved from DI each time → always a fresh instance.
///
/// All navigation happens on the UI thread. If you call NavigateTo from a
/// background thread (e.g. inside an event handler), dispatch to UIThread first:
///   Dispatcher.UIThread.Post(() => _nav.NavigateTo&lt;UnlockViewModel&gt;());
/// </summary>
public sealed class NavigationService(MainWindowViewModel mainVm, IServiceProvider services) : INavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        mainVm.CurrentViewModel = services.GetRequiredService<TViewModel>();
    }
}
