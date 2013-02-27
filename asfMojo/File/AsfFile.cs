using System;
using System.Linq;
using System.IO;
using AsfMojo.Configuration;
using AsfMojo.Parsing;
using AsfMojo.Utils;
using System.Collections.Generic;
using AsfMojo.Media;
using System.Reflection;

namespace AsfMojo.File
{

    /// <summary>
    /// Possible media types
    /// </summary>
    public enum FileMediaType { Audio, Video };


    /// <summary>
    /// Manages access to a ASF media file
    /// </summary>
    public class AsfFile : IDisposable
    {
        public string FileName { get; private set; }
        public long StartOffset { get; set; }
        public long EndOffset { get; set; }
        public uint StartTimeOffsetVideo { get; set; }
        public uint EndTimeOffsetVideo { get; set; }
        public uint StartTimeOffsetAudio { get; set; }
        public uint EndTimeOffsetAudio { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public FileMediaType MediaType { get; set; }
        public UInt32 Length { get; set; }

        protected FileStream _fileStream;
        protected string _fileName;
        protected byte[] _streamingHeader = null;
        protected List<AsfObject> _headerObjects = null;
        protected AsfFileConfiguration _asfConfig;

        public AsfFileConfiguration PacketConfiguration
        {
            get
            {
                return _asfConfig;
            }
        }


        public static IAsfFileUpdateOptions From(string fileName)
        {
            return new AsfFileUpdateOptions() { FileName = fileName };
        }

        public AsfFile(string fileName)
        {
            _headerObjects = new List<AsfObject>();
            FileName = fileName;

            try
            {
                _asfConfig = LoadFileHeader(fileName);

            }
            catch (Exception)
            {
                //This is bad practice in general, but all sorts of exception can happen with a corrupted media file
                //This is exposed as a unified ArgumentException
                throw new ArgumentException("Invalid media file");
            }

            FileInfo fi = new FileInfo(fileName);
            SetAsfFileProperties(_asfConfig, fi);
        }


        public void Update(string targetFileName = null)
        {
            int headerLength = 0;
            bool doOverwrite = string.IsNullOrEmpty(targetFileName) || targetFileName == FileName;

            Close();

            if (doOverwrite)
            {
                targetFileName = Path.GetTempFileName();
            }

            foreach (AsfObject headerObject in _headerObjects)
            {
                if (headerObject is AsfDataObject)
                    break;
                headerLength += headerObject.GetLength();
            }

            var fileHeader = GetAsfObject<AsfFileHeader>();
            fileHeader.HeaderSize = (uint)headerLength;

            if(string.IsNullOrEmpty(targetFileName))
                targetFileName = FileName;

            using (FileStream outputStream = new FileStream(targetFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                //Update all headers
                foreach (AsfObject headerObject in _headerObjects)
                {
                    headerObject.Serialize(_asfConfig, outputStream);
                    if (headerObject is AsfDataObject)
                    {
                        long filePosition = _asfConfig.AsfHeaderSize;
                        // append the data packets
                        using (FileStream inputStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            inputStream.Seek(filePosition, SeekOrigin.Begin);
                            int bytesRead;
                            long totalBytesRead = 0;
                            long maxBytesRead = _asfConfig.AsfPacketCount * _asfConfig.AsfPacketSize;
                            byte[] buffer = new byte[32768];
                            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0 && totalBytesRead < maxBytesRead)
                            {
                                totalBytesRead += bytesRead;
                                if (totalBytesRead > maxBytesRead)
                                    bytesRead -= (int)(totalBytesRead - maxBytesRead);
                                outputStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
            }
            if (doOverwrite)
            {
                System.IO.File.Delete(FileName);
                System.IO.File.Move(targetFileName, FileName);
            }
        }

        /// <summary>
        /// Return a copy of the streaming header
        /// </summary>
        public byte[] GetStreamingHeader()
        {
            byte[] streamingHeaderCopy = null;
            if (_streamingHeader != null)
            {
                streamingHeaderCopy = new byte[_streamingHeader.Length];
                _streamingHeader.CopyTo(streamingHeaderCopy, 0);
            }
            return streamingHeaderCopy;
        }

        /// <summary>
        /// Return a copy of the streaming header
        /// </summary>
        protected AsfFileConfiguration LoadFileHeader(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            AsfFileConfiguration config = GetConfiguration(fileName);
            if (config != null)
            {
                _streamingHeader = new byte[config.AsfHeaderSize];
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int bytesReadTotal = 0;
                    while (bytesReadTotal < config.AsfHeaderSize )
                    {
                        bytesReadTotal += fs.Read(_streamingHeader, bytesReadTotal, (int)(config.AsfHeaderSize - bytesReadTotal));
                    }
                }
            }
            return config;
        }

        public List<AsfObject> GetAsfObjectByType(Guid objGuid)
        {
            return  _headerObjects.Where(o => o.Guid == objGuid).ToList();
        }

        public List<T> GetAsfObjects<T>() where T : AsfObject
        {
            return _headerObjects.OfType<T>().ToList();
        }

        public T GetAsfObject<T>() where T: AsfObject
        {
            return _headerObjects.OfType<T>().FirstOrDefault();
        }

        protected AsfFileConfiguration GetConfiguration(string fileName)
        {

            AsfFileConfiguration config = new AsfFileConfiguration();            
            bool isFirstObject = true;
            _headerObjects = new List<AsfObject>();

            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                while (true)
                {
                    AsfObject someObject = new AsfObject(stream);
                    if (isFirstObject && someObject.Guid != AsfGuid.ASF_Header_Object) // invalid file
                        return null;

                    isFirstObject = false;

                    if (someObject.Size == 0)
                        break;

                    _headerObjects.Add(AsfObject.CreateAsfObject(someObject.Guid, stream, config));
                    if (someObject.Guid == AsfGuid.ASF_Data_Object)
                    {
                        //parse asf objects after data packets, if any
                        long filePosition = config.AsfHeaderSize + config.AsfPacketCount * config.AsfPacketSize;
                        stream.Position = filePosition;
                    }
                }
            }
            return config;
        }

        internal bool SetOffsetRange(double startOffset, double endOffset, out FilePosition requestStartPosition, out FilePosition requestEndPosition, AsfStreamType streamType)
        {
            requestStartPosition = null;
            requestEndPosition = null;

            // find the file and offset for the starting time
            requestStartPosition = GetFilePosition(startOffset, streamType, true);

            if (requestStartPosition == null)
                return false;

            if (endOffset == 0)
            {
                long endFileOffset = EndOffset;
                requestEndPosition = new FilePosition(requestStartPosition.FileName, uint.MaxValue, endFileOffset);
            }
            else
            {
                // find the file and offset for the ending time
                requestEndPosition = GetFilePosition(endOffset, streamType, false);
                if (requestEndPosition == null)
                    return false;
            }

            StartOffset = requestStartPosition.FileOffset;
            EndOffset = requestEndPosition.FileOffset;
            Length = Convert.ToUInt32(EndOffset - StartOffset);
            if (_fileStream!=null)
                _fileStream.Dispose();
            _fileStream = null;

            return true;
        }

        protected FilePosition GetFilePosition(double searchOffset, AsfStreamType streamType, bool isStart)
        {
            FilePosition requestedPosition = new FilePosition(FileName, StartTimeOffsetVideo, 0, MediaType, (int)(searchOffset * 1000));
            bool found = false;
            bool isKeyframe = false;
            bool wasKeyFrame = false;
            long maxOffset = 0;
            long minOffset = 0;
            UInt32 startTimeOffset = requestedPosition.TimeOffset;
            int diff = requestedPosition.Delta;
            int prevDiff = 0;
            FileStream fs = null;
            long fileOffset = 0;
            uint targetTimeOffset;
            string fileName = requestedPosition.FileName;
            AsfFileConfiguration asfConfig = _asfConfig;
            uint TargetStreamId = streamType == AsfStreamType.asfAudio ? _asfConfig.AsfAudioStreamId : _asfConfig.AsfVideoStreamId;

            UInt32 finalTimeOffset = (UInt32)Math.Max(0, startTimeOffset + diff);
            targetTimeOffset = finalTimeOffset;

            UInt32 maxTimeDifference = streamType == AsfStreamType.asfAudio ? AsfConstants.ASF_TIME_THRESHOLD_START_AUDIO : AsfConstants.ASF_TIME_THRESHOLD;
            uint averagePacketDuration = GetAveragePacketDuration();

            minOffset = asfConfig.AsfHeaderSize;

            //set maxOffset to start of last packet
            maxOffset = asfConfig.AsfHeaderSize + (asfConfig.AsfPacketCount - 1) * asfConfig.AsfPacketSize;
            long packetCount = asfConfig.AsfPacketCount;

            // open the file
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception) {return null; }

            int prevPacketJumpCount = 0;
            bool wasBroadSearch = false;
            int totalJumpCount = 0;
            int packetJumpCount = 0;
            long searchMaxOffset = maxOffset;
            long searchMinOffset = minOffset;

            fileOffset = minOffset;

            while (!found)
            {
                totalJumpCount++;
                if (totalJumpCount > 500) //totally arbitrary number after which to give up
                    return null;


                packetJumpCount =  (int)Math.Round((float)diff / averagePacketDuration);
                if (Math.Abs(packetJumpCount) >= Math.Abs(prevPacketJumpCount) && Math.Abs(prevPacketJumpCount) > 0)
                { //we have to make sure that we converge, for this reason the number of packets we jump has to decrease at every iteration

                    if (Math.Abs(prevPacketJumpCount) > 1)
                        packetJumpCount = packetJumpCount > 0 ? Math.Abs(prevPacketJumpCount) - 1 : -Math.Abs(prevPacketJumpCount) + 1;
                    else
                        packetJumpCount = packetJumpCount > 0 ? 1 : -1;
                }
                prevPacketJumpCount = packetJumpCount;

                long byteJump = asfConfig.AsfPacketSize * packetJumpCount;

                if (fileOffset + byteJump > maxOffset)
                {
                    byteJump = maxOffset - fileOffset;
                    packetJumpCount = (int)(byteJump / asfConfig.AsfPacketSize);
                    prevPacketJumpCount = packetJumpCount;
                }
                else if (fileOffset + byteJump < minOffset)
                {
                    byteJump = minOffset - fileOffset;
                    packetJumpCount = (int)(byteJump / asfConfig.AsfPacketSize);
                    prevPacketJumpCount = packetJumpCount;
                }

                if (fs.Seek(fileOffset + byteJump, SeekOrigin.Begin) != fileOffset + byteJump)
                { return null; }
                fileOffset += byteJump;

                //track backwards if we cannot find target stream in this packet
                while (!FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref startTimeOffset, TargetStreamId))
                {
                    fileOffset -= asfConfig.AsfPacketSize;
                    if (fs.Seek(fileOffset, SeekOrigin.Begin) != fileOffset)
                    { return null; }
                }
                diff = (int)((long)finalTimeOffset - (long)startTimeOffset);
                if (Math.Abs(diff) <= maxTimeDifference)
                {
                    found = true;
                    if (isStart)
                        string.Format("Found video match at offset: {0} bytes, delta = {1} ms", fileOffset, diff).Log(LogLevel.logDetail);
                }
                else
                {	//handle case of multiple frames in packet, return the packet right after target time since we will track back to previous keyframe anyway
                    if (diff > 0 && prevDiff < 0 && Math.Abs(packetJumpCount) == 1 && !wasBroadSearch)
                    {
                        found = true;
                        if (isStart)
                            string.Format("Found video match at offset: {0} bytes, delta = {1} ms", fileOffset, diff).Log(LogLevel.logDetail);
                    }
                    wasBroadSearch = false;
                }
                prevDiff = diff;
            }

            if (found && !isStart)
            {
                bool foundAudio = false;
                bool temp;
                //keep moving forward until we are PAST the requested end time or reach the end of the current asset
                while ((!foundAudio || (foundAudio && diff > 0)) && fileOffset <= maxOffset)
                {
                    if (fileOffset <= maxOffset)
                    {
                        if (fs.Seek(fileOffset + asfConfig.AsfPacketSize, SeekOrigin.Begin) != fileOffset + asfConfig.AsfPacketSize)
                            break;
                        foundAudio = FindNextTimeOffset(asfConfig, fs, out temp, ref startTimeOffset, asfConfig.AsfAudioStreamId, false);
                        fileOffset += asfConfig.AsfPacketSize;
                    }

                    //calculate again
                    if (foundAudio)
                    {
                        diff = (int)((long)finalTimeOffset - (long)startTimeOffset);
                        targetTimeOffset = startTimeOffset;
                    }
                }
            }
            else if (found && isStart)
            {
                //now we have to find the previous keyframe, which also might be in the previous file if any exists
                //even if current frame is a keyframe we have to go back if the keyframe is after the match time so the correct still frame is shown in all cases
                //save the presentation Time of the match though, this time all earlier packets will be set to before preroll, so the 
                //stream "fast forwards" until the match time
                targetTimeOffset = finalTimeOffset;

                if (streamType == AsfStreamType.asfAudio)
                {
                    wasKeyFrame = true; // Every packet in audio is a keyframe.
                    isKeyframe = true;
                }
                else //Video
                {
                    wasKeyFrame = isKeyframe;
                    isKeyframe = false; //always go back to the previous key frame if possible to avoid edge condition with multiple frames in the same packet
                }

                while (((streamType != AsfStreamType.asfAudio && !isKeyframe) || diff < 0) && fileOffset > minOffset)
                {
                    fileOffset -= asfConfig.AsfPacketSize;
                    if (fileOffset >= asfConfig.AsfHeaderSize)
                    {
                        fs.Seek(fileOffset, SeekOrigin.Begin);
                        FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref startTimeOffset, TargetStreamId);
                        diff = (int)((long)finalTimeOffset - (long)startTimeOffset);
                    }
                    else
                    {
                        fileOffset = minOffset;
                        fs.Dispose();
                        //cannot find any earlier keyframe, just return position/file as is
                        if (found && wasKeyFrame)
                        {
                            return new FilePosition(fileName, targetTimeOffset, fileOffset, requestedPosition.MediaType, diff);
                        }
                        else
                            return null;
                    }
                } //while

                //calculate again
                diff = (int)((long)finalTimeOffset - (long)startTimeOffset);
                string.Format("Found keyframe at offset : {0} ms, delta = {1} ms", startTimeOffset, diff).Log(LogLevel.logDetail);
            }
            if (!isStart && fileOffset <= maxOffset)
                fileOffset += asfConfig.AsfPacketSize; //include current packet

            fs.Dispose();

            if (found)
                return new FilePosition(fileName, targetTimeOffset, fileOffset, requestedPosition.MediaType, diff);
            else return null; //cannot find any earlier keyframe, just return position/file as is
        }


        public bool Open()
        {
            try
            {
                _fileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                // seek to the starting offset
                return _fileStream.Seek(StartOffset, SeekOrigin.Begin) == StartOffset;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                GC.SuppressFinalize(this);
            }
        }

        public void Close()
        {
            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream = null;
            }
        }

        public int Read(byte[] array, int offset, int count)
        {
            if (_fileStream == null && !Open())
                return -1;
            else
            {
                if (_fileStream.Position >= EndOffset)
                    return 0;
                else
                    return _fileStream.Read(array, offset, Math.Min(count, (int)(EndOffset - _fileStream.Position)));
            }
        }

        public uint GetAveragePacketDuration()
        {
            uint duration = 0;

            if (MediaType == FileMediaType.Audio)
                duration = EndTimeOffsetAudio - StartTimeOffsetAudio;
            else
                duration = EndTimeOffsetVideo - StartTimeOffsetVideo;

            uint packetCount = (uint)(EndOffset / _asfConfig.AsfPacketSize);
            return duration / packetCount;
        }


        private bool SetAsfFileProperties(AsfFileConfiguration asfConfig, FileInfo fi)
        {
            bool isKeyframe = false;
            uint startTimeOffsetVideo = 0;
            uint endTimeOffsetVideo = 0;
            uint startTimeOffsetAudio = 0;
            uint endTimeOffsetAudio = 0;

            bool hasVideoStream = asfConfig.ImageWidth > 0;


            using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(asfConfig.AsfHeaderSize, SeekOrigin.Begin);
                long maxOffset = asfConfig.AsfHeaderSize + (asfConfig.AsfPacketCount - 1) * asfConfig.AsfPacketSize;
                if (maxOffset < 0)
                    return false;

                if (hasVideoStream)
                {
                    if (!FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref startTimeOffsetVideo, asfConfig.AsfVideoStreamId))
                        return false;
                    fs.Seek(asfConfig.AsfHeaderSize, SeekOrigin.Begin);
                }


                if (!FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref startTimeOffsetAudio, asfConfig.AsfAudioStreamId))
                    return false;

                //now position at last packet
                if (hasVideoStream)
                {
                    fs.Seek(maxOffset, SeekOrigin.Begin);
                    while (!FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref endTimeOffsetVideo, asfConfig.AsfVideoStreamId, false))
                    {
                        maxOffset -= asfConfig.AsfPacketSize;
                        if (maxOffset <= asfConfig.AsfHeaderSize)
                            return false;
                        fs.Seek(maxOffset, SeekOrigin.Begin);
                    }
                }

                //now position at last packet
                fs.Seek(maxOffset, SeekOrigin.Begin);

                while (!FindNextTimeOffset(asfConfig, fs, out isKeyframe, ref endTimeOffsetAudio, asfConfig.AsfAudioStreamId, false))
                {
                    maxOffset -= asfConfig.AsfPacketSize;
                    if (maxOffset <= asfConfig.AsfHeaderSize)
                        return false;
                    fs.Seek(maxOffset, SeekOrigin.Begin);
                }
            }

            double fileDuration = 0;

            if (hasVideoStream)
                fileDuration = endTimeOffsetVideo - startTimeOffsetVideo;
            else
                fileDuration = endTimeOffsetAudio - startTimeOffsetAudio;

            fileDuration /= 1000; //set to milisecond resolution

            StartTimeOffsetVideo = startTimeOffsetVideo;
            EndTimeOffsetVideo = endTimeOffsetVideo;
            StartTimeOffsetAudio = startTimeOffsetAudio;
            EndTimeOffsetAudio = endTimeOffsetAudio;

            var dataObject = GetAsfObject<AsfDataObject>();
            EndOffset = dataObject.Position + dataObject.Size;
            MediaType = endTimeOffsetVideo > 0 ? FileMediaType.Video : FileMediaType.Audio;

            return true;
        }

        internal static bool FindNextTimeOffset(AsfFileConfiguration asfConfig, FileStream fs, out bool isKeyFrame, ref uint timeOffset, uint streamId, bool seekForward = true)
        {
            bool foundTime = false;
            bool isFirstPacket = true;
            long maxOffset = asfConfig.AsfHeaderSize + (asfConfig.AsfPacketCount - 1) * asfConfig.AsfPacketSize;

            isKeyFrame = false;

            while (fs.Position < maxOffset && !foundTime && (seekForward || isFirstPacket))
            {
                isFirstPacket = false;
                byte[] packetHeader = new byte[asfConfig.AsfPacketSize];
                if (fs.Read(packetHeader, 0, (int)asfConfig.AsfPacketSize) != asfConfig.AsfPacketSize)
                    break; //reached end of file

                timeOffset = 0;
                isKeyFrame = false;
                AsfPacket currentPacket = new AsfPacket(asfConfig, packetHeader);

                var candidatePayloads = from payload in currentPacket.Payload where payload.StreamId == streamId select payload;

                foreach (PayloadInfo pi in candidatePayloads)
                {
                    foundTime = true;
                    timeOffset = pi.PresentationTime - asfConfig.AsfPreroll;
                    isKeyFrame = currentPacket.IsKeyFrame;
                    if (pi.OffsetIntoMedia == 0)
                        return true;
                }
            }
            return foundTime;
        }

    }
}

