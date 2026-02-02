using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.Resources.Converters
{
    public class InventoryQuantityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "";

            var quantity = values[0];
            var unit = values[1] as string;

            if (quantity == null)
                return "";

            if (string.Equals(unit, "unit", StringComparison.OrdinalIgnoreCase))
                return string.Format(culture, "{0}", quantity);

            if (string.IsNullOrWhiteSpace(unit))
                return string.Format(culture, "{0}", quantity);

            return string.Format(culture, "{0} {1}", quantity, unit);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
