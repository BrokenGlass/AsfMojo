using System;
using System.Collections.Generic;
using System.IO;
using AsfMojo.File;
using AsfMojo.Configuration;
using System.Linq;


namespace AsfMojo.Media
{
    public enum AsfStreamType { asfStream = 0, asfUnaltered, asfFile, asfImage, asfAudio}

    /// <summary>
    /// Exception raised from AsfStream if no stream could be instantiated with the parameters specified
    /// </summary>
    public class AsfStreamException : Exception
    {
       public AsfStreamException()
       {
       }

       public AsfStreamException(string message)
           : base(message)
       {
       }

    }

    /// <summary>
    /// Wraps a segment of an ASF media file into a stream that can be read from
    /// </summary>
    public class AsfStream : Stream
    {
        public override bool CanSeek { get { return _allowSeekBack; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanRead { get { return true; } }
        public override long Position { get; set; }

        private long _length = 0;
        public override long Length { get { return _length; } }

        private AsfFile _asfFile;
        public AsfStreamType StreamType { get; private set; }
        protected AsfStreamInfo _streamInfo;

        protected MemoryStream _readBuffer = new MemoryStream();
        private const int _maxInternalBufferLength = 500000;
        private bool _isHeaderStreamed = false;
        private bool _isFirstPacket = true;
        protected bool _allowSeekBack = true;


        protected AsfFileConfiguration _asfConfig = new AsfFileConfiguration();
        public AsfFileConfiguration Configuration { get { return _asfConfig; } }
        public AsfStreamInfo StreamInfo { get { return _streamInfo; } }

        #region Constructors

        internal AsfStream() { }


        public AsfStream(AsfFile asfFile, AsfStreamType streamType, double startOffset)
            : this(asfFile, streamType, startOffset, 0)
        {
        }

        public AsfStream(AsfFile asfFile, AsfStreamType streamType, double startOffset, double endOffset) 
        {
            _asfFile = asfFile;
            Init(streamType, startOffset, endOffset);
        }


        public AsfStream(AsfStreamType streamType, string fileName, double offset)
            : this(streamType, fileName, offset, 0)
        {
        }

        public AsfStream(AsfStreamType streamType, string fileName, double startOffset, double endOffset)
        {
            _asfFile = new AsfFile(fileName);
            Init(streamType, startOffset, endOffset);
        }

        private void Init(AsfStreamType streamType, double startOffset, double endOffset)
        {
            StreamType = streamType;
            if (StreamType == AsfStreamType.asfImage)
            {
                endOffset = 0; //use all available data
            }

            if (startOffset < 0 || endOffset < 0 || ((streamType != AsfStreamType.asfStream && streamType != AsfStreamType.asfImage && streamType != AsfStreamType.asfAudio) && endOffset < startOffset))
                throw new ArgumentOutOfRangeException();

            if (streamType == AsfStreamType.asfImage && _asfFile.PacketConfiguration.ImageWidth == 0)
            {
                throw new AsfStreamException("Cannot create image stream for audio file");
            }

            _streamInfo = new AsfStreamInfo(StreamType);

            FilePosition fileStartPos;
            FilePosition fileEndPos;

            bool status = _asfFile.SetOffsetRange(startOffset, endOffset, out fileStartPos, out fileEndPos, streamType);

            if (!status)
                throw new AsfStreamException("Asf stream data within required offsets not found"); // could not find file data covering the requested start and end offsets


            _asfConfig = _asfFile.PacketConfiguration;

            _streamInfo.StartTimeOffset = fileStartPos.TimeOffset;
            _streamInfo.EndTimeOffset = fileEndPos.TimeOffset;

            if (StreamType == AsfStreamType.asfStream)
                _length = Math.Min(int.MaxValue, _asfFile.Length + _asfConfig.AsfHeaderSize);
            else if (StreamType == AsfStreamType.asfUnaltered)
                _length = _asfFile.Length;
            else
                _length = _asfFile.Length + _asfConfig.AsfHeaderSize;
        }

        #endregion

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            long internalBufferPos;

            if (_readBuffer.Length - _readBuffer.Position >= count)
            {
                return _readBuffer.Read(buffer, offset, count);
            }

            byte[] packetBuffer = new byte[_asfConfig.AsfPacketSize];

            internalBufferPos = _readBuffer.Position;
            _readBuffer.Seek(0, SeekOrigin.End);

            while (_readBuffer.Length - internalBufferPos < count)
            {
                //fill buffer
                if (!_isHeaderStreamed && StreamType != AsfStreamType.asfUnaltered)
                {
                    byte[] headerData = _asfFile.GetStreamingHeader();
                     _readBuffer.Write(headerData, 0, headerData.Length);
                    _isHeaderStreamed = true;
                }
                else
                {
                    bytesRead = _asfFile.Read(packetBuffer, 0,  (int)_asfConfig.AsfPacketSize);
                    if (bytesRead == _asfConfig.AsfPacketSize)
                    {
                        AsfPacket currentPacket = new AsfPacket(_asfConfig, packetBuffer);
                        if (_isFirstPacket)
                        {
                            _streamInfo.ResetMediaObjects();
                            _streamInfo.StartSendTime = currentPacket.SendTime;
                            currentPacket.SetStart(Configuration, _streamInfo);
                            _isFirstPacket = false;
                        }
                        currentPacket.SetFollowup(Configuration, _streamInfo);
                        _readBuffer.Write(packetBuffer, 0, bytesRead);
                    }
                    else
                        break;
                }
            }

            //the memory buffer now contains enough bytes to satisfy the request
            _readBuffer.Seek(internalBufferPos, SeekOrigin.Begin);
            bytesRead = _readBuffer.Read(buffer, offset, count);

            if (_readBuffer.Length > _maxInternalBufferLength)
            {
                int bytesLeft = (int)(_readBuffer.Length - _readBuffer.Position);

                byte[] tempStreamBuffer = new byte[bytesLeft];

                _readBuffer.Read(tempStreamBuffer, 0, bytesLeft);
                _readBuffer = new MemoryStream();
                _readBuffer.Write(tempStreamBuffer, 0, bytesLeft);
                _readBuffer.Seek(0, SeekOrigin.Begin);
                _allowSeekBack = false;
            }
            return bytesRead;
        }

        public override void Close()
        {
            _asfFile.Close();
            base.Close();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _asfFile.Close();
                _readBuffer.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if ((StreamType == AsfStreamType.asfImage || StreamType == AsfStreamType.asfAudio) && origin == SeekOrigin.Begin && _allowSeekBack)
            {
                return _readBuffer.Seek(offset, origin);
            }
            else
                throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public virtual void WriteTo(Stream stream)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[8192];

            do
            {
                bytesRead = Read(buffer, 0, buffer.Length);
                stream.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);
        }
    }
}
