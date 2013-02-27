using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AsfMojo.Media;
using System.Drawing;
using AsfMojo.File;
using AsfMojo.Parsing;
using System.IO;
using AsfMojo.Configuration;
using System.Configuration;

namespace AsfMojoTest
{
    /// <summary>
    /// Basic Unit testing for the core AsfMojo library
    /// </summary>
    [TestClass]
    public class AsfMojoBaseTest
    {
        private string testVideoFileName = ConfigurationManager.AppSettings["VideoFile"];
        private string testAudioFileName = ConfigurationManager.AppSettings["AudioFile"];
        private double testVideoFileDuration;
        private double testAudioFileDuration;

        public AsfMojoBaseTest()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            AsfFileProperties fileProperties = asfFile.GetAsfObject<AsfFileProperties>();

            TimeSpan duration = TimeSpan.FromTicks((long)fileProperties.PlayDuration) - TimeSpan.FromMilliseconds(fileProperties.Preroll);
            testVideoFileDuration = duration.TotalSeconds;

            asfFile = new AsfFile(testVideoFileName);
            fileProperties = asfFile.GetAsfObject<AsfFileProperties>();
            duration = TimeSpan.FromTicks((long)fileProperties.PlayDuration) - TimeSpan.FromMilliseconds(fileProperties.Preroll);
            testAudioFileDuration = duration.TotalSeconds;
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion



        [TestMethod]
        /// <summary>
        /// Create an ASF wrapper object for a media file
        /// </summary>
        public void CreateAsfFile()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            Assert.IsNotNull(asfFile);
        }

        [TestMethod]
        public void EnumerateAsfObjects()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);

            //Test header objects in order of occurence in the test file

            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Header_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Content_Description_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_File_Properties_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Header_Extension_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Language_List_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Compatibility_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Metadata_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Padding_Object).Count);
            Assert.AreEqual(2, asfFile.GetAsfObjectByType(AsfGuid.ASF_Extended_Stream_Properties_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Index_Parameters_Placeholder_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Extended_Content_Description_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Codec_List_Object).Count);
            Assert.AreEqual(2, asfFile.GetAsfObjectByType(AsfGuid.ASF_Stream_Properties_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Stream_Bitrate_Properties_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Data_Object).Count);
            Assert.AreEqual(1, asfFile.GetAsfObjectByType(AsfGuid.ASF_Simple_Index_Object).Count);
        }

        [TestMethod]
        public void GetSingleAsfObjectByType()
        {

            AsfFile asfFile = new AsfFile(testVideoFileName);
            var fileProps = asfFile.GetAsfObject<AsfFileProperties>();

            Assert.IsNotNull(fileProps);
        }


        [TestMethod]
        public void EnumeratePackets()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);

            //Data Object returned from asfFile will not cover packets
            AsfDataObject asfDataObject = asfFile.GetAsfObject<AsfDataObject>();

            //to retrieve packets we need to pass a full file stream into AsfDataObject
            using (FileStream fs = new FileStream(testVideoFileName, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(asfDataObject.Position, SeekOrigin.Begin);
                AsfDataObject asfDataObjectFull = new AsfDataObject(fs, asfFile.PacketConfiguration);
            }

            Assert.AreEqual(648, asfFile.PacketConfiguration.Packets.Count);
        }

        [TestMethod]
        public void GetFps()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            AsfExtendedStreamProperties asfExtendedStreamProperties = asfFile.GetAsfObject<AsfExtendedStreamProperties>();

            if (asfExtendedStreamProperties.AvgTimePerFrame > 0)
            {
                double frameRate = 1.0 / TimeSpan.FromTicks((long)asfExtendedStreamProperties.AvgTimePerFrame).TotalSeconds;
            }
        }

        [TestMethod]
        public void CompareFirstDataPacket()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            long offsetFirstPacket = asfFile.PacketConfiguration.AsfHeaderSize;

            using (FileStream fs = new FileStream(testVideoFileName, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(offsetFirstPacket, SeekOrigin.Begin);
                byte[] packetData = new byte[asfFile.PacketConfiguration.AsfPacketSize];
                fs.Read(packetData, 0, (int)asfFile.PacketConfiguration.AsfPacketSize);

                AsfPacket packet = new AsfPacket(asfFile.PacketConfiguration, packetData);

                Assert.AreEqual(packet.SendTime, asfFile.PacketConfiguration.Packets[0].SendTime);
                Assert.AreEqual(packet.Payload.Count, asfFile.PacketConfiguration.Packets[0].Payload.Count);
                for(int i=0;i< packet.Payload.Count;i++)
                {
                    Assert.AreEqual(packet.Payload[i].PresentationTimeOffset, asfFile.PacketConfiguration.Packets[0].Payload[i].PresentationTimeOffset);
                    Assert.AreEqual(packet.Payload[i].PresentationTime, asfFile.PacketConfiguration.Packets[0].Payload[i].PresentationTime);
                    Assert.AreEqual(packet.Payload[i].StreamIDOffset, asfFile.PacketConfiguration.Packets[0].Payload[i].StreamIDOffset);
                    Assert.AreEqual(packet.Payload[i].StreamId, asfFile.PacketConfiguration.Packets[0].Payload[i].StreamId);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Not a valid ASF packet")]
        public void TryCreateInvalidPacket()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            long offsetFirstPacket = asfFile.PacketConfiguration.AsfHeaderSize;

            using (FileStream fs = new FileStream(testVideoFileName, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(offsetFirstPacket, SeekOrigin.Begin);
                byte[] packetData = new byte[asfFile.PacketConfiguration.AsfPacketSize];
                fs.Read(packetData, 0, (int)asfFile.PacketConfiguration.AsfPacketSize);
                packetData[0] = 1;
                packetData[1] = 1;
                AsfPacket packet = new AsfPacket(asfFile.PacketConfiguration, packetData);
            }
        }

        [TestMethod]
        public void CompareConfiguration()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);

            Assert.AreEqual<uint>(3000, asfFile.PacketConfiguration.AsfPreroll);
            Assert.AreEqual<uint>(171308, asfFile.PacketConfiguration.AsfBitRate);
            Assert.AreEqual<uint>(2, asfFile.PacketConfiguration.AsfVideoStreamId);
            Assert.AreEqual(180, asfFile.PacketConfiguration.ImageWidth);
            Assert.AreEqual(140, asfFile.PacketConfiguration.ImageHeight);
            Assert.AreEqual<uint>(1, asfFile.PacketConfiguration.AsfAudioStreamId);
            Assert.AreEqual<uint>(32000, asfFile.PacketConfiguration.AudioSampleRate);
            Assert.AreEqual<ushort>(16, asfFile.PacketConfiguration.AudioBitsPerSample);
            Assert.AreEqual<ushort>(2, asfFile.PacketConfiguration.AudioChannels);
        }

        [TestMethod]
        public void CreateAsfObject()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            AsfFileConfiguration config = new AsfFileConfiguration();

            AsfFileHeader asfFileHeader;
            using (FileStream fs = new FileStream(testVideoFileName, FileMode.Open, FileAccess.Read))
            {
                asfFileHeader = (AsfFileHeader)AsfObject.CreateAsfObject(AsfGuid.ASF_Header_Object, fs, config);
            }

            Assert.IsNotNull(asfFileHeader);
            Assert.AreEqual<uint>(5292, asfFileHeader.HeaderSize);
        }
    }
}
