using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using AsfMojo;
using AsfMojo.File;
using AsfMojo.Parsing;
using AsfMojo.Media;
using System.Drawing;
using System.IO;

namespace AsfMojoTest
{

    [TestClass]
    public class AsfImageTest
    {

        private string testVideoFileName = ConfigurationManager.AppSettings["VideoFile"];
        private string testAudioFileName = ConfigurationManager.AppSettings["AudioFile"];
        private double testVideoFileDuration;

        public AsfImageTest()
        {
            AsfFile asfFile = new AsfFile(testVideoFileName);
            AsfFileProperties fileProperties = asfFile.GetAsfObject<AsfFileProperties>();
            TimeSpan duration = TimeSpan.FromTicks((long)fileProperties.PlayDuration) - TimeSpan.FromMilliseconds(fileProperties.Preroll);
            testVideoFileDuration = duration.TotalSeconds;
        }

        [TestMethod]
        public void CreateImageManual()
        {
            using(AsfStream asfStream = new AsfStream(AsfStreamType.asfImage, testVideoFileName, 1.0))
            using (AsfImage asfImage = new AsfImage(asfStream))
            {
                Bitmap bitmap = asfImage.GetImage();
                Assert.AreNotEqual(bitmap, null);
            }
        }

        [TestMethod]
        public void CreateImageStatic()
        {
            Bitmap bitmap = AsfImage.FromFile(testVideoFileName, 1.0);
            Assert.AreNotEqual(bitmap, null);
        }

        [TestMethod]
        public void CreateImageFluent()
        {
            Bitmap bitmap = AsfImage.FromFile(testVideoFileName)
                                    .AtOffset(1.0);

            Assert.AreNotEqual(bitmap, null);
        }


        [TestMethod]
        public void CreateImageFileCanBeDeleted()
        {
            string tmpFile = Path.ChangeExtension(testVideoFileName, "tmp");

            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            File.Copy(testVideoFileName, tmpFile);

            Bitmap bitmap = AsfImage.FromFile(tmpFile).AtOffset(1.0);
            File.Delete(tmpFile);
            Assert.IsFalse(File.Exists(tmpFile));
        }

        [TestMethod]
        [ExpectedException(typeof(AsfStreamException), "Cannot create image stream for audio file")]
        public void TryCreateImageFromAudioFile()
        {
            Bitmap bitmap = AsfImage.FromFile(testAudioFileName)
                                    .AtOffset(1.0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TryCreateImageFromAudioStream()
        {
            using (AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0))
            {
                AsfImage asfImage = new AsfImage(asfStream);
            }
        }

        [TestMethod]
        public void CreateImageFromOffsetRandom()
        {
            Random r = new Random();
            double[] startOffsets = new double[100];

            startOffsets = Enumerable.Range(0, 100).Select(x => (testVideoFileDuration-1) * r.NextDouble()).ToArray();

            foreach (double startOffset in startOffsets)
            {
                Bitmap bitmap = AsfImage.FromFile(testVideoFileName).AtOffset(startOffset);
                Assert.IsNotNull(bitmap);
            }
        }

    }
}
