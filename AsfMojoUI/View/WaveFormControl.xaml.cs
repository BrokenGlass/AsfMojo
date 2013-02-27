using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AsfMojoUI.Model;
using AsfMojoUI.ViewModel;
using System.Threading.Tasks;
using System.Media;
using NAudio.Wave;
using AsfMojo.Media;
using System.IO;
using AsfMojo.Parsing;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace AsfMojoUI.View
{
    /// <summary>
    /// Interaction logic for WaveFormControl.xaml
    /// </summary>
    public partial class WaveFormControl : UserControl
    {
        double yTranslate = 40;
        double yScale = 40;

        List<Line> lines = new List<Line>();
        private uint startTimeOffset;

        public int MaxSampleCount { get; set; }


        public WaveFormControl()
        {
            //turn off anti-aliasing otherwise line rendering will be anti-aliased and not look as intended
            this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            InitializeComponent();
            MaxSampleCount = 50000;

            this.SizeChanged += new SizeChangedEventHandler(WaveFormControl_SizeChanged);
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(WaveFormControl_DataContextChanged);
        }

        void WaveFormControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender != null && ((FrameworkElement)sender).DataContext != null)
            {
                PayloadInfo payloadInfo = ((PayloadInfo)(((FrameworkElement)sender).DataContext));
                startTimeOffset = payloadInfo.PresentationTime - AsfHeaderItem.Configuration.AsfPreroll;


                if (payloadInfo != null)
                    Task.Factory.StartNew((Action)(() => { ComputeWaveForm(startTimeOffset); }));
            }
        }

        void WaveFormControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.yTranslate = mainCanvas.ActualHeight / 2;
            this.yScale = mainCanvas.ActualHeight / 2;

            PayloadInfo payloadInfo = ((PayloadInfo)(((FrameworkElement)sender).DataContext));
            startTimeOffset = payloadInfo.PresentationTime - AsfHeaderItem.Configuration.AsfPreroll;


            if (payloadInfo != null)
                Task.Factory.StartNew((Action)(() => { ComputeWaveForm(startTimeOffset); }));
        }

        public void Reset()
        {
            lines.Clear();
            mainCanvas.Children.Clear();
        }

        private Line CreateLine(int x1, int x2, float y1, float y2, System.Windows.Media.Brush brush)
        {
            Line line =  new Line();
            lines.Add(line);
            mainCanvas.Children.Add(line);

            line.Stroke = brush;
            line.StrokeThickness = 1;
            line.X1 = x1;
            line.X2 = x2;
            line.Y1 = y1;
            line.Y2 = y2;
            line.Visibility = Visibility.Visible;
            return line;
        }
        public void DrawAudio(float[] data)
        {
            Reset();
            //Bitmap b = DrawNormalizedAudio(data);
            //b.Save(@"D:\samples\testSamples.png", ImageFormat.Png);

            int width = (int)ActualWidth;
            int size = data.Length;
            int height = (int)mainCanvas.ActualHeight;
            double pixelFactor = ActualWidth / data.Length;
            var lineBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 20, 255, 20));

            for (int iPixel = 0; iPixel < width; iPixel++)
            {
                // determine start and end points within WAV
                int start = (int)((float)iPixel * ((float)size / (float)width));
                int end = (int)((float)(iPixel + 1) * ((float)size / (float)width));
                float min = float.MaxValue;
                float max = float.MinValue;
                for (int i = start; i < end; i++)
                {
                    float val = data[i];
                    min = val < min ? val : min;
                    max = val > max ? val : max;
                }
                int yMax = height - (int)((max + 1) * .5 * height);
                int yMin = height - (int)((min + 1) * .5 * height);

                CreateLine(iPixel, (yMin == yMax) ? (iPixel + 1) : iPixel, yMin, yMax, lineBrush);
            }

            //show axis line
            CreateLine(0, width, height / 2, height / 2, System.Windows.Media.Brushes.Gray);
        }

        public static Bitmap DrawNormalizedAudio(float[] data)
        {
            System.Drawing.Color waveformColor = System.Drawing.Color.FromArgb(51, 255, 0);
            System.Drawing.Color gridColor = System.Drawing.Color.FromArgb(112, 112, 112);

            Bitmap bmp;
            int imageWidth = 400;
            int imageHeight = 200;

            bmp = new Bitmap(imageWidth, imageHeight);

            int BORDER_WIDTH = 0;
            int BORDER_WIDTH_LEFT = 23;
            int width = bmp.Width - (2 * BORDER_WIDTH) - BORDER_WIDTH_LEFT;
            int height = bmp.Height - (2 * BORDER_WIDTH);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.DarkBlue);

                System.Drawing.Pen pen = new System.Drawing.Pen(waveformColor);
                int size = data.Length;
                for (int iPixel = 0; iPixel < width; iPixel++)
                {
                    // determine start and end points within WAV
                    int start = (int)((float)iPixel * ((float)size / (float)width));
                    int end = (int)((float)(iPixel + 1) * ((float)size / (float)width));
                    float min = float.MaxValue;
                    float max = float.MinValue;
                    for (int i = start; i < end; i++)
                    {
                        float val = data[i];
                        min = val < min ? val : min;
                        max = val > max ? val : max;
                    }
                    int yMax = BORDER_WIDTH + height - (int)((max + 1) * .5 * height);
                    int yMin = BORDER_WIDTH + height - (int)((min + 1) * .5 * height);
                    g.DrawLine(pen, iPixel + BORDER_WIDTH + BORDER_WIDTH_LEFT, yMax,
                        iPixel + BORDER_WIDTH + BORDER_WIDTH_LEFT, yMin);
                }

                //draw axis, tickmarks
                System.Drawing.Pen tickmarkPen = new System.Drawing.Pen(waveformColor);
                System.Drawing.Pen gridPen = new System.Drawing.Pen(gridColor);
                decimal tickValue = -1.0M;
                bool everyOther = false;
                for (int y = imageHeight; y > 0; y -= imageHeight / 20)
                {
                    g.DrawLine(tickmarkPen, 0, y, 2, y);
                    if (tickValue >= -0.9M && tickValue <= 0.9M)
                    {
                        g.DrawString(tickValue.ToString(), new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Regular), System.Drawing.Brushes.LightGray, tickValue < 0 ? 2 : 5, y - 5);
                        if (everyOther)
                            g.DrawLine(gridPen, 23, y, imageWidth, y);
                        everyOther = !everyOther;
                    }
                    tickValue += 0.1M;
                }

                //draw vertical grid lines, one for every second

                for (int x = BORDER_WIDTH_LEFT; x < imageWidth - BORDER_WIDTH_LEFT; x += (imageWidth - BORDER_WIDTH_LEFT) / 5)
                {
                    g.DrawLine(gridPen, x, 0, x, imageHeight);
                }
            }

            return bmp;
        }

        public void ComputeWaveForm(UInt32 presentationTime)
        {
            try
            {
                double timeInSeconds = presentationTime;
                timeInSeconds /= 1000;

                float[] samples;

                using (AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, ViewModelLocator.MainStatic.FileName, timeInSeconds))
                using (AsfAudio asfAudio = new AsfAudio(asfStream))
                {
                    int sampleCountForTwoSeconds = (int) (2 * asfStream.Configuration.AudioSampleRate);
                    samples = asfAudio.GetSamples(sampleCountForTwoSeconds).Select(sample => sample.Left).ToArray();
                }
                Dispatcher.BeginInvoke((Action)(() => DrawAudio(samples)));
            }
            catch (AsfStreamException) { }
        }

        private void WaveFormPlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double timeInSeconds = (double)startTimeOffset / 1000;
                using(AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, ViewModelLocator.MainStatic.FileName, timeInSeconds))
                using (AsfAudio asfAudio = new AsfAudio(asfStream))
                {
                    //play a two second sample
                    byte[] data = asfAudio.GetSampleBytes(2 * (int)  asfStream.Configuration.AudioSampleRate  * asfStream.Configuration.AudioChannels);

                    WaveMemoryStream mwav = new WaveMemoryStream(data, (int)asfStream.Configuration.AudioSampleRate, asfStream.Configuration.AudioBitsPerSample, asfStream.Configuration.AudioChannels);
                    SoundPlayer sp = new SoundPlayer(mwav);
                    sp.Play();
                }
            }
            catch (AsfStreamException) { }
        }



    }
}
