// filepath: c:\Users\pmich\OneDrive\Documents\Projets\MyPos\RestaurantPOS\Converters\StringToBoolConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? parameter : null;
        }
    }
}