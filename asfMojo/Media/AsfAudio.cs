using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsMediaLib;
using System.Runtime.InteropServices;
using System.IO;

namespace AsfMojo.Media
{
    /// <summary>
    /// A single PCM audio sample
    /// </summary>
    public class AudioSample
    {
        private float[] _sample;

        public AudioSample(float[] sample)
        {
            _sample = new float[sample.Length];
            Array.Copy(sample, _sample, sample.Length);
        }

        public float Left { get { return _sample[0]; } }
        public float Right { get { return _sample[1]; } }
    }

    /// <summary>
    /// Audio extractor based on an underlying ASF stream
    /// </summary>
    public sealed class AsfAudio : IDisposable
    {
        public AsfStream BaseStream { get { return _asfStream; } }

        private bool _disposed = false;
        private AsfStream _asfStream;
        private AsfIStream _asfMemoryStream;
        private IWMSyncReader _syncReader;

        Queue<AudioSample> _sampleBuffer;

        public AsfAudio(AsfStream asfStream)
        {
            _asfStream = asfStream;
            _asfMemoryStream = null;
            _sampleBuffer = new Queue<AudioSample>();
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) //managed resources
                {
                    if (_asfMemoryStream != null)
                    {
                        _asfMemoryStream.Close();
                        _asfMemoryStream = null;
                    }

                    if (_asfStream != null)
                        _asfStream.Close();
                }
                Marshal.FinalReleaseComObject(_syncReader);
                _disposed = true;
            }
        }

        ~AsfAudio()
        {
            Dispose(false);
        }


        /// <summary>
        /// Get the PCM Wave memory stream for the extracted audio data
        /// </summary>
        public WaveMemoryStream GetWaveStream()
        {
            MemoryStream ms = new MemoryStream();
            WriteTo(ms);
            ms.Position = 0;
            WaveMemoryStream waveMemoryStream = new WaveMemoryStream(ms, 
                                                                     (int)_asfStream.Configuration.AudioSampleRate, 
                                                                     _asfStream.Configuration.AudioBitsPerSample, 
                                                                     _asfStream.Configuration.AudioChannels);
            return waveMemoryStream;
        }

        /// <summary>
        /// Write the PCM audio data to a stream
        /// </summary>
        public void WriteTo(Stream stream)
        {
            _asfMemoryStream = new AsfIStream(_asfStream);
            WMUtils.WMCreateSyncReader(IntPtr.Zero, Rights.Playback, out _syncReader);
            _syncReader.OpenStream(_asfMemoryStream);

            short audioStreamNum = (short)_asfStream.Configuration.AsfAudioStreamId;
            _syncReader.SetReadStreamSamples(audioStreamNum, false);

            long cnsSampleTime;
            long cnsSampleDuration;
            SampleFlag dwFlags;
            INSSBuffer pSample;
            int dwOutputNum = 0;
            short dwStreamNum = 0;

            try
            {
                while (true)
                {
                    _syncReader.GetNextSample(audioStreamNum, out pSample, out cnsSampleTime, out cnsSampleDuration, out dwFlags, out dwOutputNum, out dwStreamNum);
                    IntPtr pBuffer;
                    int bufferLength;
                    pSample.GetBufferAndLength(out pBuffer, out bufferLength);
                    byte[] sampleData = new byte[bufferLength];
                    Marshal.Copy(pBuffer, sampleData, 0, bufferLength);
                    Marshal.FinalReleaseComObject(pSample);

                    stream.Write(sampleData, 0, sampleData.Length);
                }
            }
            catch (COMException) // no more samples or corrupted content
            {
            }
        }

        /// <summary>
        /// Get audio sample bytes from the underlying stream
        /// </summary>
        public byte[] GetSampleBytes(int maxSampleCount)
        {
            _asfMemoryStream = new AsfIStream(_asfStream);
            WMUtils.WMCreateSyncReader(IntPtr.Zero, Rights.Playback, out _syncReader);
            _syncReader.OpenStream(_asfMemoryStream);

            short audioStreamNum = (short)_asfStream.Configuration.AsfAudioStreamId;
            _syncReader.SetReadStreamSamples(audioStreamNum, false);

            long cnsSampleTime;
            long cnsSampleDuration;
            SampleFlag dwFlags;
            INSSBuffer pSample;
            int dwOutputNum = 0;
            short dwStreamNum = 0;

            List<byte> sampleList = new List<byte>();

            long samplesRead = 0;
            int sampleSize = _asfStream.Configuration.AudioBitsPerSample / 8;

            try
            {
                while (samplesRead < maxSampleCount)
                {
                    _syncReader.GetNextSample(audioStreamNum, out pSample, out cnsSampleTime, out cnsSampleDuration, out dwFlags, out dwOutputNum, out dwStreamNum);
                    IntPtr pBuffer;
                    int bufferLength;
                    pSample.GetBufferAndLength(out pBuffer, out bufferLength);
                    byte[] sampleData = new byte[bufferLength];
                    Marshal.Copy(pBuffer, sampleData, 0, bufferLength);
                    Marshal.FinalReleaseComObject(pSample);

                    samplesRead += sampleData.Length / (sampleSize *  _asfStream.Configuration.AudioChannels);
                    sampleList.AddRange(sampleData);
                }
            }
            catch (COMException) // no more samples or corrupted content
            {
            }
            return sampleList.Take(maxSampleCount).ToArray();
        }

        public IEnumerable<AudioSample> GetSamples(int maxSampleCount = 0)
        {
            _asfMemoryStream = new AsfIStream(_asfStream);
            WMUtils.WMCreateSyncReader(IntPtr.Zero, Rights.Playback, out _syncReader);
            _syncReader.OpenStream(_asfMemoryStream);

            short audioStreamNum = (short)_asfStream.Configuration.AsfAudioStreamId;
            _syncReader.SetReadStreamSamples(audioStreamNum, false);

            long cnsSampleTime;
            long cnsSampleDuration;
            SampleFlag dwFlags;
            INSSBuffer pSample;
            int dwOutputNum = 0;
            short dwStreamNum = 0;

            int totalSampleCount = 0;

            while (true)
            {
                if (_sampleBuffer.Count > 0)
                    yield return _sampleBuffer.Dequeue();

                else
                {
                    if(totalSampleCount >= maxSampleCount)
                        yield break;

                    try
                    {
                        _syncReader.GetNextSample(audioStreamNum, out pSample, out cnsSampleTime, out cnsSampleDuration, out dwFlags, out dwOutputNum, out dwStreamNum);
                        IntPtr pBuffer;
                        int bufferLength;
                        pSample.GetBufferAndLength(out pBuffer, out bufferLength);
                        byte[] sampleData = new byte[bufferLength];
                        Marshal.Copy(pBuffer, sampleData, 0, bufferLength);
                        Marshal.FinalReleaseComObject(pSample);

                        float sample = 0;
                        float[] samples = new float[_asfStream.Configuration.AudioChannels];
                        int fullSampleSize = _asfStream.Configuration.AudioChannels * (_asfStream.Configuration.AudioBitsPerSample / 8);
                        int takeSamplesPerSec = 10000; 
                        int sampleStep = (int)( fullSampleSize * (_asfStream.Configuration.AudioSampleRate / takeSamplesPerSec));

                        for (int sampleOffset = 0; sampleOffset < sampleData.Length; sampleOffset += sampleStep)
                        {
                            for (int i = 0; i < _asfStream.Configuration.AudioChannels; i++)
                            {
                                if (_asfStream.Configuration.AudioBitsPerSample == 16)
                                {
                                    sample = BitConverter.ToInt16(sampleData, sampleOffset + i * 2);
                                    sample = sample / Int16.MaxValue;
                                }
                                else if (_asfStream.Configuration.AudioBitsPerSample == 32)
                                {
                                    sample = BitConverter.ToInt32(sampleData, sampleOffset + i * 4);
                                    sample = sample / Int32.MaxValue;
                                }
                                samples[i] = sample;
                            }

                            totalSampleCount++;
                            if (maxSampleCount == 0 || totalSampleCount <= maxSampleCount)
                                _sampleBuffer.Enqueue(new AudioSample(samples));
                            else
                                continue;
                        }
                    }
                    catch (COMException) // no more samples or corrupted content
                    {
                        totalSampleCount = maxSampleCount; 
                    }
                }
            }
        }
    }
}
