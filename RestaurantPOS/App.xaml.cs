using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.Models;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Interfaces;
using RestaurantPOS.ViewModels;
using System.Windows;

namespace RestaurantPOS
{
    public partial class App : Application
    {
        public ServiceProvider Services { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var services = new ServiceCollection();
            
            // Order session management - singleton service that manages the current order
            services.AddSingleton<IOrderSession, OrderSessionService>();
           
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPopupService, PopupService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFeatureService, FeatureService>();
            services.AddSingleton<ICashControlService, CashControlService>();

            services.AddSingleton<IInventoryService>(sp =>
            {
                var features = sp.GetRequiredService<IFeatureService>();
                return features.InventoryModuleEnabled
                    ? new InventoryService()
                    : new NoopInventoryService();
            });

            services.AddSingleton<IInventoryCostService>(sp =>
            {
                var features = sp.GetRequiredService<IFeatureService>();
                return features.InventoryCostModuleEnabled
                    ? new InventoryCostService()
                    : new NoopInventoryCostService();
            });

            services.AddSingleton<MenuService>();

            //ViewModels
            services.AddSingleton<MainMenuViewModel>();
            services.AddSingleton<OrderEntryViewModel>();
            services.AddSingleton<PaymentViewModel>();
            services.AddSingleton<InventoryViewModel>();
            services.AddSingleton<AddInventoryViewModel>();
            services.AddSingleton<RemoveInventoryViewModel>();
            services.AddSingleton<BackofficeNavMenuViewModel>();
            services.AddSingleton<EndShiftViewModel>();
            services.AddSingleton<AddCashViewModel>();
            services.AddSingleton<RemoveCashViewModel>();
            services.AddSingleton<TopBarViewModel>();

        


            //Views
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            // Make FeatureService available for view bindings
            Current.Resources["FeatureService"] = Services.GetRequiredService<IFeatureService>();

            var nav = Services.GetRequiredService<INavigationService>();
            nav.Navigate<MainMenuViewModel>();


            var mainWindow = Services.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }
    }
}