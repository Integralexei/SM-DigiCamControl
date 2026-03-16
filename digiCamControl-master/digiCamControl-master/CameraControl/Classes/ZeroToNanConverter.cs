using System;
using System.Globalization;
using System.Windows.Data;

namespace CameraControl.Classes
{
    /// <summary>
    /// Converts a zero or negative numeric value to Double.NaN so that WPF
    /// auto-sizes the Image element from its bitmap source rather than
    /// collapsing to zero width/height.
    ///
    /// Used on FileInfo.Width / FileInfo.Height XAML bindings to prevent the
    /// ZoomAndPanControl content from collapsing during thumbnail generation:
    /// GenerateCache calls exiv2Helper.Load() which sets FileItem.FileInfo =
    /// new FileInfo() (Width=0), then sets Size in-place without a second
    /// PropertyChanged. The binding sees Width=0 and collapses the Image to
    /// 30x30 px (Margin only), causing a spurious ScaleToFit call that makes
    /// the image appear to shrink and disappear.
    ///
    /// With this converter: Width=0 → NaN → auto-size from DisplayImage →
    /// no content collapse → no spurious zoom change.
    /// </summary>
    [ValueConversion(typeof(int), typeof(double))]
    public class ZeroToNanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return double.NaN;
            try
            {
                double d = System.Convert.ToDouble(value);
                return d > 0 ? d : double.NaN;
            }
            catch
            {
                return double.NaN;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
