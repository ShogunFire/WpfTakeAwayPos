using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantPOS.Resources.Converters
{
    public class CashVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string paymentMethod && paymentMethod == "Cash")
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
