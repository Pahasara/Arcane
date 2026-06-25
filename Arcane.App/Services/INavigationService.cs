using Arcane.App.ViewModels;

namespace Arcane.App.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}
