using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batchbrake.Converters
{
    public class IndexToBrushConverter : IValueConverter
    {
        public IBrush EvenBrush { get; set; }
        public IBrush OddBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index % 2 == 0 ? EvenBrush : OddBrush;
            }
            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
