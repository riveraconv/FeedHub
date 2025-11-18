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

            bool result = value switch
            {
                null => false,
                string str => !string.IsNullOrWhiteSpace(str),
                DateTime dt => dt != default,
                _ => true
            };

            return invert ? !result : result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


//returns TRUE when there are valid text, not empty not null
//returns FALSE when there aren't text


