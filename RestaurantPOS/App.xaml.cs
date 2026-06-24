using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using RestaurantPOS.Configuration;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Interfaces;
using RestaurantSynchronizationLib;
using RestaurantSynchronizationLib.Configuration;
using RestaurantPOS.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Net.Http;

namespace RestaurantPOS
{
    public partial class App : Application
    {
        public ServiceProvider Services { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SqliteDb.Initialize();
            var services = new ServiceCollection();

            // Configure logging to file
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RestaurantPOS",
                "Logs");
            
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var logFilePath = Path.Combine(logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");

            services.AddLogging(builder =>
            { // For Visual Studio debug output
                builder.AddProvider(new FileLoggerProvider(logFilePath)); // For file logging
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Order session management - singleton service that manages the current order
            services.AddSingleton<IOrderSession, OrderSessionService>();
           
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPopupService, PopupService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFeatureService, FeatureService>();
            services.AddSingleton<IShiftService, ShiftService>();
            services.AddSingleton<ICashControlService, CashControlService>();
            services.AddSingleton<ISyncEventService, SyncEventService>();
            services.AddSingleton<OrderService>();
            services.AddSingleton<PaymentService>();

            var posSettings = new PosSettings
            {
                ApiBaseAddress = new Uri("https://localhost:7106/"),
                LocationId = Guid.Parse("11111111-1111-1111-1111-111111111111") // Main Branch
            };

            services.AddSingleton(posSettings);
            services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = sp.GetRequiredService<PosSettings>().ApiBaseAddress
            });

            services.AddSingleton<IInventoryService>(sp =>
            {
                var features = sp.GetRequiredService<IFeatureService>();
                var syncEventService = sp.GetRequiredService<ISyncEventService>();
                var settings = sp.GetRequiredService<PosSettings>();
                return features.InventoryModuleEnabled
                    ? new InventoryService(syncEventService, settings)
                    : new NoopInventoryService();
            });

            services.AddSingleton<IInventoryCostService>(sp =>
            {
                var features = sp.GetRequiredService<IFeatureService>();
                var shiftService = sp.GetRequiredService<IShiftService>();
                return features.InventoryCostModuleEnabled
                    ? new InventoryCostService(shiftService)
                    : new NoopInventoryCostService();
            });

            services.AddSingleton<MenuService>();
            services.AddSingleton<IMasterDataSyncService>(sp => new MasterDataSyncService(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<PosSettings>()));

            var syncConfig = new SyncConfiguration
            {
                ApiBaseAddress = "https://localhost:7106",
                DeviceId = DeviceIdProvider.GetDeviceId(),
                DatabaseConnectionString = SqliteDb.ConnectionString,
                UseBatchEndpoint = true,
                BatchSize = 10,
                RequestTimeoutSeconds = 30
            };

            services.AddRestaurantSynchronization(syncConfig);

            //ViewModels
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<MainMenuViewModel>();
            services.AddSingleton<OrderEntryViewModel>();
            services.AddSingleton<PaymentViewModel>();
            services.AddSingleton<InventoryViewModel>();
            services.AddSingleton<AddInventoryViewModel>();
            services.AddSingleton<RemoveInventoryViewModel>();
            services.AddSingleton<BackofficeNavMenuViewModel>();
            services.AddSingleton<EndShiftViewModel>();
            services.AddSingleton<ShiftSummaryViewModel>();
            services.AddSingleton<CashControlViewModel>();
            services.AddSingleton<StepPopupViewModel>();
            services.AddSingleton<TopBarViewModel>();

        


            //Views
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            // Sync master data from server on startup (non-blocking to avoid UI deadlocks/startup hangs).
            var masterDataSync = Services.GetRequiredService<IMasterDataSyncService>();
            _ = masterDataSync.SyncMasterDataAsync();

            // Log startup and file location
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("RestaurantPOS starting up. Logs are being written to: {LogPath}", logFilePath);

            // Make FeatureService available for view bindings
            Current.Resources["FeatureService"] = Services.GetRequiredService<IFeatureService>();

            var nav = Services.GetRequiredService<INavigationService>();
            nav.Navigate<LoginViewModel>();


            var mainWindow = Services.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }
    }
}