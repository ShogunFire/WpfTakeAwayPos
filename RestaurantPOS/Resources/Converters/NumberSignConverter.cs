using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.Converters
{
    public class NumberSignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                    return "Positive";
                else if (decimalValue < 0)
                    return "Negative";
                else
                    return "Zero";
            }
            
            return "Zero";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
