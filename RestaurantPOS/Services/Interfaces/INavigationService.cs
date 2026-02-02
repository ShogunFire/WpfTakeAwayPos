using RestaurantPOS.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantPOS.Services.Interfaces
{
    public interface INavigationService : INotifyPropertyChanged
    {
        void Navigate<TViewModel>() where TViewModel : BaseViewModel;
        void GoHome();
        BaseViewModel CurrentViewModel { get; }
        bool ShowHomeButton { get; }
        event Action? CurrentViewModelChanged;
    }
}
