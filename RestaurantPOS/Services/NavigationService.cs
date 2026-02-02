using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.Services.Interfaces;
using RestaurantPOS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace RestaurantPOS.Services
{
    public partial class NavigationService : ObservableObject, INavigationService
    {

        IServiceProvider serviceProvider;

        [ObservableProperty]
        private BaseViewModel currentViewModel;

        [ObservableProperty]
        private bool showHomeButton;

        // Event for subscribers
        public event Action? CurrentViewModelChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {

            this.serviceProvider = serviceProvider;

        }


        public void Navigate<TViewModel>() where TViewModel : BaseViewModel
        {
            CurrentViewModel = serviceProvider.GetRequiredService<TViewModel>();
            
            // Show home button for all views except MainMenuViewModel
            ShowHomeButton = !(CurrentViewModel is MainMenuViewModel);

            // Fire the event whenever CurrentViewModel changes
            CurrentViewModelChanged?.Invoke();
        }

        public void GoHome()
        {
            CurrentViewModel = serviceProvider.GetRequiredService<MainMenuViewModel>();
            ShowHomeButton = false;
            CurrentViewModelChanged?.Invoke();
        }

        [RelayCommand]
        private async Task GoHomeAsync()
        {
            var current = CurrentViewModel;

            if (current is INavigationGuard guard)
            {
                if (!await guard.CanNavigateAwayAsync())
                    return;
            }

            GoHome();
        }

      
    }
}
