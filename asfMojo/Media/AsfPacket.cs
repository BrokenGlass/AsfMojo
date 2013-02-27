using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AsfMojo.Configuration;

namespace AsfMojo.Media
{

    /// <summary>
    /// Wrapper class for an ASF packet
    /// </summary>
    public class AsfPacket
    {
        public UInt16 Duration { get; private set; }
        public bool IsKeyFrame { get; private set; }
        public int PayloadCount { get { return Payload.Count; } }
        public int PayloadIdOffset { get; private set; }
        public uint PacketID { get; private set; }
        public int Size { get; private set; }
        public List<PayloadInfo> Payload {get; private set;}

        protected byte[] _packet;
        protected int _sendTimeOffset;
        protected AsfFileConfiguration _asfConfig;

        private UInt32 _sendTime;
        public UInt32 SendTime 
        { 
            get
            {
                return _sendTime;
            }
            private set
            {
                _sendTime = value;
                Buffer.BlockCopy(BitConverter.GetBytes(_sendTime), 0, _packet, _sendTimeOffset, sizeof(UInt32));
            }
        }


        public AsfPacket(AsfFileConfiguration asfConfig, byte[] packetData, uint packetID = 0, int payloadIdOffset = 0)
        {
            if (asfConfig.AsfPacketSize != packetData.Length)
                throw new ArgumentException("Packet data length doesn't match packet configuration");

            _asfConfig = asfConfig;
            Size = packetData.Length;
            PacketID = packetID;
            PayloadIdOffset = payloadIdOffset;
            Payload = new List<PayloadInfo>();
            _packet = packetData;
            ParsePacket();
        }

        /// <summary>
        /// Parse the ASF packet, extract presentation time, send time and payload information
        /// </summary>
        void ParsePacket()
        {
            //check error correction data, usually byte value 130 indicating error correction present, 2 bytes error correction data
            byte errorCorrection = _packet[0];
            bool errorCorrectionPresent = Convert.ToBoolean(errorCorrection & 128);
            byte errorCorrectionLengthType = (byte)((errorCorrection ^ 128) >> 5);
            byte errorCorrectionDataFieldSize = (byte)(errorCorrection & 15);
            bool opaqueDataPresent = Convert.ToBoolean(errorCorrection & 16);

            if(opaqueDataPresent)
                throw new ArgumentException("Not a valid ASF packet");

            if (errorCorrectionPresent)
            {
                if ((errorCorrectionLengthType == 0 && errorCorrectionDataFieldSize != 2) || (errorCorrectionLengthType!=0 && errorCorrectionDataFieldSize != 0) )
                    throw new ArgumentException("Not a valid ASF packet");
            }

            byte errorCorrectionType = _packet[1];
            byte errorCorrectionCycle = _packet[2];

            if(errorCorrectionType != 0 || errorCorrectionCycle != 0)
                throw new ArgumentException("Cannot parse ASF packet with error correction data");

            byte lengthTypeFlags = _packet[3];
            byte propertyFlags = _packet[4];

            int parsingOffset = 5;
            UInt32 sequence = 0;
            UInt32 packetSize = 0;
            UInt32 paddingLength = 0;
            bool hasMultiplePayloads = false;

            if ((lengthTypeFlags & 1) == 1)
            {	//More than one payload in this packet
                hasMultiplePayloads = true;
            }
            if ((lengthTypeFlags & 2) == 2)
            {	//8-bit sequence field specified
                sequence = _packet[parsingOffset];
                parsingOffset += 1;
            }
            else if ((lengthTypeFlags & 4) == 4)
            {	//16-bit sequence field specified
                sequence = BitConverter.ToUInt16(_packet, parsingOffset);
                parsingOffset += 2;
            }
            else if ((lengthTypeFlags & 6) == 6)
            {	//32-bit sequence field specified
                sequence = BitConverter.ToUInt32(_packet, parsingOffset);
                parsingOffset += 4;
            }

            if ((lengthTypeFlags & 8) == 8)
            {	//8-bit padding size specified
                paddingLength = _packet[parsingOffset];
                parsingOffset += 1;
            }
            else if ((lengthTypeFlags & 16) == 16)
            {	//16-bit padding size specified
                paddingLength = BitConverter.ToUInt16(_packet, parsingOffset);
                parsingOffset += 2;
            }
            else if ((lengthTypeFlags & 24) == 24)
            {	//32-bit padding size specified
                paddingLength = BitConverter.ToUInt32(_packet, parsingOffset);
                parsingOffset += 4;
            }

            if ((lengthTypeFlags & 32) == 32)
            {	//8-bit packet size specified
                packetSize = _packet[parsingOffset];
                parsingOffset += 1;
            }
            else if ((lengthTypeFlags & 64) == 64)
            {	//16-bit packet size specified
                packetSize = BitConverter.ToUInt16(_packet, parsingOffset);
                parsingOffset += 2;
            }
            else if ((lengthTypeFlags & 96) == 96)
            {	//32-bit packet size specified
                packetSize = BitConverter.ToUInt32(_packet, parsingOffset);
                parsingOffset += 4;
            }

            _sendTimeOffset = parsingOffset;
            SendTime = BitConverter.ToUInt32(_packet, parsingOffset);
            parsingOffset += 4;
            Duration = BitConverter.ToUInt16(_packet, parsingOffset);
            parsingOffset += 2;

            int payloadCount = 1;

            if (hasMultiplePayloads)
            {
                byte payloadFlags = _packet[parsingOffset];
                parsingOffset++;
                payloadCount = payloadFlags & 63; // bits 0-5 are payload count
                int payLoadLengthType = payloadFlags & 192; // bits 6-7 are payLoadLengthType
            }

            for (int idx = 0; idx < payloadCount; idx++)
            {
                PayloadInfo pi = ParsePayLoad(idx + PayloadIdOffset, hasMultiplePayloads, paddingLength, ref parsingOffset);
                Payload.Add(pi);
            }
        }

        private PayloadInfo ParsePayLoad(int payloadId, bool hasMultiplePayloads, UInt32 paddingLength, ref int packetOffset)
        {
            byte streamID;
            bool isKeyFrame = false;

            PayloadInfo pi = new PayloadInfo();
            pi.PayloadId = payloadId;

            streamID = _packet[packetOffset];

            pi.StreamIDOffset = packetOffset;
            pi.StreamId = (byte)(streamID & 0x7f); //127
            packetOffset++;
            isKeyFrame = (streamID & 0x80) == 0x80;

            byte mediaObjectNumber = _packet[packetOffset];
            pi.MediaObjectNumber = mediaObjectNumber;
            pi.MediaObjectNumberOffset = packetOffset;

            packetOffset++;
            pi.MediaOffset = packetOffset;
            UInt32 offsetIntoMedia = BitConverter.ToUInt32(_packet, packetOffset);
            packetOffset += 4;
            pi.OffsetIntoMedia = offsetIntoMedia;

            pi.IsKeyframeStart = isKeyFrame && offsetIntoMedia == 0;
            IsKeyFrame = IsKeyFrame || pi.IsKeyframeStart; //we are only interested in the first packet part of a keyframe

            byte replicatedDataLength; //should be set to 1 for compressed data
            replicatedDataLength = _packet[packetOffset];
            packetOffset++;


            UInt32 mediaObjectSize = 0;
            UInt32 presentationTime = 0;
            if (replicatedDataLength > 0)
            {
                mediaObjectSize = BitConverter.ToUInt32(_packet, packetOffset);
                packetOffset += 4;

                pi.PresentationTimeOffset = packetOffset;
                presentationTime = BitConverter.ToUInt32(_packet, packetOffset);
                pi.PresentationTime = presentationTime;
                packetOffset += 4;

                //skip over extension field part of replicated data
                packetOffset += replicatedDataLength - sizeof(UInt32) - sizeof(UInt32);
            }
            UInt16 payLoadLength = 0;
            if (hasMultiplePayloads)
            {
                payLoadLength = BitConverter.ToUInt16(_packet, packetOffset);
                packetOffset += sizeof(UInt16);
            }
            else
            {
                payLoadLength = (UInt16)(_packet.Length - packetOffset - paddingLength);
            }
            pi.PayLoadLength = payLoadLength;

            //skip over payload data
            packetOffset += payLoadLength;
            return pi;
        }

        /// <summary>
        /// Correct presentation and send time stamps based on the stream info
        /// <param name="configuration">The ASF configuration</param>
        /// <param name="asfStream">The stream info</param>
        /// </summary>
        public bool SetStart(AsfFileConfiguration configuration, AsfStreamInfo asfStream)
        {
            if (asfStream.StreamType != AsfStreamType.asfAudio) //nothing to do for audio
            {

                //determine keyframe
                PayloadInfo keyframeInfo = (from payload in Payload where (payload.StreamId == configuration.AsfVideoStreamId && payload.IsKeyframeStart) select payload).LastOrDefault();

                if (keyframeInfo == null)
                    return false;
                //now cut out everything before that presentation time
                for (int idx = 0; idx < Payload.Count; idx++)
                {
                    if (Payload[idx].StreamId == configuration.AsfVideoStreamId && !Payload[idx].IsKeyframeStart && Payload[idx].PresentationTime < keyframeInfo.PresentationTime)
                    {
                        byte streamId = Payload[idx].StreamId;

                        if (Payload[idx].MediaObjectNumber < keyframeInfo.MediaObjectNumber && asfStream.StreamType != AsfStreamType.asfUnaltered) //this is a B or P-frame that comes before the key frame, so it must be disabled
                            streamId += AsfConstants.ASF_PRIVATE_STREAM_OFFSET;

                        Payload[idx].StreamId = streamId;
                        Buffer.SetByte(_packet, Payload[idx].StreamIDOffset, streamId);

                        //fix media offset as well
                        UInt32 mediaOffset = 0;
                        Payload[idx].OffsetIntoMedia = mediaOffset;
                        Buffer.BlockCopy(BitConverter.GetBytes(mediaOffset), 0, _packet, Payload[idx].MediaOffset, sizeof(UInt32));
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Correct presentation and send time stamps based on the stream info
        /// <param name="configuration">The ASF configuration</param>
        /// <param name="asfStreamInfo">The stream info</param>
        /// </summary>
        public void SetFollowup(AsfFileConfiguration configuration, AsfStreamInfo asfStreamInfo)
        {
            Int64 packetSendTime = (Int64)SendTime - asfStreamInfo.StartSendTime;

            if (packetSendTime - AsfConstants.ASF_SEND_SAFTEY_THRESHOLD > 0)
                packetSendTime -= AsfConstants.ASF_SEND_SAFTEY_THRESHOLD;
            else
                packetSendTime = 0;

            SendTime = (uint)packetSendTime;

            //base payload on packet send time, add delta to zero based send time
            for (int i = 0; i < Payload.Count; i++)
            {
                Int64 payloadPresentationTime = (Int64)Payload[i].PresentationTime - asfStreamInfo.StartTimeOffset;
                if (payloadPresentationTime < _asfConfig.AsfPreroll)
                {
                    //an audio payload before the preroll must be eliminated, in this case we assign it a private stream id
                    if (Payload[i].StreamId == configuration.AsfAudioStreamId && asfStreamInfo.StreamType != AsfStreamType.asfUnaltered)
                    {
                        payloadPresentationTime = SendTime + _asfConfig.AsfPreroll;
                        MovePayloadPrivate(Payload[i], (uint)payloadPresentationTime);
                    }
                    else
                    {
                        //set the time slightly before the preroll time - not used by renderer, but decoded so first frame (seek point) can be delta frame
                        payloadPresentationTime = asfStreamInfo.StreamType == AsfStreamType.asfImage ? (_asfConfig.AsfPreroll - 100) : _asfConfig.AsfPreroll;
                    }
                }


                //remove unnecesscary audio data for image stream, that means only one stream is remaining which sets the timeline
                if (asfStreamInfo.StreamType == AsfStreamType.asfImage && Payload[i].StreamId == configuration.AsfAudioStreamId)
                {
                    MovePayloadPrivate(Payload[i], SendTime + _asfConfig.AsfPreroll);
                }

                //Packet Sendtime must be earlier than Payload presentation times
                if (payloadPresentationTime < SendTime)
                    SendTime = Math.Max((uint)payloadPresentationTime, asfStreamInfo.MinPacketSendTime);

                SetPayloadPresentationTime(Payload[i], (uint)payloadPresentationTime);

                if ((asfStreamInfo.StreamType != AsfStreamType.asfUnaltered) && asfStreamInfo.StreamType != AsfStreamType.asfImage && Payload[i].PresentationTime > _asfConfig.AsfPreroll && Payload[i].PresentationTime - _asfConfig.AsfPreroll > (asfStreamInfo.EndTimeOffset - asfStreamInfo.StartTimeOffset))
                {
                    //Crop both audio and video at the end of the segment
                    payloadPresentationTime = (asfStreamInfo.EndTimeOffset - asfStreamInfo.StartTimeOffset) + _asfConfig.AsfPreroll;
                    MovePayloadPrivate(Payload[i], (uint)payloadPresentationTime);
                }

                //Handle media object id's: must be consecutive, starting at zero, roll over at 255
                uint maxPresentationTime = 0;
                asfStreamInfo.MaxPresentationTime.TryGetValue(Payload[i].StreamId, out maxPresentationTime);
                if (maxPresentationTime < Payload[i].PresentationTime)
                    asfStreamInfo.MaxPresentationTime[Payload[i].StreamId] = Payload[i].PresentationTime;

                if ((asfStreamInfo.MediaObjectId[Payload[i].StreamId] == 0 && asfStreamInfo.PrevMediaObjectId[Payload[i].StreamId] == 0) || asfStreamInfo.PrevMediaObjectId[Payload[i].StreamId] != Payload[i].MediaObjectNumber)
                {
                    asfStreamInfo.MediaObjectId[Payload[i].StreamId]++;
                }

                asfStreamInfo.PrevMediaObjectId[Payload[i].StreamId] = Payload[i].MediaObjectNumber;
                SetMediaObjectNumber(i, asfStreamInfo.MediaObjectId[Payload[i].StreamId]);
            }

            //the send time of the next packet must be larger or equal than the send time of the current packet, keep track of send time
            asfStreamInfo.MinPacketSendTime = SendTime;
        }


        /// <summary>
        /// Update the packet send time
        /// </summary>
        bool SetSendTime(UInt32 sendTime)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(sendTime), 0, _packet, _sendTimeOffset, sizeof(UInt32));
            return true;
        }


        /// <summary>
        /// Update a payload presentation time
        /// </summary>
        bool SetPayloadPresentationTime(PayloadInfo pi, UInt32 presentationTime)
        {
            pi.PresentationTime = presentationTime;
            //change underlying data
            Buffer.BlockCopy(BitConverter.GetBytes(presentationTime), 0, _packet, pi.PresentationTimeOffset, sizeof(UInt32));
            return true;
        }

        /// <summary>
        /// Move a payload to a private stream id
        /// </summary>
        void MovePayloadPrivate(PayloadInfo pi, uint presentationTime)
        {
            byte streamId = (byte)(pi.StreamId + AsfConstants.ASF_PRIVATE_STREAM_OFFSET);
            Buffer.SetByte(_packet, pi.StreamIDOffset, streamId);

            SetPayloadPresentationTime(pi, presentationTime);
        }

        /// <summary>
        /// Update a payload presentation time
        /// </summary>
        bool SetMediaObjectNumber(int payLoadIndex, byte mediaObjectNumber)
        {
            if (payLoadIndex < Payload.Count)
            {
                Payload[payLoadIndex].MediaObjectNumber = mediaObjectNumber;

                //change underlying data
                Buffer.SetByte(_packet, Payload[payLoadIndex].MediaObjectNumberOffset, mediaObjectNumber);
                return true;
            }
            else
                return false;
        }
    }
}
