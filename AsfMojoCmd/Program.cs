using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsfMojo.File;
using AsfMojo.Configuration;
using AsfMojo.Parsing;
using AsfMojo.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;
using System.Media;
using System.Threading;

namespace AsfMojoCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            //example usage:
            //-i test.wmv -l  
            //-i test.wmv -t  -start 144.5074 -o D:\samples\test.jpg
            //-i test.wmv -a -start 5.0 -end 12.5 -o D:\samples\audio.wav

            Dictionary<string, object> switches = new Dictionary<string, object>();

            for (int i = 0; i < args.Length; i++)
            {
                if(args[i] == "-l")
                    switches.Add("PrintDuration", "");

                if (args[i] == "-t")
                    switches.Add("ExtractImage", "");

                if (args[i] == "-a")
                    switches.Add("ExtractAudio", "");

                if (args[i] == "-u")
                    switches.Add("UpdateProperties", "");

                if (args[i] == "-author")
                    switches.Add("Author", args[++i].Trim('\"'));

                if (args[i] == "-description")
                    switches.Add("Description", args[++i].Trim('\"'));

                if (args[i] == "-title")
                    switches.Add("Title", args[++i].Trim('\"'));

                if (args[i] == "-copyright")
                    switches.Add("Copyright", args[++i].Trim('\"'));

                if (args[i] == "-start")
                    switches.Add("StartOffset", Convert.ToDouble(args[++i]));

                if (args[i] == "-end")
                    switches.Add("EndOffset", Convert.ToDouble(args[++i]));

                if (args[i] == "-w")
                    switches.Add("Width", Convert.ToInt32(args[++i]));

                if (args[i] == "-i")
                    switches.Add("InputFile", args[++i]);

                if (args[i] == "-o")
                    switches.Add("OutputFile", args[++i]);

                if (args[i] == "-?")
                    switches.Add("ShowHelp", args[++i]);
            }

            if (switches.ContainsKey("InputFile") && !switches.ContainsKey("ShowHelp"))
            {
                string fileName = (string)switches["InputFile"];
                ExecuteCommands(fileName, switches);
            }
            else
                PrintUsage();
        }

        public static void ExecuteCommands(string fileName, Dictionary<string, object> switches)
        {
            try
            {
                if (switches.ContainsKey("PrintDuration")) // print file duration
                {
                    AsfFile asfFile = new AsfFile(fileName);
                    AsfFileProperties fileProperties = asfFile.GetAsfObject<AsfFileProperties>();
                    Console.WriteLine(string.Format("File {0} has a duration of {1}", fileName, fileProperties.Duration.ToString("mm':'ss\\.fff")));
                }
                else if (switches.ContainsKey("ExtractImage")) //extract an image thumb from a time offset
                {
                    //create thumb
                    double startOffset = (double)switches["StartOffset"];
                    string outputFile = (string)switches["OutputFile"];

                    Bitmap bitmap = AsfImage.FromFile(fileName, startOffset);

                    if (switches.ContainsKey("Width"))
                    {
                        int width = (int)switches["Width"];
                        int height = (int)(bitmap.Height * ((double)width / bitmap.Width));

                        Bitmap thumbBitmap = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(thumbBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(bitmap, 0, 0, width, height);
                        }
                        bitmap = thumbBitmap;
                    }

                    ImageFormat outputFormat = ImageFormat.Bmp;
                    if (outputFile.ToLower().Contains(".jpg"))
                        outputFormat = ImageFormat.Jpeg;
                    else if (outputFile.ToLower().Contains(".png"))
                        outputFormat = ImageFormat.Png;

                    bitmap.Save(outputFile, outputFormat);
                }
                else if (switches.ContainsKey("ExtractAudio")) //extract audio data from a time range
                {
                    double startOffset = (double)switches["StartOffset"];
                    double endOffset = (double)switches["EndOffset"];
                    string outputFile = (string)switches["OutputFile"];

                    WaveMemoryStream waveStream = WaveMemoryStream.FromFile(fileName, startOffset, endOffset);

                    using (FileStream fs = new FileStream(outputFile, FileMode.Create))
                    waveStream.WriteTo(fs);
                }
                else if (switches.ContainsKey("UpdateProperties")) //update content description properties
                {
                    AsfFile asfFile = new AsfFile(fileName);
                    AsfContentDescriptionObject contentDescription = asfFile.GetAsfObject<AsfContentDescriptionObject>();

                    if (contentDescription != null)
                    {

                        string author = switches.ContainsKey("Author") ? (string)switches["Author"] : contentDescription.ContentProperties["Author"];
                        string copyright = switches.ContainsKey("Copyright") ? (string)switches["Copyright"] : contentDescription.ContentProperties["Copyright"];
                        string title = switches.ContainsKey("Title") ? (string)switches["Title"] : contentDescription.ContentProperties["Title"];
                        string description = switches.ContainsKey("Description") ? (string)switches["Description"] : contentDescription.ContentProperties["Description"];

                        AsfFile.From(fileName)
                               .WithAuthor(author)
                               .WithDescription(description)
                               .WithCopyright(copyright)
                               .WithTitle(title)
                               .Update();

                        Console.WriteLine(string.Format("Content description properties updated."));
                    }
                    else
                        Console.WriteLine(string.Format("No content description properties available."));
                }

                
            }
            catch (Exception)
            {
                PrintUsage();
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("AsfMojoCmd options:");
            Console.WriteLine("Displaying the media file playback duration:");
            Console.WriteLine("  AsfMojoCmd -i <filename> -l");
            Console.WriteLine("Example:");
            Console.WriteLine("  AsfMojoCmd -i test.wmv -l");

            Console.WriteLine("---------------------------");
            Console.WriteLine("Extracting a still frame from an offset:");
            Console.WriteLine("  AsfMojoCmd -i <filename> -t -start <start offset> [-w <pixel width>] -o <image output file>");
            Console.WriteLine("Example:");
            Console.WriteLine("  -i test.wmv -t  -start 52.3 -o test.jpg");

            Console.WriteLine("---------------------------");
            Console.WriteLine("Extracting a WAVE audio segment from an offset:");
            Console.WriteLine("  AsfMojoCmd -i <filename> -t -start <start offset> -end <end offset> -o <wav output file>");
            Console.WriteLine("Example:");
            Console.WriteLine("  -i test.wmv -a -start 5.0 -end 12.5 -o audio.wav");
            Console.WriteLine("---------------------------");
            Console.WriteLine("Displaying help:");
            Console.WriteLine("  AsfMojoCmd -?");
        }
    }
}
