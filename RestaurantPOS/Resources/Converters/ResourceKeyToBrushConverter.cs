using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantPOS.Converters
{
    public class ResourceKeyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string resourceKey)
            {
                try
                {
                    var brush = Application.Current.Resources[resourceKey] as Brush;
                    return brush ?? Brushes.Black;
                }
                catch
                {
                    return Brushes.Black;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
