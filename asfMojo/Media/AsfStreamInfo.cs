using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsfMojo.Media
{
    /// <summary>
    /// Stream information for an AsfStream, used to correct send time 
    /// and presentation time offsets
    /// </summary>
    public class AsfStreamInfo
    {

        public AsfStreamType StreamType { get; private set; }
        public uint StartSendTime { get; set; }
        public uint MinPacketSendTime { get; set; }
        public uint StartTimeOffset { get; set; }
        public uint EndTimeOffset { get; set; }

        private byte[] _mediaObjectId;
        public byte[] MediaObjectId { get { return _mediaObjectId; } } //starting at 1 ascending

        private byte[] _prevMediaObjectId;
        public byte[] PrevMediaObjectId { get { return _prevMediaObjectId; } } //relative to input data packet

        private Dictionary<byte, uint> _maxPresentationTime;
        public Dictionary<byte, uint> MaxPresentationTime { get { return _maxPresentationTime; } }

        public AsfStreamInfo(AsfStreamInfo info)
        {
            //copy properties
            StreamType = info.StreamType;
            StartSendTime = info.StartSendTime;
            MinPacketSendTime = info.MinPacketSendTime;
            StartTimeOffset = info.StartTimeOffset;
            EndTimeOffset = info.EndTimeOffset;

            _maxPresentationTime = new Dictionary<byte,uint>(info.MaxPresentationTime);
            UpdateFromStream(info);
        }

        public AsfStreamInfo(AsfStreamType streamType)
        {
            StreamType = streamType;
            _maxPresentationTime = new Dictionary<byte, uint>();
            StartSendTime = 0;
            MinPacketSendTime = 0;
            StartTimeOffset = 0;
            EndTimeOffset = 0;

            ResetMediaObjects();
        }

        public void ResetMediaObjects()
        {
            _mediaObjectId = new byte[256];
            _prevMediaObjectId = new byte[256];
        }

        public void UpdateFromStream(AsfStreamInfo info)
        {
            _mediaObjectId = new byte[info.MediaObjectId.Length];
            _prevMediaObjectId = new byte[info.PrevMediaObjectId.Length];
            Array.Copy(info.MediaObjectId, _mediaObjectId, info.MediaObjectId.Length);
            Array.Copy(info.PrevMediaObjectId, _prevMediaObjectId, info.PrevMediaObjectId.Length);

            _maxPresentationTime = new Dictionary<byte, uint>(info.MaxPresentationTime);
        }
    }

    /// <summary>
    /// Payload information for an ASF packet
    /// </summary>
    public class PayloadInfo
    {
        public int PayloadId { get; set; }
        public byte StreamId { get; set; }
        public int StreamIDOffset { get; set; }//offset in the data to stream id field
        public int MediaOffset	{ get; set; }//offset in the data to offsetIntoMedia field
        public int MediaObjectNumberOffset { get; set; }
        public bool IsKeyframeStart { get; set; }
        public byte MediaObjectNumber { get; set; }
        public ulong OffsetIntoMedia { get; set; }
        public int PresentationTimeOffset { get; set; }
        public uint PresentationTime { get; set; }
        public UInt16 PayLoadLength { get; set; }
    }
}
