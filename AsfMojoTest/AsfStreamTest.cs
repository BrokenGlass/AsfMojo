using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AsfMojo.Media;
using AsfMojo.File;
using AsfMojo.Parsing;
using System.Configuration;

namespace AsfMojoTest
{
    [TestClass]
    public class AsfStreamTest
    {
        private string testVideoFileName = ConfigurationManager.AppSettings["VideoFile"];
        private string testBadFileName = ConfigurationManager.AppSettings["BadFile"];
        private string testAudioFileName = ConfigurationManager.AppSettings["AudioFile"];

        [TestMethod]
        public void CreateAudioAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0, 2.0);
            Assert.IsNotNull(asfStream);
        }

        [TestMethod]
        public void CreateImageAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfImage, testVideoFileName, 1.0, 2.0);
            Assert.IsNotNull(asfStream);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TryCreateInvalidFileNameAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfStream, "", 0.0, 1.0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TryCreateInvalidFileAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfStream, testBadFileName, 0, 1.0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TryCreateInvalidRangeAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, -1.0, 4.0);
        }


        [TestMethod]
        [ExpectedException(typeof(AsfStreamException))]
        public void TryCreateNotCoveredRangeAsfStream()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0, 5000);
        }

        [TestMethod]
        public void ValidateAsfStreamLength()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfStream, testVideoFileName, 1.0, 2.0);
            int bytesRead = 0;
            int totalBytesRead = 0;

            byte[] data = new byte[8192];

            do
            {
                bytesRead = asfStream.Read(data, 0, data.Length);
                totalBytesRead += bytesRead;
            } while(bytesRead > 0);

            Assert.AreEqual(asfStream.Length, totalBytesRead);
        }


    }
}
