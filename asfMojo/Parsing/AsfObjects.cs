using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using AsfMojo.Configuration;
using AsfMojo.Media;
using AsfMojo.Utils;

namespace AsfMojo.Parsing
{
    /// <summary>
    /// Wrapper class around ASF objects represented by Guid
    /// Use the factory method CreateAsfObject to create an actual instance
    /// </summary>
    public class AsfObject
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public long Position { get; set; }
        public long Size { get; set; }

        protected Stream _stream;
        protected byte[] _rawData;

        public AsfObject(Stream stream, string name="Asf Object")
        {
            Name = name;
            _stream = stream;


            AsfMojoObject someObject = ReadStruct<AsfMojoObject>(_stream);
            Size = (long)someObject.object_size;
            Guid  = someObject.object_id.ToGuid();

            //seek back to beginning of structure
            _stream.Seek(_stream.Position - Marshal.SizeOf(typeof(AsfMojoObject)), SeekOrigin.Begin);
            Position = _stream.Position;

            //copy raw data

            if (Guid != AsfGuid.ASF_Data_Object)
            {
                _rawData = new byte[Size];
                int bytesRead = 0;
                while (bytesRead < Size)
                {
                    bytesRead += _stream.Read(_rawData, 0, (int)Size - bytesRead);
                }
            }
            else
                _rawData = new byte[0];

            //seek back to beginning of structure
            _stream.Seek(Position, SeekOrigin.Begin);
        }

        public virtual void Serialize(AsfFileConfiguration config, Stream stream)
        {
            //just write out raw data, unchanged
            stream.Write(_rawData, 0, _rawData.Length);
        }

        public virtual int GetLength()
        {
            return _rawData.Length;
        }

        /// <summary>
        /// Creates an AsfMojo wrapper object
        /// </summary>
        /// <param name="objGuid">The ASF Guid for which to create a wrapper object for</param>
        /// <param name="format">The stream to read the object from</param>
        /// <param name="format">The packet configuration to update</param>
        public static AsfObject CreateAsfObject(Guid objGuid, Stream stream, AsfFileConfiguration config)
        {
            if (objGuid == AsfGuid.ASF_Bitrate_Mutual_Exclusion_Object) return new AsfBitrateMutualExclusionObject(stream);
            if (objGuid == AsfGuid.ASF_Codec_List_Object) return new AsfCodecListObject(stream);
            if (objGuid == AsfGuid.ASF_Compatibility_Object) return new AsfCompatibilityObject(stream);
            if (objGuid == AsfGuid.ASF_Content_Description_Object) return new AsfContentDescriptionObject(stream);
            if (objGuid == AsfGuid.ASF_Data_Object) return new AsfDataObject(stream, config);
            if (objGuid == AsfGuid.ASF_Extended_Content_Description_Object) return new AsfExtendedContentDescription(stream);
            if (objGuid == AsfGuid.ASF_Extended_Stream_Properties_Object) return new AsfExtendedStreamProperties(stream);
            if (objGuid == AsfGuid.ASF_File_Properties_Object) return new AsfFileProperties(stream, config);
            if (objGuid == AsfGuid.ASF_Header_Extension_Object) return new AsfHeaderExtension(stream);
            if (objGuid == AsfGuid.ASF_Header_Object) return new AsfFileHeader(stream, config);
            if (objGuid == AsfGuid.ASF_Index_Object) return new AsfIndexObject(stream, config);
            if (objGuid == AsfGuid.ASF_Index_Parameters_Placeholder_Object) return new AsfIndexParametersPlaceholder(stream);
            if (objGuid == AsfGuid.ASF_Language_List_Object) return new AsfLanguageListObject(stream);
            if (objGuid == AsfGuid.ASF_Metadata_Object) return new AsfMetadataObject(stream);
            if (objGuid == AsfGuid.ASF_Padding_Object) return new AsfPaddingObject(stream);
            if (objGuid == AsfGuid.ASF_Script_Command_Object) return new AsfScriptCommandObject(stream);
            if (objGuid == AsfGuid.ASF_Simple_Index_Object) return new AsfSimpleIndexObject(stream, config);
            if (objGuid == AsfGuid.ASF_Stream_Bitrate_Properties_Object) return new AsfStreamBitrateProperties(stream, config);
            if (objGuid == AsfGuid.ASF_Stream_Prioritization_Object) return new AsfStreamPrioritizationObject(stream);
            if (objGuid == AsfGuid.ASF_Stream_Properties_Object) return new AsfStreamPropertiesObject(stream, config);
            if (objGuid == AsfGuid.ASF_Timecode_Index_Parameters_Object) return new AsfTimecodeIndexParametersObject(stream);

            return new AsfUnknownObject(stream);
        }

        protected static T ReadStruct<T>(Stream stream)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            stream.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T typedStruct = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return typedStruct;
        }

        protected static void WriteStruct<T>(T inputStruct, Stream stream) where T: struct
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);//Allocate the buffer to memory and pin it so that GC cannot use the space (Disable GC) 
            Marshal.StructureToPtr(inputStruct, handle.AddrOfPinnedObject(), false);// copy the struct into int byte[] mem alloc 
            handle.Free(); //Allow GC to do its job 
            stream.Write(buffer, 0, buffer.Length);
        }

        protected static void GetStructProperties<T>(T myStruct, ref Dictionary<string, object> props) where T : struct
        {
            Type t = myStruct.GetType();
            System.Reflection.FieldInfo[] fields = t.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                props.Add(field.Name, field.GetValue(myStruct));
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Header_Object
    /// </summary>
    public class AsfFileHeader : AsfObject
    {
        public uint HeaderSize { get; set; }
        private AsfMojoFileHeader _asfFileHeader;

        public AsfFileHeader(Stream stream, AsfFileConfiguration config = null)
            : base(stream, "Header Object")
        {
            long streamPosition = stream.Position;
            _asfFileHeader = ReadStruct<AsfMojoFileHeader>(_stream);

            HeaderSize = (uint)_asfFileHeader.object_size;

            if(config!=null)
                config.AsfPacketHeaderSize = (uint)_asfFileHeader.object_size;
        }

        public override void Serialize(AsfFileConfiguration config, Stream stream)
        {
            _asfFileHeader.object_size = HeaderSize;
            WriteStruct<AsfMojoFileHeader>(_asfFileHeader, stream);
            //stream.Write(_rawData, 0, (int)Marshal.SizeOf(typeof(AsfMojoFileHeader)));
        }

        public override int GetLength()
        {
            return Marshal.SizeOf(typeof(AsfMojoFileHeader));
        }

    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Stream_Bitrate_Properties_Object
    /// </summary>
    public class AsfStreamBitrateProperties : AsfObject
    {
        public Dictionary<int, uint> Bitrate { get; set; }

        public AsfStreamBitrateProperties(Stream stream, AsfFileConfiguration config = null)
            : base(stream, "Stream Bitrate Properties")
        {
            AsfMojoStreamBitrateProperties asfStreamBitrateProperties = ReadStruct<AsfMojoStreamBitrateProperties>(_stream);

            Bitrate = new Dictionary<int, uint>();

            for (int i = 0; i < asfStreamBitrateProperties.bitrate_records_count; i++)
            {
                AsfMojoBitrateRecord asfBitrateRecord = ReadStruct<AsfMojoBitrateRecord>(_stream);
                //do something with stream bitrate properties here
                int streamNumber = asfBitrateRecord.flags & 255; //LSB 7 bits
                Bitrate.Add(streamNumber, asfBitrateRecord.average_bitrate);
            }
        }
    }


    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_File_Properties_Object
    /// </summary>
    public class AsfFileProperties : AsfObject
    {
        public Guid FileId { get; set; }
        public ulong FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        public TimeSpan Duration { get; set; }
        public ulong PlayDuration { get; set; }
        public ulong SendDuration { get; set; }
        public ulong PacketCount { get; set; }
        public ulong Preroll { get; set; }
        public uint Flags { get; set; }
        public uint MinPacketSize { get; set; }
        public uint MaxPacketSize { get; set; }
        public uint MaxBitRate { get; set; }
        public bool IsBroadcast { get; set; }
        public bool IsSeekable { get; set; }

        protected AsfMojoFileProperties _asfFileProperties;

        public AsfFileProperties(Stream stream, AsfFileConfiguration config = null)
            : base(stream, "File Properties Object")
        {
            _asfFileProperties = ReadStruct<AsfMojoFileProperties>(_stream);

            CreationTime  = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)_asfFileProperties.creation_date);
            FileId = _asfFileProperties.file_id.ToGuid();
            FileSize = _asfFileProperties.file_size;
            PacketCount = _asfFileProperties.data_packet_count;

            PlayDuration = _asfFileProperties.play_duration;
            SendDuration = _asfFileProperties.send_duration;
            Preroll = _asfFileProperties.preroll;
            Flags = _asfFileProperties.flags;

            Duration  = TimeSpan.FromTicks((long)PlayDuration) - TimeSpan.FromMilliseconds(Preroll);


            if (config != null)
            {
                config.AsfPacketSize = _asfFileProperties.max_data_packet_size;
                config.AsfPreroll = (uint)_asfFileProperties.preroll;
                config.AsfPacketCount = (uint)_asfFileProperties.data_packet_count;
                config.Duration = (TimeSpan.FromTicks((long)PlayDuration) - TimeSpan.FromMilliseconds(Preroll)).TotalSeconds;
                config.AsfBitRate = _asfFileProperties.max_bitrate;
            }

            MinPacketSize = _asfFileProperties.min_data_packet_size;
            MaxPacketSize = _asfFileProperties.max_data_packet_size;
            MaxBitRate = _asfFileProperties.max_bitrate;

            IsBroadcast = (Flags & 1) > 0;
            IsSeekable = (Flags & 2) > 0;
        }

        public override void Serialize(AsfFileConfiguration config, Stream stream)
        {
            using (MemoryStream ms = new MemoryStream(_rawData))
            {
                ms.Position = 0;

                _asfFileProperties.creation_date = (ulong)(CreationTime.Ticks - new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks);
                _asfFileProperties.data_packet_count = PacketCount;
                _asfFileProperties.file_id = FileId.ToByteArray();
                _asfFileProperties.file_size = FileSize;

                uint flags = 0;
                if (IsBroadcast) flags |= 1;
                if (IsSeekable) flags |= 2;

                _asfFileProperties.flags = flags;
                _asfFileProperties.max_bitrate = MaxBitRate;
                _asfFileProperties.max_data_packet_size = config.AsfPacketSize;

                WriteStruct<AsfMojoFileProperties>(_asfFileProperties, ms);

                ms.WriteTo(stream);
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Header_Extension_Object
    /// </summary>
    public class AsfHeaderExtension : AsfObject
    {
        public Guid R1Guid { get; set; }
        public ushort ClockSize { get; set; }
        public uint ExtendedHeaderSize { get; set; }

        private AsfMojoHeaderExtension _asfMojoHeaderExtension;

        public AsfHeaderExtension(Stream stream)
            : base(stream, "Header Extension Object")
        {
            _asfMojoHeaderExtension = ReadStruct<AsfMojoHeaderExtension>(_stream);

            R1Guid = _asfMojoHeaderExtension.r1.ToGuid();
            ClockSize = _asfMojoHeaderExtension.r2;
            ExtendedHeaderSize = _asfMojoHeaderExtension.header_extension_data_size;
        }

        public override void Serialize(AsfFileConfiguration config, Stream stream)
        {
            WriteStruct<AsfMojoHeaderExtension>(_asfMojoHeaderExtension, stream);
        }

        public override int GetLength()
        {
            return Marshal.SizeOf(typeof(AsfMojoHeaderExtension));
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Language_List_Object
    /// </summary>
    public class AsfLanguageListObject : AsfObject
    {
        public List<string> Languages;

        public AsfLanguageListObject(Stream stream)
            : base(stream, "Language List Object")
        {
            AsfMojoLanguageListObject asfLanguageListObject = ReadStruct<AsfMojoLanguageListObject>(_stream);

            Languages = new List<string>();

            for (int i = 0; i < asfLanguageListObject.language_id_records_count; i++)
            {
                byte asfLanguageIDRecordLength = (byte)_stream.ReadByte();
                byte[] languageId = new byte[asfLanguageIDRecordLength];
                _stream.Read(languageId, 0, asfLanguageIDRecordLength);

                string language = Encoding.Unicode.GetString(languageId, 0, languageId.Length).Trim('\0'); //trim null-termination from end of string
                Languages.Add(language);
            }

        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Stream_Properties_Object
    /// </summary>
    public class AsfExtendedStreamProperties : AsfObject
    {
        public ushort StreamNumber { get; set; }
        public ulong StartTime { get; set; }
        public ulong EndTime { get; set; }
        public ulong AvgTimePerFrame { get; set; }
        public uint MaxObjectSize { get; set; }
        public uint DataBitrate { get; set; }
        public uint MaxDataBitrate { get; set; }
        public uint BufferSize { get; set; }
        public uint AlternateBufferSize { get; set; }
        public uint Flags { get; set; }
        public bool IsReliable { get; set; }
        public bool IsSeekable { get; set; }
        public bool HasNoCleanpoints { get; set; }
        public bool DoResendLiveCleanpoints { get; set; }
        public ushort LanguageIndex { get; set; }

        Dictionary<UInt16, string> StreamNames;

        public AsfExtendedStreamProperties(Stream stream)
            : base(stream, "Extended Stream Properties Object")
        {
            AsfMojoExtendedStreamProperties asfExtendedStreamProperties = ReadStruct<AsfMojoExtendedStreamProperties>(_stream);

            Name = string.Format("Extended Stream Properties Object [{0}]", asfExtendedStreamProperties.stream_number);
            StreamNumber = asfExtendedStreamProperties.stream_number;
            StartTime = asfExtendedStreamProperties.start_time;
            EndTime = asfExtendedStreamProperties.end_time;
            AvgTimePerFrame = asfExtendedStreamProperties.avg_time_per_frame;
            MaxObjectSize =  asfExtendedStreamProperties.max_object_size;
            DataBitrate = asfExtendedStreamProperties.data_bitrate;
            MaxDataBitrate = asfExtendedStreamProperties.alternate_data_bitrate;
            BufferSize = asfExtendedStreamProperties.buffer_size;
            AlternateBufferSize = asfExtendedStreamProperties.alternate_buffer_size;
            Flags = asfExtendedStreamProperties.flags;

            IsReliable = (asfExtendedStreamProperties.flags & 1) > 0;
            IsSeekable = (asfExtendedStreamProperties.flags & 2) > 0;
            HasNoCleanpoints = (asfExtendedStreamProperties.flags & 4) > 0;
            DoResendLiveCleanpoints = (asfExtendedStreamProperties.flags & 8) > 0;
            LanguageIndex = asfExtendedStreamProperties.stream_language_id_idx;

            StreamNames = new Dictionary<ushort, string>();

            for (int i = 0; i < asfExtendedStreamProperties.stream_name_count; i++)
            {
                byte[] data = new byte[2];
                _stream.Read(data, 0, data.Length);
                UInt16 languageIdIndex = BitConverter.ToUInt16(data, 0);
                _stream.Read(data, 0, data.Length);
                UInt16 streamNameLength = BitConverter.ToUInt16(data, 0);
                byte[] name = new byte[streamNameLength];
                _stream.Read(name, 0, name.Length);
                string streamName = Encoding.Unicode.GetString(name, 0, name.Length);

                StreamNames.Add(languageIdIndex, streamName);
            }

            for (int i = 0; i < asfExtendedStreamProperties.payload_extension_system_count; i++)
            {
                AsfMojoPayloadExtension asfPayloadExtension = ReadStruct<AsfMojoPayloadExtension>(_stream);
                if (asfPayloadExtension.extension_system_id.ToGuid() == AsfGuid.ASF_Payload_Extension_System_Pixel_Aspect_Ratio)
                {
                    if (asfPayloadExtension.extension_system_info_length == 2)
                    {
                        byte pixelAspectRatioX = (byte)_stream.ReadByte();
                        byte pixelAspectRatioY = (byte)_stream.ReadByte();
                    }
                }
                else if (asfPayloadExtension.extension_system_id.ToGuid() == AsfGuid.ASF_Payload_Extension_System_Sample_Duration)
                {
                    if (asfPayloadExtension.extension_system_info_length == 2)
                    {
                        byte[] data = new byte[2];
                        _stream.Read(data, 0, data.Length);
                        UInt16 mediaObjectSampleDuration = BitConverter.ToUInt16(data, 0);
                    }
                }
                else if (asfPayloadExtension.extension_system_id.ToGuid() == AsfGuid.ASF_Payload_Extension_System_Degradable_JPEG)
                {
                    //BETA TODO
                }
                else
                {
                    //BETA TODO
                }
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Compatibility_Object
    /// </summary>
    public class AsfCompatibilityObject : AsfObject
    {
        public AsfCompatibilityObject(Stream stream)
            : base(stream, "Compatibility Object")
        {
            AsfMojoCompatibilityObject asfCompatibilityObject = ReadStruct<AsfMojoCompatibilityObject>(_stream);
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Stream_Properties_Object
    /// </summary>
    public class AsfStreamPropertiesObject : AsfObject
    {
        public ushort StreamNumber { get; set; }
        public bool IsEncrypted { get; set; }
        public Guid StreamType { get; set; }
        public Dictionary<string, object> StreamProperties;

        public AsfStreamPropertiesObject(Stream stream, AsfFileConfiguration config = null)
            : base(stream, "Stream Properties Object")
        {
            AsfMojoStreamPropertiesObject asfStreamPropertiesObject = ReadStruct<AsfMojoStreamPropertiesObject>(_stream);
            int streamNumber = asfStreamPropertiesObject.flags & 255; //LSB 7 bits
            Name = string.Format("Stream Properties Object [{0}]", streamNumber);

            StreamNumber = (ushort)streamNumber;
            IsEncrypted = (asfStreamPropertiesObject.flags & 32768) > 0;

            Dictionary<string, object> streamSectionProperties = new Dictionary<string, object>();
            StreamType = asfStreamPropertiesObject.stream_type.ToGuid();
            StreamProperties = new Dictionary<string, object>();


            if (asfStreamPropertiesObject.stream_type.ToGuid() == AsfGuid.ASF_Audio_Media)
            {
                AsfMojoAudioStreamProperties asfAudioStreamProperties = ReadStruct<AsfMojoAudioStreamProperties>(_stream);
                StreamProperties.Add("AudioStreamProperties", asfAudioStreamProperties);

                if (config != null)
                {
                    config.AsfAudioStreamId = (uint)streamNumber;
                    config.AudioChannels = asfAudioStreamProperties.number_channels;
                    config.AudioSampleRate = asfAudioStreamProperties.samples_per_second;
                    config.AudioBitsPerSample = asfAudioStreamProperties.bits_per_sample;
                }

                //skip codec specific data and error correction data
                _stream.Seek(_stream.Position + asfAudioStreamProperties.codec_specific_data_size + asfStreamPropertiesObject.error_correction_data_length, SeekOrigin.Begin);
            }
            else if (asfStreamPropertiesObject.stream_type.ToGuid() == AsfGuid.ASF_Video_Media)
            {
                AsfMojoVideoStreamProperties asfVideoStreamProperties = ReadStruct<AsfMojoVideoStreamProperties>(_stream);
                StreamProperties.Add("VideoStreamProperties", asfVideoStreamProperties);

                AsfMojoVideoStreamFormatData asfVideoStreamFormatData = ReadStruct<AsfMojoVideoStreamFormatData>(_stream);
                StreamProperties.Add("VideoStreamFormatData", asfVideoStreamFormatData);

                if (config != null)
                {
                    config.AsfVideoStreamId = (uint)streamNumber;
                    config.ImageWidth = (int)asfVideoStreamFormatData.image_width;
                    config.ImageHeight = (int)asfVideoStreamFormatData.image_height;
                }

                byte[] codec_specific_data = new byte[asfVideoStreamFormatData.format_data_size - (long)Marshal.SizeOf(typeof(AsfMojoVideoStreamFormatData))];
                _stream.Read(codec_specific_data, 0, codec_specific_data.Length);

                //skip error correction data
                _stream.Seek(_stream.Position + asfStreamPropertiesObject.error_correction_data_length, SeekOrigin.Begin);
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Metadata_Object
    /// </summary>
    public class AsfMetadataObject : AsfObject
    {
        public List<AsfProperty> DescriptionRecords;

        public AsfMetadataObject(Stream stream)
            : base(stream, "Metadata Object")
        {
            AsfMojoMetadataObject asfMetadataObject = ReadStruct<AsfMojoMetadataObject>(_stream);

            DescriptionRecords = new List<AsfProperty>();

            for (int i = 0; i < asfMetadataObject.description_records_count; i++)
            {
                AsfMojoDescriptionRecord asfDescriptionRecord = ReadStruct<AsfMojoDescriptionRecord>(_stream);
                byte[] name = new byte[asfDescriptionRecord.name_length];
                _stream.Read(name, 0, name.Length);
                string nameProperty = Encoding.Unicode.GetString(name, 0, name.Length).Trim('\0'); //trim null-termination from end of string
                byte[] data = new byte[asfDescriptionRecord.data_length];
                _stream.Read(data, 0, data.Length);

                switch (asfDescriptionRecord.data_type)
                {
                    case 0:
                        string textValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0');
                        DescriptionRecords.Add(new AsfProperty() { StreamNumber = asfDescriptionRecord.stream_number, Name = nameProperty, Value = textValue });
                        break;
                    case 1:
                        break;
                    case 2:
                        bool boolVal = Convert.ToBoolean(BitConverter.ToUInt16(data, 0));
                        DescriptionRecords.Add(new AsfProperty() { StreamNumber = asfDescriptionRecord.stream_number, Name = nameProperty, Value = boolVal });
                        break;
                    case 3:
                        UInt32 dwordVal = BitConverter.ToUInt32(data, 0);
                        DescriptionRecords.Add(new AsfProperty() { StreamNumber = asfDescriptionRecord.stream_number, Name = nameProperty, Value = dwordVal });
                        break;
                    case 4:
                        UInt64 qwordVal = BitConverter.ToUInt64(data, 0);
                        DescriptionRecords.Add(new AsfProperty() { StreamNumber = asfDescriptionRecord.stream_number, Name = nameProperty, Value = qwordVal });
                        break;
                    case 5:
                        UInt16 wordVal = BitConverter.ToUInt16(data, 0);
                        DescriptionRecords.Add(new AsfProperty() { StreamNumber = asfDescriptionRecord.stream_number, Name = nameProperty, Value = wordVal });
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Index_Parameters_Placeholder_Object
    /// </summary>
    public class AsfIndexParametersPlaceholder : AsfObject
    {
        public AsfIndexParametersPlaceholder(Stream stream)
            : base(stream, "Index Parameters Placeholder Object")
        {
            AsfMojoIndexParametersPlaceholder asfIndexParametersPlaceholder = ReadStruct<AsfMojoIndexParametersPlaceholder>(_stream);

            //just skip over 
            long seekPos = _stream.Position - Marshal.SizeOf(typeof(AsfMojoIndexParametersPlaceholder)) + (long)asfIndexParametersPlaceholder.object_size;
            _stream.Seek(seekPos, SeekOrigin.Begin);
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Padding_Object
    /// </summary>
    public class AsfPaddingObject : AsfObject
    {
        public AsfPaddingObject(Stream stream)
            : base(stream, "Padding Object")
        {
            //just skip over 
            AsfMojoPaddingObject asfPaddingObject = ReadStruct<AsfMojoPaddingObject>(_stream);
            long seekPos = _stream.Position - Marshal.SizeOf(typeof(AsfMojoPaddingObject)) + (long)asfPaddingObject.object_size;
            _stream.Seek(seekPos, SeekOrigin.Begin);
        }
    }


    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Extended_Content_Description_Object
    /// </summary>
    public class AsfExtendedContentDescription : AsfObject
    {
        public Dictionary<string, object> ContentDescriptions;


        public AsfExtendedContentDescription(Stream stream)
            : base(stream, "Extended Content Description Object")
        {
            AsfMojoExtendedContentDescription asfExtendedContentDescription = ReadStruct<AsfMojoExtendedContentDescription>(_stream);

            ContentDescriptions = new Dictionary<string, object>();

            for (int i = 0; i < asfExtendedContentDescription.content_descriptors_count; i++)
            {
                byte[] data = new byte[2];
                _stream.Read(data, 0, data.Length);

                UInt16 length;
                byte[] content;

                length = BitConverter.ToUInt16(data, 0);

                content = new byte[length];
                _stream.Read(content, 0, content.Length);
                string descriptorName = Encoding.Unicode.GetString(content, 0, content.Length).Trim('\0'); //trim null-termination from end of string
                _stream.Read(data, 0, data.Length);
                UInt16 descriptorValueDataType = BitConverter.ToUInt16(data, 0);
                _stream.Read(data, 0, data.Length);
                length = BitConverter.ToUInt16(data, 0);
                data = new byte[length];
                _stream.Read(data, 0, data.Length);

                switch (descriptorValueDataType)
                {
                    case 0:
                        string textValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0');
                        ContentDescriptions.Add(descriptorName, textValue);
                        break;
                    case 1:
                        break;
                    case 2:
                        bool boolVal = Convert.ToBoolean(BitConverter.ToUInt16(data, 0));
                        ContentDescriptions.Add(descriptorName, boolVal);
                        break;
                    case 3:
                        UInt32 dwordVal = BitConverter.ToUInt32(data, 0);
                        ContentDescriptions.Add(descriptorName, dwordVal);
                        break;
                    case 4:
                        UInt64 qwordVal = BitConverter.ToUInt64(data, 0);
                        ContentDescriptions.Add(descriptorName, qwordVal);
                        break;
                    case 5:
                        UInt16 wordVal = BitConverter.ToUInt16(data, 0);
                        ContentDescriptions.Add(descriptorName, wordVal);
                        break;
                }


            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Codec_List_Object
    /// </summary>
    public class AsfCodecListObject : AsfObject
    {
        public List<Dictionary<string, object>> CodecProperties;
        public Guid CodecId { get; set; }

        public AsfCodecListObject(Stream stream)
            : base(stream, "Codec List Object")
        {

            AsfMojoCodecListObject asfCodecListObject = ReadStruct<AsfMojoCodecListObject>(_stream);
            CodecId = asfCodecListObject.reserved.ToGuid();

            CodecProperties = new List<Dictionary<string, object>>();

            for (int i = 0; i < asfCodecListObject.codec_entries_count; i++)
            {
                Dictionary<string, object> codecProperties = new Dictionary<string, object>();
                CodecProperties.Add(codecProperties);

                byte[] data = new byte[2];
                _stream.Read(data, 0, data.Length);
                UInt16 codecType = BitConverter.ToUInt16(data, 0);

                codecProperties.Add("Type", codecType == 1 ? "Video Codec" : codecType == 2 ? "Audio Codec" : "Unknown Codec");

                UInt16 length;
                byte[] content;

                _stream.Read(data, 0, data.Length);
                length = BitConverter.ToUInt16(data, 0);
                content = new byte[length * 2];//length is number of unicode chars, need to mult*2 to account for UTF16 encoding
                _stream.Read(content, 0, content.Length);
                string codecName = Encoding.Unicode.GetString(content, 0, content.Length).Trim('\0'); //trim null-termination from end of string
                codecProperties.Add("Name", codecName);

                _stream.Read(data, 0, data.Length);
                length = BitConverter.ToUInt16(data, 0);
                content = new byte[length * 2];//length is number of unicode chars, need to mult*2 to account for UTF16 encoding
                _stream.Read(content, 0, content.Length);
                string codecDescription = Encoding.Unicode.GetString(content, 0, content.Length).Trim('\0'); //trim null-termination from end of string
                codecProperties.Add("Description", codecDescription);

                _stream.Read(data, 0, data.Length);
                length = BitConverter.ToUInt16(data, 0);
                content = new byte[length];
                _stream.Read(content, 0, content.Length);

                string codecInfo = "";
                foreach (byte b in content)
                    codecInfo += string.Format("{0} ", b);

                codecProperties.Add("Info", codecInfo.TrimEnd());

            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with an unknown Guid
    /// </summary>
    public class AsfUnknownObject : AsfObject
    {
        public AsfUnknownObject(Stream stream)
            : base(stream, "Unknown Object")
        {
            //just skip over 
            AsfMojoObject someObject = ReadStruct<AsfMojoObject>(stream);
            long seekPos = _stream.Position - Marshal.SizeOf(typeof(AsfMojoObject)) + (long)someObject.object_size;
            _stream.Seek(seekPos, SeekOrigin.Begin);
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Data_Object
    /// </summary>
    public class AsfDataObject : AsfObject
    {
        public List<AsfPacket> Packets { get; set; }
        public long HeaderSize { get; set; }

        private AsfMojoDataObject _asfMojoDataObject;

        public AsfDataObject(Stream stream, AsfFileConfiguration config)
            : base(stream, "Data Object")
        {
            _asfMojoDataObject = ReadStruct<AsfMojoDataObject>(_stream);

            HeaderSize = (UInt32)_stream.Position;

            if (config != null)
            {
                config.AsfHeaderSize = (UInt32)_stream.Position;
                config.AsfPacketCount = (uint)_asfMojoDataObject.total_data_packets;
            }

            Packets = new List<AsfPacket>();

            byte[] packetData = new byte[config.AsfPacketSize];

            uint packetId = 0;
            int payloadIdOffset = 0;

            while (packetId < _asfMojoDataObject.total_data_packets)
            {
                if (stream.Read(packetData, 0, (int)config.AsfPacketSize) != config.AsfPacketSize)
                    break;

                AsfPacket packet = new AsfPacket(config, packetData, packetId++, payloadIdOffset);
                payloadIdOffset += packet.PayloadCount;
                Packets.Add(packet);
            }

            config.Packets = Packets;
        }

        public override void Serialize(AsfFileConfiguration config, Stream stream)
        {
            WriteStruct<AsfMojoDataObject>(_asfMojoDataObject, stream);
        }

        public override int GetLength()
        {
            return Marshal.SizeOf(typeof(AsfMojoDataObject));
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Timecode_Index_Parameters_Object
    /// </summary>
    public class AsfTimecodeIndexParametersObject : AsfObject
    {
        public ulong IndexEntryCountInterval { get; set; }
        public uint IndexSpecifiersCount { get; set; }
        public List<StreamIndexSpecifier> StreamIndexSpecifiers;


        public AsfTimecodeIndexParametersObject(Stream stream)
            : base(stream, "Index Parameters Placeholder Object")
        {
            AsfMojoTimecodeIndexParametersObject asfTimecodeIndexParametersObject = ReadStruct<AsfMojoTimecodeIndexParametersObject>(_stream);

            IndexEntryCountInterval = asfTimecodeIndexParametersObject.index_entry_count_interval;
            StreamIndexSpecifiers = new List<StreamIndexSpecifier>();

            //enumerate index specifiers
            byte[] data = new byte[2];
            for (int i = 0; i < asfTimecodeIndexParametersObject.index_specifiers_count; i++)
            {
                _stream.Read(data, 0, 2);
                UInt16 streamNumber = BitConverter.ToUInt16(data, 0);

                _stream.Read(data, 0, 2);
                UInt16 indexType = BitConverter.ToUInt16(data, 0);

                StreamIndexSpecifiers.Add(new StreamIndexSpecifier() { StreamNumber = streamNumber, IndexType = indexType });
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Index_Parameters_Object
    /// </summary>
    public class AsfIndexParametersObject : AsfObject
    {
        public ulong IndexEntryTimeInterval { get; set; }
        public uint IndexSpecifiersCount { get; set; }
        public List<StreamIndexSpecifier> StreamIndexSpecifiers;


        public AsfIndexParametersObject(Stream stream)
            : base(stream, "Index Parameters Placeholder Object")
        {
            AsfMojoIndexParametersObject asfIndexParametersObject = ReadStruct<AsfMojoIndexParametersObject>(_stream);

            IndexEntryTimeInterval = asfIndexParametersObject.index_entry_time_interval;
            StreamIndexSpecifiers = new List<StreamIndexSpecifier>();

            //enumerate index specifiers
            byte[] data = new byte[2];
            for (int i = 0; i < asfIndexParametersObject.index_specifiers_count; i++)
            {
                _stream.Read(data, 0, 2);
                UInt16 streamNumber = BitConverter.ToUInt16(data, 0);

                _stream.Read(data, 0, 2);
                UInt16 indexType = BitConverter.ToUInt16(data, 0);

                StreamIndexSpecifiers.Add(new StreamIndexSpecifier() { StreamNumber = streamNumber, IndexType = indexType });
            }


        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Index_Object
    /// </summary>
    public class AsfIndexObject : AsfObject
    {
        public ulong IndexEntryTimeInterval { get; set; }
        public ulong BlockCount { get; set; }
        public uint IndexSpecifiersCount { get; set; }
        public List<StreamIndexSpecifier> StreamIndexSpecifiers;
        public uint IndexSize { get; set; }
        public uint IndexEntryCount { get; set; }
        private List<StreamIndex> _streamIndexList;
        public List<StreamIndex> StreamIndexList
        {
            get
            {
                return _streamIndexList;
            }
        }

        public AsfIndexObject(Stream stream, AsfFileConfiguration config)
            : base(stream, "Index Object")
        {
            AsfMojoIndexObject asfIndexObject = ReadStruct<AsfMojoIndexObject>(_stream);

            IndexEntryTimeInterval = asfIndexObject.index_entry_time_interval;
            StreamIndexSpecifiers = new List<StreamIndexSpecifier>();

            IndexSize = (UInt32)asfIndexObject.object_size;


            //enumerate index specifiers
            byte[] data = new byte[8];
            for (int i = 0; i < asfIndexObject.index_specifiers_count; i++)
            {
                _stream.Read(data, 0, 2);
                UInt16 streamNumber = BitConverter.ToUInt16(data, 0);

                _stream.Read(data, 0, 2);
                UInt16 indexType = BitConverter.ToUInt16(data, 0);

                StreamIndexSpecifiers.Add(new StreamIndexSpecifier() { StreamNumber = streamNumber, IndexType = indexType });
            }
            IndexSpecifiersCount = (uint)StreamIndexSpecifiers.Count;
            BlockCount = asfIndexObject.index_blocks_count;

            //enumerate block offsets
            Dictionary<UInt16, List<UInt64>> streamIndexOffsets = new Dictionary<UInt16, List<UInt64>>();
            for (int i = 0; i < asfIndexObject.index_blocks_count; i++)
            {
                Dictionary<UInt16, UInt64> blockOffsets = new Dictionary<UInt16, UInt64>();

                _stream.Read(data, 0, 4);
                UInt32 indexEntryCount = BitConverter.ToUInt32(data, 0);
                foreach (StreamIndexSpecifier streamIndexSpecifier in StreamIndexSpecifiers)
                {
                    _stream.Read(data, 0, 8);

                    UInt64 blockOffset = BitConverter.ToUInt64(data, 0);
                    blockOffsets[streamIndexSpecifier.StreamNumber] = blockOffset;
                    streamIndexOffsets[streamIndexSpecifier.StreamNumber] = new List<UInt64>();
                }

                for (int j = 0; j < indexEntryCount; j++)
                {

                    foreach (StreamIndexSpecifier streamIndexSpecifier in StreamIndexSpecifiers)
                    {
                        _stream.Read(data, 0, 4);
                        UInt32 offset = BitConverter.ToUInt32(data, 0);
                        if (offset == UInt32.MaxValue)
                            offset = 0;
                        streamIndexOffsets[streamIndexSpecifier.StreamNumber].Add(offset + blockOffsets[streamIndexSpecifier.StreamNumber] );
                    }
                }

            }

            //generate seek points
            _streamIndexList = new List<StreamIndex>();
            //enumerate seek points
            foreach (UInt16 streamId in streamIndexOffsets.Keys)
            {
                StreamIndex streamSeekPoints = new StreamIndex();
                List<AsfIndexEntry> seekPoints = new List<AsfIndexEntry>();
                int seekPointId = 0;
                for (int i = 0; i < streamIndexOffsets[streamId].Count; i++)
                {
                    UInt64 currentOffset = streamIndexOffsets[streamId][i];

                    int packetIdx = (int)(currentOffset / config.AsfPacketSize);
                    AsfPacket packet = config.Packets[packetIdx];
                    AsfIndexEntry asfIndexEntry = new AsfIndexEntry() { SeekPointID = seekPointId++, PacketNumber = packet.PacketID, PacketCount = 1 };
                    asfIndexEntry.Packets.Add(packet);
                    seekPoints.Add(asfIndexEntry);
                }
                _streamIndexList.Add(new StreamIndex() { StreamNumber = streamId, SeekPoints = seekPoints });
            }
       }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Simple_Index_Object
    /// </summary>
    public class AsfSimpleIndexObject : AsfObject
    {
        public Guid FileId { get; set; }
        public ulong IndexEntryTimeInterval { get; set; }
        public uint IndexEntryCount { get; set; }
        public uint MaxPacketCount { get; set; }
        public uint IndexSize { get; set; }

        private List<AsfIndexEntry> _seekPoints;
        public List<AsfIndexEntry> SeekPoints 
        {
            get
            {
                return _seekPoints;
            }
        }

        public AsfSimpleIndexObject(Stream stream, AsfFileConfiguration config)
            : base(stream, "Simple Index Object")
        {
            AsfMojoSimpleIndexObject asfSimpleIndexObject = ReadStruct<AsfMojoSimpleIndexObject>(_stream);

            config.AsfIndexSize = (UInt32)asfSimpleIndexObject.object_size;


            IndexSize = (UInt32)asfSimpleIndexObject.object_size;
            FileId = asfSimpleIndexObject.file_id.ToGuid();
            IndexEntryTimeInterval = asfSimpleIndexObject.index_entry_time_interval;
            IndexEntryCount = asfSimpleIndexObject.index_entries_count;
            MaxPacketCount = asfSimpleIndexObject.max_packet_count;

            _seekPoints = new List<AsfIndexEntry>();

            byte[] data = new byte[4];
            uint index_interval = (uint)asfSimpleIndexObject.index_entry_time_interval / 10000;
            //enumerate seek points
            for (int i = 0; i < asfSimpleIndexObject.index_entries_count; i++)
            {
                _stream.Read(data, 0, 4);
                UInt32 packetNumber = BitConverter.ToUInt32(data, 0);
                _stream.Read(data, 0, 2);
                UInt16 packetCount = BitConverter.ToUInt16(data, 0);

                AsfIndexEntry asfIndexEntry = new AsfIndexEntry() { PacketNumber = packetNumber, PacketCount = packetCount, SeekPointID = i };
                asfIndexEntry.Packets.AddRange(config.Packets.GetRange((int)packetNumber, packetCount));
                asfIndexEntry.Time = (uint)(i * index_interval);
                _seekPoints.Add(asfIndexEntry);
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Bitrate_Mutual_Exclusion_Object
    /// </summary>
    public class AsfBitrateMutualExclusionObject : AsfObject
    {
        public Guid ExclusionType { get; set; }
        public List<UInt16> StreamNumbers { get; set; }

        public AsfBitrateMutualExclusionObject(Stream stream)
            : base(stream, "Bitrate Mutual Exclusion Object")
        {
            AsfMojoBitrateMutualExclusionObject asfBitrateMutualExclusionObject = ReadStruct<AsfMojoBitrateMutualExclusionObject>(_stream);

            ExclusionType = asfBitrateMutualExclusionObject.exclusion_type.ToGuid();

            byte[] data = new byte[2];
            StreamNumbers = new List<UInt16>();

            for (int i = 0; i < asfBitrateMutualExclusionObject.stream_numbers_count; i++)
            {
                _stream.Read(data, 0, data.Length);
                UInt16 streamNumber = BitConverter.ToUInt16(data, 0);
                StreamNumbers.Add(streamNumber);
            }
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Content_Description_Object
    /// </summary>
    public class AsfContentDescriptionObject : AsfObject
    {
        public Dictionary<string,string> ContentProperties;
        protected AsfMojoContentDescriptionObject _asfContentDescriptionObject;

        public AsfContentDescriptionObject(Stream stream)
            : base(stream, "Content Description Object")
        {
            _asfContentDescriptionObject = ReadStruct<AsfMojoContentDescriptionObject>(_stream);
            byte[] data;
            string propertyValue;

            ContentProperties = new Dictionary<string, string>();

            data = new byte[_asfContentDescriptionObject.title_length];
            _stream.Read(data, 0, data.Length);
            propertyValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0'); //trim null-termination from end of string
            ContentProperties.Add("Title", propertyValue);

            data = new byte[_asfContentDescriptionObject.author_length];
            _stream.Read(data, 0, data.Length);
            propertyValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0'); //trim null-termination from end of string
            ContentProperties.Add("Author", propertyValue);

            data = new byte[_asfContentDescriptionObject.copyright_length];
            _stream.Read(data, 0, data.Length);
            propertyValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0'); //trim null-termination from end of string
            ContentProperties.Add("Copyright", propertyValue);

            data = new byte[_asfContentDescriptionObject.description_length];
            _stream.Read(data, 0, data.Length);
            propertyValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0'); //trim null-termination from end of string
            ContentProperties.Add("Description", propertyValue);

            data = new byte[_asfContentDescriptionObject.rating_length];
            _stream.Read(data, 0, data.Length);
            propertyValue = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0'); //trim null-termination from end of string
            ContentProperties.Add("Rating", propertyValue);
        }

        public override void Serialize(AsfFileConfiguration config, Stream stream)
        {
            string title = ContentProperties["Title"];
            _asfContentDescriptionObject.title_length = (ushort)(2 * title.Length + 2); // 2 bytes for each UTF16 character + null termination

            string author = ContentProperties["Author"];
            _asfContentDescriptionObject.author_length = (ushort)(2 * author.Length + 2); // 2 bytes for each UTF16 character + null termination

            string copyright = ContentProperties["Copyright"];
            _asfContentDescriptionObject.copyright_length = (ushort)(2 * copyright.Length + 2); // 2 bytes for each UTF16 character + null termination

            string description = ContentProperties["Description"];
            _asfContentDescriptionObject.description_length = (ushort)(2 * description.Length + 2); // 2 bytes for each UTF16 character + null termination

            string rating = ContentProperties["Rating"];
            _asfContentDescriptionObject.rating_length = (ushort)(2 * rating.Length + 2); // 2 bytes for each UTF16 character + null termination


            _asfContentDescriptionObject.object_size = (ulong)GetLength();
            WriteStruct<AsfMojoContentDescriptionObject>(_asfContentDescriptionObject, stream);

            string[] values = { title, author, copyright, description, rating };
            byte[] data;
            byte[] nullTerminator = { 0, 0 };
            foreach (string value in values)
            {
                data = Encoding.Unicode.GetBytes(value);
                stream.Write(data, 0, data.Length);
                stream.Write(nullTerminator, 0, nullTerminator.Length);
            }
        }

        public override int GetLength()
        {
            int length = Marshal.SizeOf(typeof(AsfMojoContentDescriptionObject));

            foreach (string item in ContentProperties.Values)
                length += 2*item.Length + 2;
            
            return length;
        }
    }

    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Script_Command_Object
    /// </summary>
    public class AsfScriptCommandObject : AsfObject
    {
        public List<ScriptCommand> ScriptCommands;

        public AsfScriptCommandObject(Stream stream)
            : base(stream, "Script Command Object")
        {
            AsfMojoScriptCommandObject asfScriptCommandObject = ReadStruct<AsfMojoScriptCommandObject>(_stream);
            ScriptCommands = new List<ScriptCommand>();

            byte[] data = new byte[4];
            List<string> commandTypeNames = new List<string>();
            for (int i = 0; i < asfScriptCommandObject.command_types_count; i++)
            {
                _stream.Read(data, 0, 2);
                UInt16 commandTypeNameLength = BitConverter.ToUInt16(data, 0);
                byte[] name = new byte[2 * commandTypeNameLength]; //Unicode 2 bytes per character
                _stream.Read(name, 0, name.Length);
                string commandTypeName = Encoding.Unicode.GetString(name, 0, name.Length).Trim('\0'); 
                commandTypeNames.Add(commandTypeName);
            }

            for (int i = 0; i < asfScriptCommandObject.commands_count; i++)
            {
                _stream.Read(data, 0, 4);
                UInt32 presentationTime = BitConverter.ToUInt32(data, 0);

                _stream.Read(data, 0, 2);
                UInt16 typeIndex = BitConverter.ToUInt16(data, 0);

                _stream.Read(data, 0, 2);
                UInt16 commandNameLength = BitConverter.ToUInt16(data, 0);

                byte[] name = new byte[2 * commandNameLength]; //Unicode 2 bytes per character
                _stream.Read(name, 0, name.Length);
                string commandName = Encoding.Unicode.GetString(name, 0, name.Length).Trim('\0'); 

                ScriptCommands.Add(new ScriptCommand() { Name = commandName, Type = commandTypeNames[typeIndex], PresentationTime = presentationTime });
            }
        }

    }
    /// <summary>
    /// Wrapper for ASF object with Guid AsfGuid.ASF_Stream_Prioritization_Object
    /// </summary>
    public class AsfStreamPrioritizationObject : AsfObject
    {
        public List<StreamPrioritizationInfo> PrioritizationInfo;

        public AsfStreamPrioritizationObject(Stream stream)
            : base(stream, "Stream Prioritization Object")
        {
            AsfMojoStreamPrioritizationObject asfStreamPrioritizationObject = ReadStruct<AsfMojoStreamPrioritizationObject>(_stream);
            PrioritizationInfo = new List<StreamPrioritizationInfo>();

            byte[] data = new byte[2];
            for (int i = 0; i < asfStreamPrioritizationObject.priority_records_count; i++)
            {
                _stream.Read(data, 0, data.Length);
                UInt16 streamNumber = BitConverter.ToUInt16(data, 0);
                _stream.Read(data, 0, data.Length);
                UInt16 flags = BitConverter.ToUInt16(data, 0);

                PrioritizationInfo.Add(new StreamPrioritizationInfo() { StreamNumber = streamNumber,  Flags = flags, IsMandatory =  (flags & 1) > 0 });
            }
        }
    }


    #region classes returned by AsfMojo Wrapper objects
    public class AsfProperty
    {
        public ushort StreamNumber { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class ScriptCommand
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public UInt32 PresentationTime { get; set; }
    }

    public class StreamPrioritizationInfo
    {
        public ushort StreamNumber {get;set;}
        public ushort Flags {get;set;}
        public bool IsMandatory {get;set;}
    }

    public class StreamIndexSpecifier
    {
        public ushort StreamNumber { get; set; }
        public ushort IndexType { get; set; }
    }

    public class StreamIndex
    {
        public ushort StreamNumber { get; set; }
        public List<AsfIndexEntry> SeekPoints { get; set; }
    }
    public class StreamIndexBlock
    {
        public UInt64 Offset { get; set; }
        public UInt32 IndexEntry { get; set; }
    }


    public class AsfIndexEntry
    {
        public AsfIndexEntry()
        {
            Packets = new List<AsfPacket>();
        }
        public int SeekPointID { get; set; }
        public UInt32 PacketNumber { get; set; }
        public UInt16 PacketCount { get; set; }
        public UInt32 Time { get; set; }
        public List<AsfPacket> Packets { get; set; }
    }
#endregion

}



