using System.Windows;
using System.Windows.Controls;
using RestaurantPOS.ViewModels;

namespace RestaurantPOS.Views.Controls
{
    public partial class PopupContainer : UserControl
    {
        public static readonly DependencyProperty CurrentPopupViewModelProperty =
            DependencyProperty.Register(
                nameof(CurrentPopupViewModel),
                typeof(BaseViewModel),
                typeof(PopupContainer),
                new PropertyMetadata(null));

        public BaseViewModel CurrentPopupViewModel
        {
            get => (BaseViewModel)GetValue(CurrentPopupViewModelProperty);
            set => SetValue(CurrentPopupViewModelProperty, value);
        }

        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register(
                nameof(IsPopupOpen),
                typeof(bool),
                typeof(PopupContainer),
                new PropertyMetadata(false));

        public bool IsPopupOpen
        {
            get => (bool)GetValue(IsPopupOpenProperty);
            set => SetValue(IsPopupOpenProperty, value);
        }

        public PopupContainer()
        {
            InitializeComponent();
        }
    }
}
