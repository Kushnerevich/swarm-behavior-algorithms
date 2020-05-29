using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace SwarmBehaviorAlgorithms.UI.Converters
{
    public class EnumNamesToStringsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !value.GetType().IsEnum) return string.Empty;
            try
            {
                return GetEnumDescription(value);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetEnumDescription(object value)
        {
            var fi = value.GetType().GetField(value.ToString());

            return fi.GetCustomAttribute<DescriptionAttribute>() is { } attribute
                ? attribute.Description
                : Enum.GetName(value.GetType(), value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
