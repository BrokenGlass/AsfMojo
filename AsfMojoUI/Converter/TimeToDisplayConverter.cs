using System;
using System.Windows.Data;

namespace AsfMojoUI.Converter
{

    public class TimeToDisplayConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            UInt32 timeValue = Convert.ToUInt32(value);
            double timeInSeconds = timeValue;
            timeInSeconds /= 1000;

            TimeSpan ts = TimeSpan.FromSeconds(timeInSeconds);
            return ts.ToString("h\\:mm':'ss\\.ff");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
