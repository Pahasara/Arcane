using System;
using Arcane.App.Services;
using Arcane.App.ViewModels;
using Arcane.App.ViewModels.Auth;
using Arcane.App.ViewModels.Entries;
using Arcane.App.ViewModels.Tags;
using Arcane.Core.Data;
using Arcane.Core.Encryption;
using Arcane.Core.Helpers;
using Arcane.Core.Services.Implementations;
using Arcane.Core.Services.Interfaces;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arcane.App;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Build the DI container
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm      = _services.GetRequiredService<MainWindowViewModel>();
            var vaultSvc    = _services.GetRequiredService<IVaultService>();
            var navSvc      = _services.GetRequiredService<INavigationService>();

            // When the vault auto-locks (fires on a ThreadPool thread),
            // dispatch to the UI thread and navigate back to UnlockView
            vaultSvc.VaultLocked += (_, _) =>
                Dispatcher.UIThread.Post(() => navSvc.NavigateTo<UnlockViewModel>());

            // Route to the correct first screen
            if (vaultSvc.VaultExists())
                navSvc.NavigateTo<UnlockViewModel>();
            else
                navSvc.NavigateTo<SetupViewModel>();

            var mainWindow = new MainWindow { DataContext = mainVm };

            // Wire inactivity reset — any pointer move or key press resets the auto-lock timer
            mainWindow.PointerMoved += (_, _) => vaultSvc.ResetInactivityTimer();
            mainWindow.KeyDown      += (_, _) => vaultSvc.ResetInactivityTimer();

            // Lock the vault when the window closes (clears key from memory)
            mainWindow.Closing += (_, _) => vaultSvc.Lock();

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

private static void ConfigureServices(ServiceCollection services)
{
    // --- Core infrastructure ---
    services.AddDbContextFactory<ArcaneDbContext>(options =>
        options.UseSqlite($"Data Source={PathHelper.DatabasePath}"));

    services.AddSingleton<IEncryptionService,    AesEncryptionService>();
    services.AddSingleton<IKeyDerivationService, KeyDerivationService>();
    services.AddSingleton<IVaultService,         VaultService>();

    services.AddSingleton<IEntryService,         EntryService>();
    services.AddSingleton<ITagService,           TagService>();

    // --- App-level ---
    services.AddSingleton<MainWindowViewModel>();
    services.AddSingleton<INavigationService>(sp =>
        new NavigationService(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp));

    // --- ViewModels ---
    services.AddTransient<SetupViewModel>();
    services.AddTransient<UnlockViewModel>();
    services.AddTransient<EntryListViewModel>();
    services.AddTransient<EntryEditorViewModel>();
    services.AddTransient<TagManagerViewModel>();
    services.AddTransient<MainShellViewModel>();
}
}
