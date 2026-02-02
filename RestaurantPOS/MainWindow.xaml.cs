using RestaurantPOS.Services.Interfaces;
using RestaurantPOS.ViewModels;
using System.Windows;

namespace RestaurantPOS
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly IPopupService _popupService;
        private readonly TopBarViewModel _topBarViewModel;

        public MainWindow(INavigationService navigationService, IDialogService dialogService, IPopupService popupService, TopBarViewModel topBarViewModel)
        {
            InitializeComponent();
            
            _navigationService = navigationService;
            _popupService = popupService;
            _topBarViewModel = topBarViewModel;
            
            // Create a composite data context wrapper that provides both services
            this.DataContext = new CompositeMainWindowViewModel(navigationService, popupService, topBarViewModel);
            
            // Initialize DialogService with the DialogBox control
            dialogService.Initialize(DialogBoxControl);
        }
    }

    public class CompositeMainWindowViewModel : BaseViewModel
    {
        public INavigationService NavigationService { get; }
        public IPopupService PopupService { get; }
        public TopBarViewModel TopBar { get; }

        public CompositeMainWindowViewModel(INavigationService navigationService, IPopupService popupService, TopBarViewModel topBar)
        {
            NavigationService = navigationService;
            PopupService = popupService;
            TopBar = topBar;
        }
    }
}
