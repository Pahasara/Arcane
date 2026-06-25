using System;
using System.Linq;
using System.Threading.Tasks;
using Arcane.App.Services;
using Arcane.Core.Data;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace Arcane.App.ViewModels.Auth;

public partial class UnlockViewModel : ViewModelBase
{
    private readonly IVaultService _vault;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private bool _shakeError;

    /// <summary>Hint shown below the input: "Vault created March 2025"</summary>
    [ObservableProperty]
    private string _vaultHint = string.Empty;

    public UnlockViewModel(
        IVaultService vault,
        INavigationService navigation,
        IDbContextFactory<ArcaneDbContext> dbFactory)
    {
        _vault      = vault;
        _navigation = navigation;
        LoadVaultHint(dbFactory);
    }

    private void LoadVaultHint(IDbContextFactory<ArcaneDbContext> dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var profile = db.VaultProfiles.FirstOrDefault();
        if (profile is not null)
            VaultHint = $"Vault created {profile.CreatedAt:MMMM yyyy}";
    }

    partial void OnPasswordChanged(string value) => ErrorMessage = null;

    [RelayCommand]
    private void ToggleShowPassword() => ShowPassword = !ShowPassword;

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private async Task UnlockAsync()
    {
        IsBusy       = true;
        ErrorMessage = null;
        ShakeError   = false;

        try
        {
            var success = await _vault.UnlockAsync(Password);

            if (success)
            {
                _navigation.NavigateTo<MainShellViewModel>();
            }
            else
            {
                ErrorMessage = "Incorrect password. Try again.";

                // Trigger shake animation, then reset so it can fire again next attempt
                ShakeError = true;
                await Task.Delay(500);
                ShakeError = false;

                Password = string.Empty; // Clear the field on wrong attempt
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanUnlock() => !IsBusy && !string.IsNullOrWhiteSpace(Password);

    partial void OnIsBusyChanged(bool value) => UnlockCommand.NotifyCanExecuteChanged();
}
