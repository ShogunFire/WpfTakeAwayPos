using RestaurantPOS.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantPOS.Resources.Converters
{
    public class CashTransactionTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CashTransactionType type)
            {
                return type switch
                {
                    CashTransactionType.Addition => new SolidColorBrush(Colors.Green),
                    CashTransactionType.Removal => new SolidColorBrush(Colors.Red),
                    CashTransactionType.Sale => new SolidColorBrush(Colors.Green),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
