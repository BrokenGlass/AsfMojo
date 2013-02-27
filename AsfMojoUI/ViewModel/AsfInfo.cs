using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using AsfMojo.Parsing;
using AsfMojo.Utils;

namespace AsfMojoUI.ViewModel
{
    public class AsfInfo
    {
        public static List<AsfHeaderItem> GetHeaderObjects(string fileName)
        {
            List<AsfHeaderItem> asfHeaderItems = new List<AsfHeaderItem>();
            bool isFirstObject = true;

            AsfHeaderItem asfHeaderItem = null;
            AsfHeaderItem asfHeaderExtensionsItem = null;

            AsfHeaderItem.Configuration.Reset();

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    while (true)
                    {
                        AsfObject someObject = new AsfObject(fs);

                        long objSize = (long)someObject.Size;
                        if (objSize == 0)
                            break;
                        Guid objGuid = someObject.Guid;

                        if (isFirstObject && objGuid != AsfGuid.ASF_Header_Object) // invalid file
                            return null;

                        if (objGuid == AsfGuid.ASF_Header_Object)
                        {
                            asfHeaderItem = new AsfFileHeaderItem(fs);
                            asfHeaderItems.Add(asfHeaderItem);
                            isFirstObject = false;
                        }
                        else if (objGuid == AsfGuid.ASF_Stream_Bitrate_Properties_Object)
                        {
                            AsfHeaderItem item = new AsfStreamBitratePropertiesItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_File_Properties_Object)
                        {
                            AsfFilePropertiesItem item = new AsfFilePropertiesItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Header_Extension_Object)
                        {
                            asfHeaderExtensionsItem = new AsfHeaderExtensionItem(fs);
                            asfHeaderItem.Add(asfHeaderExtensionsItem);
                        }
                        else if (objGuid == AsfGuid.ASF_Language_List_Object)
                        {
                            AsfLanguageListObjectItem item = new AsfLanguageListObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Stream_Properties_Object)
                        {
                            AsfStreamPropertiesObjectItem item = new AsfStreamPropertiesObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Extended_Stream_Properties_Object)
                        {
                            AsfExtendedStreamPropertiesItem item = new AsfExtendedStreamPropertiesItem(fs);
                            asfHeaderExtensionsItem.Add(item);

                        }
                        else if (objGuid == AsfGuid.ASF_Compatibility_Object)
                        {
                            AsfCompatibilityObjectItem item = new AsfCompatibilityObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Metadata_Object)
                        {
                            AsfMetadataObjectItem item = new AsfMetadataObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Padding_Object)
                        {
                            AsfPaddingObjectItem item = new AsfPaddingObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Index_Parameters_Placeholder_Object)
                        {
                            AsfIndexParametersPlaceholderItem item = new AsfIndexParametersPlaceholderItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Extended_Content_Description_Object)
                        {
                            AsfExtendedContentDescriptionItem item = new AsfExtendedContentDescriptionItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Codec_List_Object)
                        {
                            AsfCodecListObjectItem item = new AsfCodecListObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Data_Object)
                        {
                            AsfDataObjectItem item = new AsfDataObjectItem(fs);
                            asfHeaderItems.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Simple_Index_Object)
                        {
                            AsfSimpleIndexObjectItem item = new AsfSimpleIndexObjectItem(fs);
                            asfHeaderItems.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Bitrate_Mutual_Exclusion_Object)
                        {
                            AsfBitrateMutualExclusionObjectItem item = new AsfBitrateMutualExclusionObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Content_Description_Object)
                        {
                            AsfContentDescriptionObjectItem item = new AsfContentDescriptionObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Stream_Prioritization_Object)
                        {
                            AsfStreamPrioritizationObjectItem item = new AsfStreamPrioritizationObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Timecode_Index_Parameters_Object)
                        {
                            AsfTimecodeIndexParametersObjectItem item = new AsfTimecodeIndexParametersObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Index_Parameters_Object)
                        {
                            AsfIndexParametersObjectItem item = new AsfIndexParametersObjectItem(fs);
                            asfHeaderExtensionsItem.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Index_Object)
                        {
                            AsfIndexObjectItem item = new AsfIndexObjectItem(fs);
                            asfHeaderItems.Add(item);
                        }
                        else if (objGuid == AsfGuid.ASF_Script_Command_Object)
                        {
                            AsfScriptCommandObjectItem item = new AsfScriptCommandObjectItem(fs);
                            asfHeaderItem.Add(item);
                        }
                        else //Unknown object
                        {
                            fs.Seek(fs.Position + objSize, SeekOrigin.Begin);
                        }

                    }
                    return asfHeaderItems;
                }
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}


