using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsfMojo.Media
{
    /// <summary>
    /// Fluent interface to create a WaveMemoryStream from a file, start- and end-offset
    /// </summary>
    public interface IWaveMemoryStreamProperties
    {
        string FileName { get; set; }
        double? StartOffset { get; set; }
        double? EndOffset { get; set; }

        /// <summary>
        /// Sets the start offset of the wave stream
        /// </summary>
        IWaveMemoryStreamProperties From(double offset);

        /// <summary>
        /// Sets the end offset of the wave stream and returns the stream
        /// </summary>
        WaveMemoryStream To(double offset);
    }



    internal class WaveMemoryStreamProperties : IWaveMemoryStreamProperties
    {
        public string FileName { get; set; }
        public double? StartOffset { get; set; }
        public double? EndOffset { get; set; }

        /// <summary>
        /// Sets the start offset of the wave stream
        /// </summary>
        public IWaveMemoryStreamProperties From(double offset)
        {
            StartOffset = offset;
            return this;
        }

        /// <summary>
        /// Sets the end offset of the wave stream and returns the stream
        /// </summary>
        public WaveMemoryStream To(double offset)
        {
            if (StartOffset == null)
                throw new ArgumentException("Must have a valid start offset");

            EndOffset = offset;
            return WaveMemoryStream.FromFile(FileName, StartOffset.Value, EndOffset.Value);
        }
    }
}
