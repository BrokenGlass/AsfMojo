using System;

namespace AsfMojo.Parsing
{
    /// <summary>
    /// List of Guids used in the ASF specification from Doc revision 01.20.05
    /// http://www.microsoft.com/downloads/en/details.aspx?displaylang=en&FamilyID=56de5ee4-51ca-46c6-903b-97390ad14fea
    /// </summary>
    public class AsfGuid
    {
        #region Asf Guid declarations
        public static readonly Guid ASF_Header_Object = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
        public static readonly Guid ASF_Data_Object = new Guid("75B22636-668E-11CF-A6D9-00AA0062CE6C");
        public static readonly Guid ASF_Simple_Index_Object = new Guid("33000890-E5B1-11CF-89F4-00A0C90349CB");
        public static readonly Guid ASF_Index_Object = new Guid("D6E229D3-35DA-11D1-9034-00A0C90349BE");
        public static readonly Guid ASF_Index_Parameters_Placeholder_Object = new Guid("D9AADE20-7C17-4F9C-BC28-8555DD98E2A2");
        public static readonly Guid ASF_Media_Object_Index_Object = new Guid("FEB103F8-12AD-4C64-840F-2A1D2F7AD48C");
        public static readonly Guid ASF_Timecode_Index_Object = new Guid("3CB73FD0-0C4A-4803-953D-EDF7B6228F0C");
        public static readonly Guid ASF_File_Properties_Object = new Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
        public static readonly Guid ASF_Stream_Properties_Object = new Guid("B7DC0791-A9B7-11CF-8EE6-00C00C205365");
        public static readonly Guid ASF_Header_Extension_Object = new Guid("5FBF03B5-A92E-11CF-8EE3-00C00C205365");
        public static readonly Guid ASF_Codec_List_Object = new Guid("86D15240-311D-11D0-A3A4-00A0C90348F6");
        public static readonly Guid ASF_Script_Command_Object = new Guid("1EFB1A30-0B62-11D0-A39B-00A0C90348F6");
        public static readonly Guid ASF_Marker_Object = new Guid("F487CD01-A951-11CF-8EE6-00C00C205365");
        public static readonly Guid ASF_Bitrate_Mutual_Exclusion_Object = new Guid("D6E229DC-35DA-11D1-9034-00A0C90349BE");
        public static readonly Guid ASF_Error_Correction_Object = new Guid("75B22635-668E-11CF-A6D9-00AA0062CE6C");
        public static readonly Guid ASF_Content_Description_Object = new Guid("75B22633-668E-11CF-A6D9-00AA0062CE6C");
        public static readonly Guid ASF_Extended_Content_Description_Object = new Guid("D2D0A440-E307-11D2-97F0-00A0C95EA850");
        public static readonly Guid ASF_Content_Branding_Object = new Guid("2211B3FA-BD23-11D2-B4B7-00A0C955FC6E");
        public static readonly Guid ASF_Stream_Bitrate_Properties_Object = new Guid("7BF875CE-468D-11D1-8D82-006097C9A2B2");
        public static readonly Guid ASF_Content_Encryption_Object = new Guid("2211B3FB-BD23-11D2-B4B7-00A0C955FC6E");
        public static readonly Guid ASF_Extended_Content_Encryption_Object = new Guid("298AE614-2622-4C17-B935-DAE07EE9289C");
        public static readonly Guid ASF_Digital_Signature_Object = new Guid("2211B3FC-BD23-11D2-B4B7-00A0C955FC6E");
        public static readonly Guid ASF_Padding_Object = new Guid("1806D474-CADF-4509-A4BA-9AABCB96AAE8");

        public static readonly Guid ASF_Extended_Stream_Properties_Object = new Guid("14E6A5CB-C672-4332-8399-A96952065B5A");
        public static readonly Guid ASF_Advanced_Mutual_Exclusion_Object = new Guid("A08649CF-4775-4670-8A16-6E35357566CD");
        public static readonly Guid ASF_Group_Mutual_Exclusion_Object = new Guid("D1465A40-5A79-4338-B71B-E36B8FD6C249");
        public static readonly Guid ASF_Stream_Prioritization_Object = new Guid("D4FED15B-88D3-454F-81F0-ED5C45999E24");
        public static readonly Guid ASF_Bandwidth_Sharing_Object = new Guid("A69609E6-517B-11D2-B6AF-00C04FD908E9");
        public static readonly Guid ASF_Language_List_Object = new Guid("7C4346A9-EFE0-4BFC-B229-393EDE415C85");
        public static readonly Guid ASF_Metadata_Object = new Guid("C5F8CBEA-5BAF-4877-8467-AA8C44FA4CCA");
        public static readonly Guid ASF_Metadata_Library_Object = new Guid("44231C94-9498-49D1-A141-1D134E457054");
        public static readonly Guid ASF_Index_Parameters_Object = new Guid("D6E229DF-35DA-11D1-9034-00A0C90349BE");
        public static readonly Guid ASF_Media_Object_Index_Parameters_Object = new Guid("6B203BAD-3F11-48E4-ACA8-D7613DE2CFA7");
        public static readonly Guid ASF_Timecode_Index_Parameters_Object = new Guid("F55E496D-9797-4B5D-8C8B-604DFE9BFB24");
        public static readonly Guid ASF_Compatibility_Object = new Guid("26F18B5D-4584-47EC-9F5F-0E651F0452C9");
        public static readonly Guid ASF_Advanced_Content_Encryption_Object = new Guid("43058533-6981-49E6-9B74-AD12CB86D58C");
        public static readonly Guid ASF_Audio_Media = new Guid("F8699E40-5B4D-11CF-A8FD-00805F5C442B");
        public static readonly Guid ASF_Video_Media = new Guid("BC19EFC0-5B4D-11CF-A8FD-00805F5C442B");
        public static readonly Guid ASF_Command_Media = new Guid("59DACFC0-59E6-11D0-A3AC-00A0C90348F6");
        public static readonly Guid ASF_JFIF_Media = new Guid("B61BE100-5B4E-11CF-A8FD-00805F5C442B");
        public static readonly Guid ASF_Degradable_JPEG_Media = new Guid("35907DE0-E415-11CF-A917-00805F5C442B");
        public static readonly Guid ASF_File_Transfer_Media = new Guid("91BD222C-F21C-497A-8B6D-5AA86BFC0185");
        public static readonly Guid ASF_Binary_Media = new Guid("3AFB65E2-47EF-40F2-AC2C-70A90D71D343");

        public static readonly Guid ASF_Extended_Stream_Type_Audio = new Guid("31178c9d03e14528b5823df9db22f503");

        public static readonly Guid ASF_Web_Stream_Media_Subtype = new Guid("776257D4-C627-41CB-8F81-7AC7FF1C40CC");
        public static readonly Guid ASF_Web_Stream_Format = new Guid("DA1E6B13-8359-4050-B398-388E965BF00C");

        public static readonly Guid ASF_No_Error_Correction = new Guid("20FB5700-5B55-11CF-A8FD-00805F5C442B");
        public static readonly Guid ASF_Audio_Spread = new Guid("BFC3CD50-618F-11CF-8BB2-00AA00B4E220");

        public static readonly Guid ASF_Content_Encryption_System_Windows_Media_DRM_Network_Devices = new Guid("7A079BB6-DAA4-4e12-A5CA-91D38DC11A8D");

        public static readonly Guid ASF_Reserved_1 = new Guid("ABD3D211-A9BA-11cf-8EE6-00C00C205365");
        public static readonly Guid ASF_Reserved_2 = new Guid("86D15241-311D-11D0-A3A4-00A0C90348F6");
        public static readonly Guid ASF_Reserved_3 = new Guid("4B1ACBE3-100B-11D0-A39B-00A0C90348F6");
        public static readonly Guid ASF_Reserved_4 = new Guid("4CFEDB20-75F6-11CF-9C0F-00A0C90349CB");

        public static readonly Guid ASF_Mutex_Language = new Guid("D6E22A00-35DA-11D1-9034-00A0C90349BE");
        public static readonly Guid ASF_Mutex_Bitrate = new Guid("D6E22A01-35DA-11D1-9034-00A0C90349BE");
        public static readonly Guid ASF_Mutex_Unknown = new Guid("D6E22A02-35DA-11D1-9034-00A0C90349BE");

        public static readonly Guid ASF_Bandwidth_Sharing_Exclusive = new Guid("AF6060AA-5197-11D2-B6AF-00C04FD908E9");
        public static readonly Guid ASF_Bandwidth_Sharing_Partial = new Guid("AF6060AB-5197-11D2-B6AF-00C04FD908E9");

        public static readonly Guid ASF_Payload_Extension_System_Timecode = new Guid("399595EC-8667-4E2D-8FDB-98814CE76C1E");
        public static readonly Guid ASF_Payload_Extension_System_File_Name = new Guid("E165EC0E-19ED-45D7-B4A7-25CBD1E28E9B");
        public static readonly Guid ASF_Payload_Extension_System_Content_Type = new Guid("D590DC20-07BC-436C-9CF7-F3BBFBF1A4DC");
        public static readonly Guid ASF_Payload_Extension_System_Pixel_Aspect_Ratio = new Guid("1B1EE554-F9EA-4BC8-821A-376B74E4C4B8");
        public static readonly Guid ASF_Payload_Extension_System_Sample_Duration = new Guid("C6BD9450-867F-4907-83A3-C77921B733AD");
        public static readonly Guid ASF_Payload_Extension_System_Encryption_Sample_ID = new Guid("6698B84E-0AFA-4330-AEB2-1C0A98D7A44D");
        public static readonly Guid ASF_Payload_Extension_dvr_ms_timing_rep_data = new Guid("fd3cc02a06db4cfa801c7212d38745e4");
        public static readonly Guid ASF_Payload_Extension_dvr_ms_vid_frame_rep_data = new Guid("dd6432cce22940db80f6d26328d2761f");
        public static readonly Guid ASF_Payload_Extension_System_Degradable_JPEG = new Guid("00E1AF06-7BEC-11D1-A582-00C04FC29CFB");
        #endregion
    }
}
