using RestaurantPOS.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.Resources.Converters
{
    public class CashTransactionTypeToSignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CashTransactionType type)
            {
                return type switch
                {
                    CashTransactionType.Addition => "+ $",
                    CashTransactionType.Removal => "- $",
                    CashTransactionType.Sale => "+ $",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
