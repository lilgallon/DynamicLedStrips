using NAudio.Dsp;
using NAudio.Wave;
using System;
using Windows.Media.Devices;

namespace GallonHelpers
{
    /// <summary>
    /// Created by Lilian Gallon, 11/01/2020
    /// 
    /// Used to get the sound level of the operating system.
    /// It gets updated automatically to ALWAYS listen to the default
    /// output device.
    /// </summary>
    public class SoundListener
    {
        private WasapiLoopbackCapture capture;

        // Sound level
        private float soundLevel = 0;
        private float bassLevel = 0;

        // Settings
        private bool listenForBass = false;

        // Audio spectrum
        private float[] lastFftBuffer; // the last saved data
        private bool bufferAvailible = false; // prevents reading the buffer if it's being written in
        private const int fftLength = 2048; // 44.1kHz

        /// <summary>
        /// It initializes everything. You can call GetSoundLevel() right after
        /// instanciating this class.
        /// ListenForBass turned off by default (use ListenForBass(true))
        /// </summary>
        public SoundListener()
        {
            InitCapture();

            MediaDevice.DefaultAudioRenderDeviceChanged += (sender, newDevice) =>
            {
                // TODO: log that defautl device changed
                Dispose();
                InitCapture();
            };
        }

        ~SoundListener()
        {
            Dispose();
        }

        /// <summary>
        /// Inits the capture:
        /// - inits the capture to listen to the current default output device
        /// - creates the listener
        /// - starts recording
        /// </summary>
        private void InitCapture()
        {
            // Takes the current default output device
            capture = new WasapiLoopbackCapture();

            // Used to get the audio spectrum using FFT
            int fftPos = 0;
            int m = (int)Math.Log(fftLength, 2.0);
            object _lock = new object();
            Complex[] fftBuffer = new Complex[fftLength]; // the data
            lastFftBuffer = new float[fftLength]; // the last data saved

            capture.DataAvailable += (object sender, WaveInEventArgs args) =>
            {
                // Interprets the sample as 32 bit floating point audio (-> FloatBuffer)
                soundLevel = 0;
                var buffer = new WaveBuffer(args.Buffer);

                for (int index = 0; index < args.BytesRecorded / 4; index++)
                {
                    var sample = buffer.FloatBuffer[index];

                    // Sound level
                    if (sample < 0) sample = -sample; // abs
                    if (sample > soundLevel) soundLevel = sample;

                    // Bass level

                    if (listenForBass)
                    {
                        // HannWindow sample with amplitude -> amplitude to decibels
                        fftBuffer[fftPos].X = (float)(sample * FastFourierTransform.HannWindow(fftPos, fftLength));
                        fftBuffer[fftPos].Y = 0;
                        fftPos++;

                        if (fftPos >= fftLength)
                        {
                            fftPos = 0;

                            FastFourierTransform.FFT(true, m, fftBuffer);

                            lock (_lock)
                            {
                                bufferAvailible = false;

                                for (int c = 0; c < fftLength; c++)
                                {
                                    float amplitude = (float)Math.Sqrt(fftBuffer[c].X * fftBuffer[c].X + fftBuffer[c].Y * fftBuffer[c].Y);
                                    lastFftBuffer[c] = amplitude;
                                }

                                bufferAvailible = true;
                            }
                        }
                    }
                }


            };

            capture.StartRecording();
        }

        /// <summary>
        /// If true, it will analyse the specturm of every sample. It's optimized:
        /// from 0% processor use to 0%.
        /// </summary>
        /// <param name="listenForBass">if true, it will listen for the bass</param>
        public void ListenForBass(bool listenForBass)
        {
            this.listenForBass = listenForBass;
            bufferAvailible = false;
        }

        /// <summary>
        /// Given a frequency in Hz, returns the amplitude of the last recorded audio spectrum.
        /// </summary>
        /// <param name="frequency">Frequency in Hz</param>
        /// <returns></returns>
        private int GetFFTFrequencyIndex(int frequency)
        {
            int index = (frequency / (capture.WaveFormat.SampleRate / fftLength / capture.WaveFormat.Channels));
            return index;
        }

        /// <summary>
        /// If possible, it updates the bassLevel variable.
        /// </summary>
        private void UpdateBassLevel()
        {
            if (bufferAvailible)
            {

                float avg = 0;

                // Listens from 0 Hz to 250Hz somehow

                const int freqStart = 30;
                const int freqEnd = 100;
                const int freqStep = 10;

                for (int freq = freqStart; freq <= freqEnd; freq += freqStep)
                {
                    avg += Math.Min(lastFftBuffer[GetFFTFrequencyIndex(freq)] * 100f, 1.0f);
                }

                bassLevel = avg / ((freqEnd - freqStart) / freqStep + 1);
            }
        }

        /// <summary>
        /// The audio is taken from the default output device.
        /// Make sure that ListenForBass is set to true.
        /// </summary>
        /// <returns>The current system sound level between 0.0f and 1.0f</returns>
        public float GetSoundLevel()
        {
            return soundLevel;
        }

        /// <summary>
        /// The audio is taken from the default output device
        /// </summary>
        /// <returns>The current system bass level between 0.0f and 1.0f</returns>
        public float GetBassLevel()
        {
            UpdateBassLevel();
            return bassLevel;
        }

        /// <summary>
        /// Clears the allocated resources
        /// </summary>
        public void Dispose()
        {
            capture?.StopRecording();
            capture?.Dispose();
        }
    }
}
