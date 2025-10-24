using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FeedHub_App.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool result = !string.IsNullOrEmpty(value as string);
            return invert ? !result : result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}


