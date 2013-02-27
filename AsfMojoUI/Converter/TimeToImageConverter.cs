using System;
using System.Windows.Data;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

using AsfMojoUI.ViewModel;
using AsfMojo.Media;
using AsfMojo.File;

namespace AsfMojoUI.Converter
{

    public class TimeToImageConverter : IValueConverter
    {
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private Bitmap _bitmap = null;
        private static AsfFile _asfFile = null;

        public static void EmptyCache()
        {
            if (_asfFile != null)
            {
                _asfFile.Dispose();
                _asfFile = null;
            }
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            UInt32 timeValue = (UInt32)value; //value is a time offset in units of 100 nanoseconds (see ASF specification)
            if (_asfFile == null)
                _asfFile = new AsfFile(ViewModelLocator.MainStatic.FileName);

            if (AsfHeaderItem.Configuration.ImageWidth <= 0) // no video stream
                return null;

            double timeInSeconds = timeValue - AsfHeaderItem.Configuration.AsfPreroll; //subtract Preroll
            timeInSeconds /= 1000;

            Task.Factory.StartNew((Action)(() => this.CreateImage(timeInSeconds)));
            _resetEvent.WaitOne();

            if (_bitmap != null)
            {
                MemoryStream ms = new MemoryStream();
                _bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                ViewModelLocator.MainStatic.CurrentImage = _bitmap;
                ViewModelLocator.MainStatic.CurrentImageSource = bi;
                _bitmap.Dispose();
                return bi;
            }
            else return null;
        }

        void CreateImage(double timeInSeconds)
        {
            try
            {
                using(AsfStream asfStream = new AsfStream(_asfFile, AsfStreamType.asfImage, timeInSeconds))
                using(AsfImage asfImage = new AsfImage(asfStream))
                _bitmap = asfImage.GetImage();
            }
            catch (Exception)
            {
                _bitmap = null;
            }
            _resetEvent.Set(); // signal that worker is done
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}