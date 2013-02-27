using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsfMojo.Media;
using System.Runtime.InteropServices;
using WindowsMediaLib;
using WindowsMediaLib.Defs;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;

namespace AsfMojo.Media
{
    /// <summary>
    /// Create Image from an underlying ASF stream
    /// </summary>
    public sealed class AsfImage : IDisposable
    {
        private AsfStream _asfStream;
        private AsfIStream _asfMemoryStream;
        private Bitmap _sampleBitmap= null;

        public static IAsfImageProperties FromFile(string fileName)
        {
            return new AsfImageProperties() { FileName = fileName };
        }

        public static Bitmap FromFile(string fileName, double offset)
        {
            using (AsfStream asfStream = new AsfStream(AsfStreamType.asfImage, fileName, offset))
            using (AsfImage asfImage = new AsfImage(asfStream))
                return asfImage.GetImage();
        }

        public AsfImage(AsfStream asfStream) 
        {
            if (asfStream.StreamType != AsfStreamType.asfImage)
                throw new ArgumentException();

            _asfStream = asfStream;
            _asfMemoryStream = null;
            _sampleBitmap = null;
        }
        
        public void Dispose()
        {
            if (_asfMemoryStream != null)
            {
                _asfMemoryStream.Close();
                _asfMemoryStream = null;
            }

            if (_asfStream != null)
                _asfStream.Close();
        }


        /// <summary>
        /// Writes the image to a stream
        /// </summary>
        /// <param name="stream">The stream to save the image to</param>
        /// <param name="format">The image format</param>
        public void WriteTo(Stream stream, ImageFormat format)
        {
            Bitmap bm;
            if (_sampleBitmap == null)
                bm = GetImage();
            else
                bm = _sampleBitmap;

            if (bm != null)
            {
                bm.Save(stream, format);
                bm.Dispose();
            }
        }

        /// <summary>
        /// Get the bitmap image
        /// </summary>
        public Bitmap GetImage()
        {
            if (_sampleBitmap == null)
            {
                try
                {
                    _asfMemoryStream = new AsfIStream(_asfStream);
                    IWMSyncReader syncReader;
                    WMUtils.WMCreateSyncReader(IntPtr.Zero, Rights.Playback, out syncReader);
                    syncReader.OpenStream(_asfMemoryStream);

                    short videoStreamNum = (short)_asfStream.Configuration.AsfVideoStreamId;
                    syncReader.SetReadStreamSamples(videoStreamNum, false);

                    long cnsSampleTime;
                    long cnsSampleDuration;
                    SampleFlag dwFlags;
                    INSSBuffer pSample;
                    int dwOutputNum = 0;
                    short dwStreamNum = 0;
                    bool isBitmapCreated = false;

                    while (!isBitmapCreated)
                    {
                        syncReader.GetNextSample(0, out pSample, out cnsSampleTime, out cnsSampleDuration, out dwFlags, out dwOutputNum, out dwStreamNum);

                        if ((dwFlags & SampleFlag.CleanPoint) == SampleFlag.CleanPoint)
                        {
                            //Get the bitmap from the frame
                            IntPtr pBuffer;
                            int bufferLength;
                            pSample.GetBufferAndLength(out pBuffer, out bufferLength);
                            byte[] sampleData = new byte[bufferLength];
                            Marshal.Copy(pBuffer, sampleData, 0, bufferLength);
                            _sampleBitmap = CopyDataToBitmap(sampleData, _asfStream.Configuration.ImageWidth, _asfStream.Configuration.ImageHeight);
                            isBitmapCreated = true;
                        }
                        Marshal.FinalReleaseComObject(pSample);
                    }
                    Marshal.FinalReleaseComObject(syncReader);
                }
                catch (Exception) //catch and ignore, returned bitmap will be null
                {
                }
                finally
                {
                    Dispose();
                }
            }
            return _sampleBitmap;
        }

        private Bitmap CopyDataToBitmap(byte[] data, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            //Create a BitmapData and Lock all pixels to be written
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                              ImageLockMode.WriteOnly, bmp.PixelFormat);

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            //Unlock the pixels
            bmp.UnlockBits(bmpData);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bmp;
        }
    }
}
