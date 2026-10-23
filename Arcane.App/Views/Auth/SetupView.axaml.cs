using Avalonia.Controls;
using Avalonia.Input;
using Arcane.App.ViewModels.Auth;

namespace Arcane.App.Views.Auth;

public partial class SetupView : UserControl
{
    public SetupView()
    {
        InitializeComponent();
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e) => SubmitOnEnter(e);
    private void OnConfirmPasswordKeyDown(object? sender, KeyEventArgs e) => SubmitOnEnter(e);

    private void SubmitOnEnter(KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not SetupViewModel vm) return;

        vm.CreateVaultCommand.Execute(null);
        e.Handled = true;
    }
}
