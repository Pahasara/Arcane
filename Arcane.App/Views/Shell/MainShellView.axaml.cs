using System;
using Arcane.App.ViewModels;
using Avalonia.Controls;

namespace Arcane.App.Views.Shell;

public partial class MainShellView : UserControl
{
    public MainShellView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load entries once the view is attached and DataContext is
    /// guaranteed to be MainShellViewModel.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainShellViewModel vm)
            _ = vm.InitializeAsync();
    }
}
