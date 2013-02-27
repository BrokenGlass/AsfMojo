using System;
using System.Collections.Generic;

using AsfMojo.Media;

namespace AsfMojo.Configuration
{

    /// <summary>
    /// ASF constants relevant for parsing, used internally
    /// </summary>
    internal class AsfConstants
    {
        public const UInt32 ASF_MAX_HEADER_SIZE = 10000; //Maximum length for asf header
        public const byte ASF_PRIVATE_STREAM_OFFSET = 50;
        public const UInt32 ASF_SEND_SAFTEY_THRESHOLD = 2000; //subtract 2 seconds from all send times
        public const UInt32 ASF_TIME_THRESHOLD = 100; //100 milliseconds tolerance for continuity check
        public const UInt32 ASF_TIME_THRESHOLD_START_AUDIO = 250; //used for Audio File streaming
    }


    /// <summary>
    /// The relevant properties of an ASF File. These configuration properties are used internally 
    /// when parsing a media file
    /// </summary>
    public class AsfFileConfiguration
    {
        public List<AsfPacket> Packets { get; set; }
        public UInt32 AsfPreroll { get; set; }
        public UInt32 AsfHeaderSize { get; set; }
        public UInt32 AsfPacketSize { get; set; }
        public UInt32 AsfPacketCount { get; set; }
        public UInt32 AsfPacketHeaderSize { get; set; }
        public UInt32 AsfIndexSize { get; set; }
        public UInt32 AsfVideoStreamId { get; set; }
        public UInt32 AsfAudioStreamId { get; set; }
        public UInt32 AsfBitRate { get; set; }
        public double Duration { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public ushort AudioChannels { get; set; }
        public ushort AudioBitsPerSample { get; set; }
        public UInt32 AudioSampleRate { get; set; }

        public AsfFileConfiguration()
        {
            Reset();
        }

        /// <summary>
        /// Resets the configuration to default values
        /// </summary>
        public void Reset()
        {
            AsfPreroll = 0; 
            AsfHeaderSize = 0;
            AsfPacketSize = 0;
            AsfPacketCount = 0;
            AsfPacketHeaderSize = 0;
            AsfIndexSize = 0;
            AsfBitRate = 0;
            ImageWidth = 0;
            ImageHeight = 0;
            Duration = 0;
            AsfVideoStreamId = 0;
            AsfAudioStreamId = 0;

            AudioChannels = 1;
            AudioSampleRate = 0;
            AudioBitsPerSample = 0;

        }
    }
}
