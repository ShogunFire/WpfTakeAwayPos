using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantPOS.Views
{
    public partial class BackofficeNavMenu : UserControl
    {
        public BackofficeNavMenu()
        {
            InitializeComponent();
            if (Application.Current is App app)
            {
                DataContext = app.Services.GetRequiredService<BackofficeNavMenuViewModel>();
            }
        }
    }
}
