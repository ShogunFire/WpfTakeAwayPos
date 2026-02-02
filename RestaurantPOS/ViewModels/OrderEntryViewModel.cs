using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services;
using RestaurantPOS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows.Input;
using FontAwesome.Sharp;
using System.Threading.Tasks;

#nullable enable

namespace RestaurantPOS.ViewModels
{
    public partial class OrderEntryViewModel : BaseViewModel, INavigationGuard
    {
        private ObservableCollection<MenuItem> _allMenuItems;
        private readonly INavigationService _navigationService;
        private readonly MenuService _menuService;
        private readonly IDialogService _dialogService;
        private readonly IOrderSession _orderSession;
        public IOrderSession OrderSession => _orderSession;

        [ObservableProperty]
        private ObservableCollection<MenuItem> filteredMenuItems;


        [ObservableProperty]
        private ObservableCollection<Category> categories;

        [ObservableProperty]
        private Category selectedCategory;

        public OrderEntryViewModel(INavigationService navigationService, MenuService menuService, IOrderSession orderSession, IDialogService dialogService)
        {
            _navigationService = navigationService;
            _menuService = menuService;
            _orderSession = orderSession;
            _dialogService = dialogService;
            _allMenuItems = new ObservableCollection<MenuItem>();
            FilteredMenuItems = new ObservableCollection<MenuItem>();
            Categories = new ObservableCollection<Category>();

            // Subscribe to order changes
            _orderSession.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IOrderSession.CurrentOrder))
                    GoToPaymentCommand.NotifyCanExecuteChanged();
            };

            _orderSession.OrderLinesChanged += (_, _) =>
                GoToPaymentCommand.NotifyCanExecuteChanged();

            // Mock data for Categories
            Categories.Add(new Category { CategoryId = 1, Name = "All Items", Icon = "Bars" });
            Categories.Add(new Category { CategoryId = 2, Name = "Appetizers", Icon = "Utensils" } );
            Categories.Add(new Category { CategoryId = 3, Name = "Burgers", Icon = "Hamburger" });
            Categories.Add(new Category { CategoryId = 4, Name = "Pizzas", Icon = "PizzaSlice" });
            Categories.Add(new Category { CategoryId = 5, Name = "Beverages", Icon = "Cocktail" });

            // Load mock menu items from MenuService
            _allMenuItems = new ObservableCollection<MenuItem>(_menuService.GetMenuItems());

            // initialize default selected category and populate center
            SelectedCategory = Categories.First();
            FilterMenuItems();

           
        }

        partial void OnSelectedCategoryChanged(Category value)
        {
            FilterMenuItems();
        }

        private void FilterMenuItems()
        {
            FilteredMenuItems.Clear();

            if (SelectedCategory == null) return;

            if (SelectedCategory.CategoryId == 1) // All Items
            {
                foreach (var item in _allMenuItems)
                    FilteredMenuItems.Add(item);
            }
            else
            {
                var filtered = _allMenuItems.Where(m => m.CategoryId == SelectedCategory.CategoryId);
                foreach (var item in filtered)
                    FilteredMenuItems.Add(item);
            }
        }

        [RelayCommand]
        private void AddItem(object parameter)
        {
            if (parameter is MenuItem menuItem)
            {
                _orderSession.CurrentOrder.AddMenuItem(menuItem);
            }
        }

        [RelayCommand]
        private void IncreaseQty(object parameter)
        {
            if (parameter is OrderLine line)
            {
                line.Quantity += 1;
            }
        }

        [RelayCommand]
        private void DecreaseQty(object parameter)
        {
            if (parameter is OrderLine line)
            {
                if (line.Quantity > 1)
                {
                    line.Quantity -= 1;
                }
                else
                {
                    _orderSession.CurrentOrder.RemoveMenuItem(line);
                }
            }
        }

        [RelayCommand]
        private void RemoveItem(object parameter)
        {
            if (parameter is OrderLine line)
            {
                _orderSession.CurrentOrder.RemoveMenuItem(line);
            }
        }


        [RelayCommand]
        private void CancelOrder()
        {
            _orderSession.Cancel();
        }

        [RelayCommand(CanExecute = nameof(CanGoToPayment))]
        private void GoToPayment()
        {
            _navigationService.Navigate<PaymentViewModel>();
        }

        private bool CanGoToPayment()
        {
            return _orderSession.CurrentOrder.OrderLines.Count > 0;
        }

        public async Task<bool> CanNavigateAwayAsync()
        {
            // Prevent navigation if there's an order in progress
            if (_orderSession.CurrentOrder?.OrderLines?.Count > 0)
            {
                await _dialogService.Alert(
                "There is items in the order. You can't leave this page");
                return false;
            }
            
            return true;
        }
    }
}

#nullable restore