using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace AsfMojo.Media
{
    /// <summary>
    /// Fluent interface to create an image from an ASF stream
    /// </summary>
    public interface IAsfImageProperties
    {
        string FileName { get; set; }
        double Offset { get; set; }

        Bitmap AtOffset(double offset);
    }

    /// <summary>
    /// Fluent interface to create an image from an ASF stream
    /// </summary>
    internal class AsfImageProperties : IAsfImageProperties
    {
        public string FileName { get; set; }
        public double Offset { get; set; }

        public Bitmap AtOffset(double offset)
        {
            return AsfImage.FromFile(FileName, offset);
        }

    }
}
