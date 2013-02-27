using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using AsfMojo.File;
using AsfMojo.Parsing;
using AsfMojo.Media;
using System.Media;
using AsfMojo;

namespace AsfMojoTest
{
    [TestClass]
    public class AsfAudioTest
    {
        private string testVideoFileName = ConfigurationManager.AppSettings["VideoFile"];
        private string testAudioFileName = ConfigurationManager.AppSettings["AudioFile"];
        private double testVideoFileDuration;
        private double testAudioFileDuration;

        public AsfAudioTest()
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

        [TestMethod]
        public void CreatePlayableWaveMemoryStreamManual()
        {
            //WaveStreamFromFile
            using(AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0, 4.0))
            using (AsfAudio asfAudio = new AsfAudio(asfStream))
            {
                WaveMemoryStream waveMemoryStream = asfAudio.GetWaveStream();
                Assert.IsNotNull(waveMemoryStream);

                //Soundplayer will throw an exception if this is not a valid Wave stream
                SoundPlayer soundPlayer = new SoundPlayer(waveMemoryStream);
            }
        }

        [TestMethod]
        public void CreatePlayableWaveMemoryStreamStatic()
        {
            WaveMemoryStream waveMemoryStream = WaveMemoryStream.FromFile(testVideoFileName, 1.0, 4.0);
            SoundPlayer soundPlayer = new SoundPlayer(waveMemoryStream);
        }


        [TestMethod]
        public void CreatePlayableWaveMemoryStreamFluent()
        {
            WaveMemoryStream waveMemoryStream = WaveMemoryStream.FromFile(testVideoFileName)
                                                                .From(1.0)
                                                                .To(4.0);
            SoundPlayer soundPlayer = new SoundPlayer(waveMemoryStream);
        }

        [TestMethod]
        public void IterateAudioSamples()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0, 10.0);
            AsfAudio asfAudio = new AsfAudio(asfStream);
            int requestedSampleCount = 10000;
            int receivedSampleCount = 0;
            foreach (AudioSample audioSample in asfAudio.GetSamples(requestedSampleCount))
            {
                receivedSampleCount++;
            }
            Assert.AreEqual(requestedSampleCount, receivedSampleCount);
        }


        [TestMethod]
        public void GetAudioSamples()
        {
            AsfStream asfStream = new AsfStream(AsfStreamType.asfAudio, testVideoFileName, 1.0, 10.0);
            AsfAudio asfAudio = new AsfAudio(asfStream);
            int requestedSampleCount = 10000;

            byte[] data = asfAudio.GetSampleBytes(requestedSampleCount);
            Assert.AreEqual(requestedSampleCount, data.Length);
        }

        [TestMethod]
        public void CreatePlayableAudioSegmentFromOffsetRandom()
        {
            Random r = new Random();

            var ranges = Enumerable.Range(0, 100).Select(x =>
            {
                double startOffset = (testVideoFileDuration - 1) * r.NextDouble();
                double endOffset = startOffset + r.Next(1, (int)(testVideoFileDuration - startOffset));
                return new { startOffset, endOffset };
            }).ToArray();

            foreach (var range in ranges)
            {

                WaveMemoryStream waveMemoryStream = WaveMemoryStream.FromFile(testVideoFileName, range.startOffset, range.endOffset);
                Assert.IsNotNull(waveMemoryStream);

                //Soundplayer will throw an exception if this is not a valid Wave stream
                SoundPlayer soundPlayer = new SoundPlayer(waveMemoryStream);
            }
        }


    }
}
