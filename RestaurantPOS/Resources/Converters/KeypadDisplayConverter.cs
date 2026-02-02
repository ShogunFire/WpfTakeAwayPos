using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantPOS.Resources.Converters
{
    public class KeypadDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return "";

            var input = values[0]?.ToString() ?? "0";
            var unit = values[1]?.ToString() ?? string.Empty;
            var isPrefix = values[2] is bool b && b;

            if (string.Equals(unit, "unit", StringComparison.OrdinalIgnoreCase))
                unit = string.Empty;

            if (string.IsNullOrWhiteSpace(unit))
                return input;

            return isPrefix
                ? string.Concat(unit, input)
                : string.Concat(input, " ", unit);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
