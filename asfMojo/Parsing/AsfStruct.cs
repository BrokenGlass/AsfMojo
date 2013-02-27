using System;
using System.Runtime.InteropServices;

namespace AsfMojo.Parsing
{
    #region public struct definitions
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AsfMojoFileHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt32 header_object_count;
        public byte r1;
        public byte r2;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AsfMojoStreamBitrateProperties
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 bitrate_records_count;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AsfMojoBitrateRecord
    {
        public UInt16 flags;
        public UInt32 average_bitrate;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Simple_Index_Object
    public struct AsfMojoSimpleIndexObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] file_id;
        public UInt64 index_entry_time_interval;
        public UInt32 max_packet_count;
        public UInt32 index_entries_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Timecode_Index_Parameters_Object
    public struct AsfMojoTimecodeIndexParametersObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt32 index_entry_count_interval;
        public UInt16 index_specifiers_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Index_Parameters_Object
    public struct AsfMojoIndexParametersObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt32 index_entry_time_interval;
        public UInt16 index_specifiers_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Index_Object
    public struct AsfMojoIndexObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt32 index_entry_time_interval;
        public UInt16 index_specifiers_count;
        public UInt32 index_blocks_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Bitrate_Mutual_Exclusion_Object
    public struct AsfMojoBitrateMutualExclusionObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] exclusion_type;
        public UInt16 stream_numbers_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Content_Description_Object
    public struct AsfMojoContentDescriptionObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 title_length;
        public UInt16 author_length;
        public UInt16 copyright_length;
        public UInt16 description_length;
        public UInt16 rating_length;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Script_Command_Object
    public struct AsfMojoScriptCommandObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] reserved;
        public UInt16 commands_count;
        public UInt16 command_types_count;
    };



    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Prioritization_Object
    public struct AsfMojoStreamPrioritizationObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 priority_records_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_File_Properties_Object
    public struct AsfMojoFileProperties
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] file_id;
        public UInt64 file_size;
        public UInt64 creation_date;
        public UInt64 data_packet_count;
        public UInt64 play_duration;
        public UInt64 send_duration;
        public UInt64 preroll;
        public UInt32 flags;

        public UInt32 min_data_packet_size;
        public UInt32 max_data_packet_size;
        public UInt32 max_bitrate;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Properties_Object
    public struct AsfMojoStreamProperties
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] stream_type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] error_correction_type;
        public UInt64 time_offset;
        public UInt32 data_length;
        public UInt16 flags;
        public UInt32 reserved;

    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Extended_Content_Description_Object
    public struct AsfMojoExtendedContentDescription
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 content_descriptors_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Header_Extension_Object
    public struct AsfMojoHeaderExtension
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] r1;
        public UInt16 r2;
        public UInt32 header_extension_data_size;
    };


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Language_List_Object
    public struct AsfMojoLanguageListObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 language_id_records_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AsfMojoLanguageIDRecord
    {
        public byte language_id_length;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Compatibility_Object
    public struct AsfMojoCompatibilityObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public byte profile;
        public byte mode;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Metadata_Object
    public struct AsfMojoMetadataObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt16 description_records_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Padding_Object
    public struct AsfMojoPaddingObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Codec_List_Object
    public struct AsfMojoCodecListObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] reserved;
        public UInt32 codec_entries_count;
    };



    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Codec_List_Object
    public struct AsfMojoDescriptionRecord
    {
        public UInt16 reserved;
        public UInt16 stream_number;
        public UInt16 name_length;
        public UInt16 data_type;
        public UInt32 data_length;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct AsfMojo_index_specifier
    {
        public UInt16 stream_number;
        public UInt16 index_type;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //asf_object
    public struct AsfMojoObject //base
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
    };


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Index_Parameters_Placeholder_Object
    public struct AsfMojoIndexParametersPlaceholder
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public byte[] reserved;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //asf_extended_stream_properties
    public struct AsfMojoExtendedStreamProperties
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        public UInt64 start_time;
        public UInt64 end_time;
        public UInt32 data_bitrate;
        public UInt32 buffer_size;
        public UInt32 initial_buffer_fullness;
        public UInt32 alternate_data_bitrate;
        public UInt32 alternate_buffer_size;
        public UInt32 alternate_initial_buffer_fullness;
        public UInt32 max_object_size;
        public UInt32 flags;
        public UInt16 stream_number;
        public UInt16 stream_language_id_idx;
        public UInt64 avg_time_per_frame;
        public UInt16 stream_name_count;
        public UInt16 payload_extension_system_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Properties_Object.
    public struct AsfMojoStreamPropertiesObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] stream_type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] error_correction_type;
        public UInt64 time_offset;
        public UInt32 type_specific_data_length;
        public UInt32 error_correction_data_length;
        public UInt16 flags;
        public UInt32 reserved;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Properties_Object / type_specific_data
    public struct AsfMojoAudioStreamProperties
    {
        public UInt16 format_tag;
        public UInt16 number_channels;
        public UInt32 samples_per_second;
        public UInt32 average_bytes_per_second;
        public UInt16 block_alignment;
        public UInt16 bits_per_sample;
        public UInt16 codec_specific_data_size;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Properties_Object / type_specific_data
    public struct AsfMojoVideoStreamProperties
    {
        public UInt32 encoded_image_width;
        public UInt32 encoded_image_height;
        public byte reserved_flags;
        public UInt16 format_data_size;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Stream_Properties_Object / video format data
    public struct AsfMojoVideoStreamFormatData
    {
        public UInt32 format_data_size;
        public UInt32 image_width;
        public UInt32 image_height;
        public UInt16 reserved;
        public UInt16 bits_per_pixel_count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] compression_id;
        public UInt32 image_size;
        public UInt32 horizontal_pixels_per_meter;
        public UInt32 vertical_pixels_per_meter;
        public UInt32 used_colors_count;
        public UInt32 important_colors_count;
    };



    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //AsfExtendedStreamProperties
    public struct AsfMojoPayloadExtension
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] extension_system_id;
        public UInt16 extension_data_size;
        public UInt32 extension_system_info_length;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    //ASF_Data_Object
    public struct AsfMojoDataObject
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] object_id;
        public UInt64 object_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] file_id;
        public UInt64 total_data_packets;
        public UInt16 reserved;
    };
    #endregion

}
