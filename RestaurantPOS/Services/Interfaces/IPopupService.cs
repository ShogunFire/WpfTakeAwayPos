using RestaurantPOS.ViewModels;
using System;

namespace RestaurantPOS.Services.Interfaces
{
    public interface IPopupService
    {
        void Show<TViewModel>() where TViewModel : BaseViewModel;
        void Close();
        BaseViewModel CurrentPopupViewModel { get; }
        bool IsPopupOpen { get; }
        event Action? PopupViewModelChanged;
    }
}
