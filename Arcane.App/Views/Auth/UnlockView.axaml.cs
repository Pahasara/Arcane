using Avalonia.Controls;
using Avalonia.Input;
using Arcane.App.ViewModels.Auth;

namespace Arcane.App.Views.Auth;

public partial class UnlockView : UserControl
{
    public UnlockView()
    {
        InitializeComponent();
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not UnlockViewModel vm) return;

        vm.UnlockCommand.Execute(null);
        e.Handled = true;
    }
}
