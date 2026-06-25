using System;
using System.Linq;
using System.Threading.Tasks;
using Arcane.App.Services;
using Arcane.Core.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcane.App.ViewModels.Auth;

public partial class SetupViewModel(IVaultService vault, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateVaultCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateVaultCommand))]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _passwordStrength = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Toggles between masked and visible password input.</summary>
    [ObservableProperty]
    private bool _showPassword;

    // Called automatically by the source-generated Password setter
    partial void OnPasswordChanged(string value)
    {
        PasswordStrength = ComputeStrength(value);
        ErrorMessage     = null;
    }

    partial void OnConfirmPasswordChanged(string value) => ErrorMessage = null;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateVaultAsync()
    {
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy       = true;
        ErrorMessage = null;

        try
        {
            await vault.SetupVaultAsync(Password);
            // Vault is now created and unlocked in memory
            navigation.NavigateTo<MainShellViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not create vault: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreate() =>
        !IsBusy &&
        Password.Length >= 8 &&
        !string.IsNullOrWhiteSpace(ConfirmPassword);

    // Triggers CreateVaultCommand.NotifyCanExecuteChanged via [NotifyCanExecuteChangedFor]
    partial void OnIsBusyChanged(bool value) => CreateVaultCommand.NotifyCanExecuteChanged();

    private static string ComputeStrength(string password)
    {
        if (password.Length < 8) return "Too short";

        int score = 0;
        if (password.Any(char.IsUpper))               score++;
        if (password.Any(char.IsLower))               score++;
        if (password.Any(char.IsDigit))               score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return score switch
        {
            1 => "Weak",
            2 => "Fair",
            3 => "Good",
            4 => "Strong",
            _ => "Weak"
        };
    }
    
    [RelayCommand]
    private void ToggleShowPassword() => ShowPassword = !ShowPassword;
}
