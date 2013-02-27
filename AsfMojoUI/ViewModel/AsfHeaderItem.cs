using System;
using System.Collections.Generic;
using System.IO;

using AsfMojo.Configuration;
using AsfMojo.Media;
using AsfMojo.Parsing;
using System.Collections.ObjectModel;

namespace AsfMojoUI.ViewModel
{

    /// <summary>
    /// Viewmodel wrapper for an ASF Mojo wrapper object with public properties used in the view
    /// </summary>
    public class AsfHeaderItem
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }

        public static AsfFileConfiguration Configuration = new AsfFileConfiguration();

        public Dictionary<string, object> Properties { get; set; }
        public Dictionary<string, Dictionary<string, object>> Subsections { get; set; }
        private List<AsfHeaderItem> _childNodes;
        protected Stream _stream;


        public AsfHeaderItem(string name, Guid type, Stream stream)
        {
            Name = name;
            _stream = stream;

            Properties = new Dictionary<string, object>();
            Subsections = new Dictionary<string, Dictionary<string, object>>();

            AsfObject someObject = new AsfObject(stream, name);

            Properties.Add("File Position", someObject.Position);
            Properties.Add("Object ID", someObject.Guid.ToString().ToUpper());
            Properties.Add("Object Size", someObject.Size);

            _childNodes = new List<AsfHeaderItem>();
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

        public void Add(AsfHeaderItem item)
        {
            _childNodes.Add(item);
        }

        public IList<AsfHeaderItem> Items
        {
            get
            {
                return _childNodes;
            }
        }
    }

    public class AsfFileHeaderItem : AsfHeaderItem
    {
        public AsfFileHeaderItem(Stream stream)
            : base("Header Object", AsfGuid.ASF_Header_Object, stream)
        {

            AsfFileHeader asfFileHeader = new AsfFileHeader(stream, Configuration);
            long streamPosition = asfFileHeader.Position;

            IsExpanded = true;
        }
    }

    public class AsfStreamBitratePropertiesItem : AsfHeaderItem
    {
        public AsfStreamBitratePropertiesItem(Stream stream)
            : base("Stream Bitrate Properties", AsfGuid.ASF_Stream_Bitrate_Properties_Object, stream)
        {
            AsfStreamBitrateProperties asfStreamBitrateProperties = new AsfStreamBitrateProperties(stream);

            foreach (var bitrateEntry in asfStreamBitrateProperties.Bitrate)
            {
                int streamNumber = bitrateEntry.Key;
                uint bitrate = bitrateEntry.Value;

                Properties.Add("Stream " + streamNumber, bitrate);
            }
        }
    }

    public class AsfFilePropertiesItem : AsfHeaderItem
    {
        public AsfFilePropertiesItem(Stream stream)
            : base("File Properties Object", AsfGuid.ASF_File_Properties_Object, stream)
        {
            AsfFileProperties asfFileProperties = new AsfFileProperties(stream, Configuration);

            IsSelected = true; 

            Properties.Add("MMS ID", asfFileProperties.FileId.ToString().ToUpper());
            Properties.Add("Total Size", asfFileProperties.FileSize);
            Properties.Add("Creation Time", asfFileProperties.CreationTime);
            Properties.Add("Packets", asfFileProperties.PacketCount);
            Properties.Add("Duration", TimeSpan.FromTicks((long)asfFileProperties.PlayDuration).ToString("mm':'ss\\.fff"));
            Properties.Add("Send Duration", TimeSpan.FromTicks((long)asfFileProperties.SendDuration).ToString("mm':'ss\\.fff"));
            Properties.Add("Preroll", TimeSpan.FromMilliseconds(asfFileProperties.Preroll).ToString("mm':'ss\\.fff"));
            Properties.Add("Flags", string.Format("{0:x}", asfFileProperties.Flags));
            Properties.Add("Broadcast", asfFileProperties.IsBroadcast);
            Properties.Add("Seekable", asfFileProperties.IsSeekable);
            Properties.Add("Max Packet Size", asfFileProperties.MaxPacketSize);
            Properties.Add("Min Packet Size", asfFileProperties.MinPacketSize);
            Properties.Add("Maximum Bitrate", asfFileProperties.MaxBitRate);
        }
    }

    public class AsfHeaderExtensionItem : AsfHeaderItem
    {
        public AsfHeaderExtensionItem(Stream stream)
            : base("Header Extension Object", AsfGuid.ASF_Header_Extension_Object, stream)
        {
            AsfHeaderExtension asfHeaderExtension = new AsfHeaderExtension(stream);

            Properties.Add("Clock Type", asfHeaderExtension.R1Guid == AsfGuid.ASF_Reserved_1 ? "Reserved 1" : asfHeaderExtension.R1Guid.ToString());
            Properties.Add("Clock Size", asfHeaderExtension.ClockSize);
            Properties.Add("Extended Header Size", asfHeaderExtension.ExtendedHeaderSize);
        }
    }

    public class AsfLanguageListObjectItem : AsfHeaderItem
    {
        public AsfLanguageListObjectItem(Stream stream)
            : base("Language List Object", AsfGuid.ASF_Language_List_Object, stream)
        {
            AsfLanguageListObject asfLanguageListObject = new AsfLanguageListObject(stream);

            int index = 0;
            foreach (string language in asfLanguageListObject.Languages)
                Properties.Add(string.Format("Language [{0}]", index++), language);
        }
    }

    public class AsfExtendedStreamPropertiesItem : AsfHeaderItem
    {
        public AsfExtendedStreamPropertiesItem(Stream stream)
            : base("Extended Stream Properties Object", AsfGuid.ASF_Extended_Stream_Properties_Object, stream)
        {
            AsfExtendedStreamProperties asfExtendedStreamProperties = new AsfExtendedStreamProperties(stream);


            Properties.Add("Stream Number", asfExtendedStreamProperties.StreamNumber);

            Name = string.Format("Extended Stream Properties Object [{0}]", asfExtendedStreamProperties.StreamNumber);

            Properties.Add("Start Time", TimeSpan.FromMilliseconds(asfExtendedStreamProperties.StartTime).ToString("mm':'ss\\.fff"));
            Properties.Add("End Time", TimeSpan.FromMilliseconds(asfExtendedStreamProperties.EndTime).ToString("mm':'ss\\.fff"));
            Properties.Add("Avg. Time / Frame", TimeSpan.FromTicks((long)asfExtendedStreamProperties.AvgTimePerFrame).ToString("mm':'ss\\.fff"));
            if (asfExtendedStreamProperties.AvgTimePerFrame > 0)
            {
                double frameRate = 1.0 / TimeSpan.FromTicks((long)asfExtendedStreamProperties.AvgTimePerFrame).TotalSeconds;
                string displayFrameRate = String.Format("{0:0.##}", frameRate);
                if (displayFrameRate == "29.97")
                    displayFrameRate += "(NTSC)";
                else if (displayFrameRate == "25.00")
                    displayFrameRate += "(PAL)";
                Properties.Add("Avg. Frames / Sec", displayFrameRate);
            }
            Properties.Add("Max. Object Size", asfExtendedStreamProperties.MaxObjectSize);
            Properties.Add("Avg. Data Bit Rate", asfExtendedStreamProperties.DataBitrate);
            Properties.Add("Max. Data Bit Rate", asfExtendedStreamProperties.MaxDataBitrate);
            Properties.Add("Avg. Buffer Size", TimeSpan.FromMilliseconds(asfExtendedStreamProperties.BufferSize).ToString("mm':'ss\\.fff"));
            Properties.Add("Max. Buffer Size", TimeSpan.FromMilliseconds(asfExtendedStreamProperties.AlternateBufferSize).ToString("mm':'ss\\.fff"));

            Properties.Add("Flags", string.Format("{0:x}", asfExtendedStreamProperties.Flags));
            Properties.Add("Reliable", asfExtendedStreamProperties.IsReliable);
            Properties.Add("Seekable", asfExtendedStreamProperties.IsSeekable);
            Properties.Add("No Cleanpoints", asfExtendedStreamProperties.HasNoCleanpoints);
            Properties.Add("Resend Live Clean Points", asfExtendedStreamProperties.DoResendLiveCleanpoints);
            Properties.Add("Language Index", asfExtendedStreamProperties.LanguageIndex);
        }
    }

    public class AsfCompatibilityObjectItem : AsfHeaderItem
    {
        public AsfCompatibilityObjectItem(Stream stream)
            : base("Compatibility Object", AsfGuid.ASF_Compatibility_Object, stream)
        {
            AsfCompatibilityObject asfCompatibilityObject = new AsfCompatibilityObject(stream);
        }
    }

    public class AsfStreamPropertiesObjectItem : AsfHeaderItem
    {
        public AsfStreamPropertiesObjectItem(Stream stream)
            : base("Stream Properties Object", AsfGuid.ASF_Stream_Properties_Object, stream)
        {
            AsfStreamPropertiesObject asfStreamPropertiesObject = new AsfStreamPropertiesObject(stream);

            Name = string.Format("Stream Properties Object [{0}]", asfStreamPropertiesObject.StreamNumber);

            Properties.Add("Stream Number", asfStreamPropertiesObject.StreamNumber);
            Properties.Add("Encrypted", asfStreamPropertiesObject.IsEncrypted);


            Dictionary<string, object> streamSectionProperties = new Dictionary<string, object>();
            Subsections.Add("Stream Type Specific", streamSectionProperties);


            if (asfStreamPropertiesObject.StreamType == AsfGuid.ASF_Audio_Media)
            {
                AsfMojoAudioStreamProperties asfAudioStreamProperties = (AsfMojoAudioStreamProperties)asfStreamPropertiesObject.StreamProperties["AudioStreamProperties"];
                streamSectionProperties.Add("Stream Type", "Audio Media");

                Configuration.AsfAudioStreamId = (uint)asfStreamPropertiesObject.StreamNumber;
                Configuration.AudioChannels = asfAudioStreamProperties.number_channels;
                Configuration.AudioSampleRate = asfAudioStreamProperties.samples_per_second;
                Configuration.AudioBitsPerSample = asfAudioStreamProperties.bits_per_sample;

                streamSectionProperties.Add("Format Tag", asfAudioStreamProperties.format_tag);
                streamSectionProperties.Add("Channels", asfAudioStreamProperties.number_channels);
                streamSectionProperties.Add("Samples / Second", asfAudioStreamProperties.samples_per_second);
                streamSectionProperties.Add("Average Bytes / Second", asfAudioStreamProperties.average_bytes_per_second);
                streamSectionProperties.Add("Average Bitrate (bits/sec)", 8 * asfAudioStreamProperties.average_bytes_per_second); //same info..
                streamSectionProperties.Add("Block Align", asfAudioStreamProperties.block_alignment);
                streamSectionProperties.Add("Bits / Sample", asfAudioStreamProperties.bits_per_sample);
                streamSectionProperties.Add("Extra Data Size", asfAudioStreamProperties.codec_specific_data_size);
            }
            else if (asfStreamPropertiesObject.StreamType == AsfGuid.ASF_Video_Media)
            {
                AsfMojoVideoStreamProperties asfVideoStreamProperties = (AsfMojoVideoStreamProperties)asfStreamPropertiesObject.StreamProperties["VideoStreamProperties"];

                streamSectionProperties.Add("Stream Type", "Video Media");

                streamSectionProperties.Add("Width", asfVideoStreamProperties.encoded_image_width);
                streamSectionProperties.Add("Height", asfVideoStreamProperties.encoded_image_height);
                streamSectionProperties.Add("Flags", asfVideoStreamProperties.reserved_flags);

                AsfMojoVideoStreamFormatData asfVideoStreamFormatData = (AsfMojoVideoStreamFormatData)asfStreamPropertiesObject.StreamProperties["VideoStreamFormatData"];

                Dictionary<string, object> streamSectionVideoBitmapProperties = new Dictionary<string, object>();
                Subsections.Add("Bitmap Info Header", streamSectionVideoBitmapProperties);

                streamSectionVideoBitmapProperties.Add("biSize", asfVideoStreamFormatData.format_data_size);

                streamSectionVideoBitmapProperties.Add("Width", asfVideoStreamFormatData.image_width);
                streamSectionVideoBitmapProperties.Add("Height", asfVideoStreamFormatData.image_height);

                Configuration.ImageWidth = (int)asfVideoStreamFormatData.image_width;
                Configuration.ImageHeight = (int)asfVideoStreamFormatData.image_height;

                streamSectionVideoBitmapProperties.Add("Planes", asfVideoStreamFormatData.reserved);
                streamSectionVideoBitmapProperties.Add("Bits", asfVideoStreamFormatData.bits_per_pixel_count);
                streamSectionVideoBitmapProperties.Add("Compression", System.Text.ASCIIEncoding.ASCII.GetString(asfVideoStreamFormatData.compression_id));
                streamSectionVideoBitmapProperties.Add("Image Size", asfVideoStreamFormatData.image_size);
                streamSectionVideoBitmapProperties.Add("X Pixels / Meter", asfVideoStreamFormatData.horizontal_pixels_per_meter);
                streamSectionVideoBitmapProperties.Add("Y Pixels / Meter", asfVideoStreamFormatData.vertical_pixels_per_meter);
                streamSectionVideoBitmapProperties.Add("Colors Used", asfVideoStreamFormatData.used_colors_count);
                streamSectionVideoBitmapProperties.Add("Colors Important", asfVideoStreamFormatData.important_colors_count);
            }
        }
    }

    public class AsfMetadataObjectItem : AsfHeaderItem
    {
        public AsfMetadataObjectItem(Stream stream)
            : base("Metadata Object", AsfGuid.ASF_Metadata_Object, stream)
        {
            AsfMetadataObject asfMetadataObject = new AsfMetadataObject(stream);

            Dictionary<string, object> attributeProperties = new Dictionary<string, object>();
            Subsections.Add("Attributes", attributeProperties);

            foreach (var descriptionRecord in asfMetadataObject.DescriptionRecords)
            {
                string name = descriptionRecord.Name + string.Format(" - Stream {0}", descriptionRecord.StreamNumber);
                attributeProperties.Add(name, descriptionRecord.Value);
            }
        }
    }

    public class AsfIndexParametersPlaceholderItem : AsfHeaderItem
    {
        public AsfIndexParametersPlaceholderItem(Stream stream)
            : base("Index Parameters Placeholder Object", AsfGuid.ASF_Index_Parameters_Placeholder_Object, stream)
        {
            AsfIndexParametersPlaceholder asfIndexParametersPlaceholder = new AsfIndexParametersPlaceholder(stream);
        }
    }

    public class AsfPaddingObjectItem : AsfHeaderItem
    {
        public AsfPaddingObjectItem(Stream stream)
            : base("Padding Object", AsfGuid.ASF_Padding_Object, stream)
        {
            AsfPaddingObject asfPaddingObject = new AsfPaddingObject(stream);
        }
    }

    public class AsfExtendedContentDescriptionItem : AsfHeaderItem
    {
        public AsfExtendedContentDescriptionItem(Stream stream)
            : base("Extended Content Description Object", AsfGuid.ASF_Extended_Content_Description_Object, stream)
        {
            AsfExtendedContentDescription asfExtendedContentDescription = new AsfExtendedContentDescription(stream);

            foreach (var item in asfExtendedContentDescription.ContentDescriptions)
                Properties.Add(item.Key, item.Value);

        }
    }

    public class AsfCodecListObjectItem : AsfHeaderItem
    {
        public AsfCodecListObjectItem(Stream stream)
            : base("Codec List Object", AsfGuid.ASF_Codec_List_Object, stream)
        {
            AsfCodecListObject asfCodecListObject = new AsfCodecListObject(stream);

            Properties.Add("Codec ID", asfCodecListObject.CodecId.ToString());

            int index = 0;
            foreach (var property in asfCodecListObject.CodecProperties)
            {
                Dictionary<string, object> codecProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Codec Info [{0}]", ++index), codecProperties);

                foreach (var item in property)
                    codecProperties.Add(item.Key, item.Value);
            }
        }
    }

    public class AsfDataObjectItem : AsfHeaderItem
    {
        private List<AsfPacket> _packets;

        public AsfDataObjectItem(Stream stream)
            : base("Data Object", AsfGuid.ASF_Simple_Index_Object, stream)
        {
            AsfDataObject asfDataObject = new AsfDataObject(stream, Configuration);
            _packets = asfDataObject.Packets;
        }

        public List<AsfPacket> Packets
        {
            get { return _packets; }
        }
    }

    public class AsfSimpleIndexObjectItem : AsfHeaderItem
    {
        private List<StreamIndex> _streamIndexList;
        public List<StreamIndex> StreamIndexList
        {
            get { return _streamIndexList; }
        }

        public AsfSimpleIndexObjectItem(Stream stream)
            : base("Simple Index Object", AsfGuid.ASF_Simple_Index_Object, stream)
        {
            AsfSimpleIndexObject asfSimpleIndexObject = new AsfSimpleIndexObject(stream, Configuration);

            Properties.Add("File ID", asfSimpleIndexObject.FileId.ToString());
            Properties.Add("Interval", TimeSpan.FromMilliseconds(asfSimpleIndexObject.IndexEntryTimeInterval / 10000).ToString("mm':'ss\\.fff"));
            Properties.Add("Entry count", asfSimpleIndexObject.IndexEntryCount);
            Properties.Add("Max. Packets in Entry", asfSimpleIndexObject.MaxPacketCount);

            _streamIndexList = new List<StreamIndex>();
            _streamIndexList.Add(new StreamIndex() { StreamNumber = (ushort)Configuration.AsfVideoStreamId, SeekPoints = asfSimpleIndexObject.SeekPoints });
        }
    }

    public class AsfBitrateMutualExclusionObjectItem : AsfHeaderItem
    {
        public AsfBitrateMutualExclusionObjectItem(Stream stream)
            : base("Bitrate Mutual Exclusion Object", AsfGuid.ASF_Bitrate_Mutual_Exclusion_Object, stream)
        {
            AsfBitrateMutualExclusionObject asfBitrateMutualExclusionObject = new AsfBitrateMutualExclusionObject(stream);

            if (asfBitrateMutualExclusionObject.ExclusionType == AsfGuid.ASF_Mutex_Language)
            {
                Properties.Add("Exclusion Type", "Same Content, Different Language");
            }
            else if (asfBitrateMutualExclusionObject.ExclusionType == AsfGuid.ASF_Mutex_Bitrate)
            {
                Properties.Add("Exclusion Type", "Same Content, Different Bitrate");
            }
            else
                Properties.Add("Exclusion Type", "Unknown");

            Properties.Add("Stream Numbers", string.Join(",", asfBitrateMutualExclusionObject.StreamNumbers));
        }
    }

    public class EditableContentProperty
    {
        public EditableContentProperty(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class AsfContentDescriptionObjectItem : AsfHeaderItem
    {
        protected AsfContentDescriptionObject ContentDescription;
        public ObservableCollection<EditableContentProperty> EditProperties { get; set; }

        public AsfContentDescriptionObjectItem(Stream stream)
            : base("Content Description Object", AsfGuid.ASF_Content_Description_Object, stream)
        {
            ContentDescription = new AsfContentDescriptionObject(stream);
            EditProperties = new ObservableCollection<EditableContentProperty>();

            List<string> editableItems = new List<string> { "Author", "Description", "Copyright", "Title", "Rating" };

            foreach (var item in ContentDescription.ContentProperties)
            {
                if (!editableItems.Contains(item.Key))
                    Properties.Add(item.Key, item.Value);
                else
                {
                    EditProperties.Add(new EditableContentProperty(item.Key, item.Value));
                }
            }

        }
    }

    public class AsfScriptCommandObjectItem : AsfHeaderItem
    {
        public AsfScriptCommandObjectItem(Stream stream)
            : base("Script Command Object", AsfGuid.ASF_Script_Command_Object, stream)
        {

            AsfScriptCommandObject asfScriptCommandObject = new AsfScriptCommandObject(stream);
            int commandIndex = 1;
            foreach (ScriptCommand scriptCommand in asfScriptCommandObject.ScriptCommands)
            {
                var commandProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Script Command [{0}]", commandIndex++), commandProperties);
                commandProperties.Add("Type", scriptCommand.Type);
                commandProperties.Add("Presentation Time", TimeSpan.FromMilliseconds(scriptCommand.PresentationTime).ToString("mm':'ss\\.fff"));
                commandProperties.Add("Name", scriptCommand.Name);
            }

        }
    }


    public class AsfStreamPrioritizationObjectItem : AsfHeaderItem
    {
        public AsfStreamPrioritizationObjectItem(Stream stream)
            : base("Stream Prioritization Object", AsfGuid.ASF_Stream_Prioritization_Object, stream)
        {
            AsfStreamPrioritizationObject asfStreamPrioritizationObject = new AsfStreamPrioritizationObject(stream);

            foreach (var info in asfStreamPrioritizationObject.PrioritizationInfo)
            {
                Dictionary<string, object> streamPrioProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Stream Prioritization [{0}]", info.StreamNumber), streamPrioProperties);
                streamPrioProperties.Add("Stream Number", info.StreamNumber);
                streamPrioProperties.Add("Flags", string.Format("{0:x}", info.Flags));
                streamPrioProperties.Add("Mandatory", info.IsMandatory);
            }
        }
    }

    public class AsfTimecodeIndexParametersObjectItem : AsfHeaderItem
    {
        public AsfTimecodeIndexParametersObjectItem(Stream stream)
            : base("Timecode Index Parameters Object", AsfGuid.ASF_Timecode_Index_Parameters_Object, stream)
        {
            AsfTimecodeIndexParametersObject asfTimecodeIndexParametersObject = new AsfTimecodeIndexParametersObject(stream);

            foreach (var info in asfTimecodeIndexParametersObject.StreamIndexSpecifiers)
            {
                Dictionary<string, object> streamTimecodeIndexProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Index Parameters [{0}]", info.StreamNumber), streamTimecodeIndexProperties);
                streamTimecodeIndexProperties.Add("Stream Number", info.StreamNumber);

                string indexType = info.IndexType.ToString();

                if (info.IndexType == 2)
                    indexType = "Nearest Past Media Object";
                else if (info.IndexType == 3)
                    indexType = "Nearest Past Cleanpoint";

                streamTimecodeIndexProperties.Add("Index Type", indexType);
            }
        }
    }

    public class AsfIndexParametersObjectItem : AsfHeaderItem
    {
        public AsfIndexParametersObjectItem(Stream stream)
            : base("Index Parameters Object", AsfGuid.ASF_Index_Parameters_Object, stream)
        {
            AsfIndexParametersObject asfIndexParametersObject = new AsfIndexParametersObject(stream);

            foreach (var info in asfIndexParametersObject.StreamIndexSpecifiers)
            {
                Dictionary<string, object> streamIndexProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Index Parameters [{0}]", info.StreamNumber), streamIndexProperties);
                streamIndexProperties.Add("Stream Number", info.StreamNumber);

                string indexType = info.IndexType.ToString();

                if (info.IndexType == 1)
                    indexType = "Nearest Past Data Packet";
                if (info.IndexType == 2)
                    indexType = "Nearest Past Media Object";
                else if (info.IndexType == 3)
                    indexType = "Nearest Past Cleanpoint";

                streamIndexProperties.Add("Index Type", indexType);
            }
        }
    }

    public class AsfIndexObjectItem : AsfHeaderItem
    {
        private List<StreamIndex> _streamIndexList;
        public List<StreamIndex> StreamIndexList
        {
            get { return _streamIndexList; }
        }

        public AsfIndexObjectItem(Stream stream)
            : base("Index Object", AsfGuid.ASF_Index_Object, stream)
        {
            AsfIndexObject asfIndexObject = new AsfIndexObject(stream, Configuration);

            Properties.Add("Interval", TimeSpan.FromMilliseconds(asfIndexObject.IndexEntryTimeInterval / 10000).ToString("mm':'ss\\.fff"));
            Properties.Add("Parameters", asfIndexObject.IndexSpecifiersCount);
            Properties.Add("Blocks", asfIndexObject.BlockCount);

            foreach (var info in asfIndexObject.StreamIndexSpecifiers)
            {
                Dictionary<string, object> streamIndexProperties = new Dictionary<string, object>();
                Subsections.Add(string.Format("Index Parameters [{0}]", info.StreamNumber), streamIndexProperties);
                streamIndexProperties.Add("Stream Number", info.StreamNumber);

                string indexType = info.IndexType.ToString();

                if (info.IndexType == 1)
                    indexType = "Nearest Past Data Packet";
                if (info.IndexType == 2)
                    indexType = "Nearest Past Media Object";
                else if (info.IndexType == 3)
                    indexType = "Nearest Past Cleanpoint";

                streamIndexProperties.Add("Index Type", indexType);
            }
            _streamIndexList = asfIndexObject.StreamIndexList;
        }
    }

}



