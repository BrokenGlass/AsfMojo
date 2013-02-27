using System;
using System.Windows.Media.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;
using AsfMojo.Media;

namespace AsfMojoUI.Model
{

    /// <summary>
    /// Thumb prreview image for a given time offset within a media file
    /// </summary>
    public class PreviewImage
    {
        public double TimeOffset { get; set; }
        public uint PresentationTime { get; set; }
        public uint DisplayTime { get; set; }
        public string FileName { get; set; }
        public bool ImageLoaded { get; set; }
        public MemoryStream SourceStream { get { return _ms;}}

        private MemoryStream _ms = null;
        private BitmapImage _thumbImage = null;


        public PreviewImage()
        {

        }

        public void GenerateSource()
        {
            try
            {
                System.Drawing.Bitmap bm = AsfImage.FromFile(FileName)
                                                   .AtOffset(TimeOffset);
                if (bm != null)
                {
                    int newWidth = (int)(bm.Width * (70.0 / bm.Height));
                    int newHeight = 70;

                    System.Drawing.Bitmap thumbBitmap = new System.Drawing.Bitmap(newWidth, newHeight);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(thumbBitmap))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bm, 0, 0, newWidth, newHeight);
                    }

                    _ms = new MemoryStream();
                    thumbBitmap.Save(_ms, ImageFormat.Png);
                    _ms.Position = 0;
                    ImageLoaded = true;
                }
            }
            catch (Exception)
            {
            }
        }

        public BitmapImage ThumbImage
        {
            get
            {
                if (_thumbImage == null && ImageLoaded)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = _ms;
                    bi.EndInit();
                    _thumbImage = bi;
                }
                return _thumbImage;
            }
        }
    }
}
