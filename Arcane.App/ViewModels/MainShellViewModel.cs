using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels;

public partial class MainShellViewModel : ViewModelBase
{
    private readonly IVaultService _vault;

    public MainShellViewModel(IVaultService vault)
    {
        _vault = vault;
    }

    [RelayCommand]
    private void Lock() => _vault.Lock();
}
