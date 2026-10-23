using System;
using System.Threading.Tasks;
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
            _ = InitializeDesktopAsync(desktop);

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var services = _services ?? throw new InvalidOperationException(
                "Arcane services were not initialized.");

            await EnsureDatabaseAsync(services);

            var mainVm   = services.GetRequiredService<MainWindowViewModel>();
            var vaultSvc = services.GetRequiredService<IVaultService>();
            var navSvc   = services.GetRequiredService<INavigationService>();

            // When the vault auto-locks (fires on a ThreadPool thread),
            // dispatch to the UI thread and navigate back to UnlockView.
            vaultSvc.VaultLocked += (_, _) =>
                Dispatcher.UIThread.Post(() => navSvc.NavigateTo<UnlockViewModel>());

            if (vaultSvc.VaultExists())
                navSvc.NavigateTo<UnlockViewModel>();
            else
                navSvc.NavigateTo<SetupViewModel>();

            var mainWindow = new MainWindow { DataContext = mainVm };

            // Any pointer move or key press resets the auto-lock timer.
            mainWindow.PointerMoved += (_, _) => vaultSvc.ResetInactivityTimer();
            mainWindow.KeyDown      += (_, _) => vaultSvc.ResetInactivityTimer();

            // Lock the vault when the window closes and clear its key from memory.
            mainWindow.Closing += (_, _) => vaultSvc.Lock();

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Arcane could not initialize its database: {ex}");
            desktop.Shutdown(1);
        }
    }

    private static async Task EnsureDatabaseAsync(IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ArcaneDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // --- Core infrastructure ---
        services.AddDbContextFactory<ArcaneDbContext>(options =>
            options.UseSqlite($"Data Source={PathHelper.DatabasePath}"));

        services.AddSingleton<IEncryptionService,    AesEncryptionService>();
        services.AddSingleton<IKeyDerivationService, KeyDerivationService>();
        services.AddSingleton<IVaultService,         VaultService>();

        services.AddSingleton<IEntryService, EntryService>();
        services.AddSingleton<ITagService,   TagService>();

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
