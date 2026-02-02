using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.Services.Interfaces;
using RestaurantPOS.ViewModels;
using System;

namespace RestaurantPOS.Services
{
    public partial class PopupService : ObservableObject, IPopupService
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private BaseViewModel currentPopupViewModel;

        [ObservableProperty]
        private bool isPopupOpen;

        public event Action? PopupViewModelChanged;

        public PopupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Show<TViewModel>() where TViewModel : BaseViewModel
        {
            CurrentPopupViewModel = _serviceProvider.GetRequiredService<TViewModel>();
            IsPopupOpen = true;
            PopupViewModelChanged?.Invoke();
        }

        public void Close()
        {
            IsPopupOpen = false;
            CurrentPopupViewModel = null;
            PopupViewModelChanged?.Invoke();
        }
    }
}
