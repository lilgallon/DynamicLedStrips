using NAudio.Wave;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Media.Devices;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace GallonHelpers
{
    public static class Utility
    {
        #region error codes
        readonly static int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly static int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly static int E_ACCESSDENIED = unchecked((int)0x80070005);
        #endregion

        /// <summary>
        /// Created by Lilian Gallon, 11/01/2020
        /// 
        /// Simple Log function that adds a prefix according to the log type
        /// </summary>
        /// <param name="msg">The message to log</param>
        /// <param name="logLevel">The type of the message</param>

        public static void Log(String msg, LogType logType)
        {
            String prefix = "";
            switch (logType)
            {
                case LogType.OK:
                    prefix = "+";
                    break;
                case LogType.PENDING:
                    prefix = "~";
                    break;
                case LogType.WARNING:
                    prefix = "!";
                    break;
                case LogType.ERROR:
                    prefix = "-";
                    break;
            }

            Console.WriteLine("[" + prefix + "] " + msg);
        }

        /// <summary>
        /// Created by Lilian Gallon, 11/01/2020
        /// 
        /// It writes the given hexadecimal value to the given gatt charateristic
        /// 
        /// </summary>
        /// <param name="hex">The hex message to write</param>
        /// <param name="characteristic">The characteristic to override</param>
        public async static Task<bool> WriteHex(String hex, GattCharacteristic characteristic)
        {
            return await WriteBufferToSelectedCharacteristicAsync(CryptographicBuffer.DecodeFromHexString(hex), characteristic);
        }

        /// <summary>
        /// Created by Lilian Gallon, 11/01/2020
        /// 
        /// It writes the given buffer to the given gatt charateristic
        /// 
        /// </summary>
        /// <param name="buffer">The hex message to write</param>
        /// <param name="characteristic">The characteristic to override</param>
        /// <returns></returns>
        private async static Task<bool> WriteBufferToSelectedCharacteristicAsync(IBuffer buffer, GattCharacteristic characteristic)
        {
            try
            {
                var result = await characteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    return true;
                }
                else
                {
                    Log("Write failed: " + result.Status, LogType.WARNING);
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                Log("Write failed: " + ex.Message, LogType.ERROR);
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                // This usually happens when a device reports that it support writing, but it actually doesn't.
                Log("Write failed: " + ex.Message, LogType.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Created by Lilian Gallon, 11/01/2020
        /// 
        /// Taken and modified from https://stackoverflow.com/questions/22904069/c-sharp-how-to-get-pixel-color-data-from-the-screen
        /// </summary>
        /// <param name="rect">Rectangle that defines the screenshot</param>
        /// <returns>A bitmap containing the image data</returns>
        public static Bitmap CaptureFromScreen(Rectangle rect)
        {
            Bitmap bmpScreenCapture = null;

            if (rect == Rectangle.Empty)//capture the whole screen
            {
                // we suppose that the user has a 1920x1080 screen
                rect = new Rectangle(0, 0, 1920, 1080);
            }

            bmpScreenCapture = new Bitmap(rect.Width, rect.Height);

            Graphics p = Graphics.FromImage(bmpScreenCapture);


            p.CopyFromScreen(rect.X,
                     rect.Y,
                     0, 0,
                     rect.Size,
                     CopyPixelOperation.SourceCopy);


            p.Dispose();

            return bmpScreenCapture;
        }

        /// <summary>
        /// Created by Lilian Gallon, 11/01/2020
        /// 
        /// Taken and modified from  https://stackoverflow.com/questions/1068373/how-to-calculate-the-average-rgb-color-values-of-a-bitmap
        /// </summary>
        /// <param name="bm">The image</param>
        /// <returns>The average color of that image</returns>
        public static Color CalculateAverageColor(Bitmap bm)
        {
            int width = bm.Width;
            int height = bm.Height;
            int red = 0;
            int green = 0;
            int blue = 0;
            int minDiversion = 15; // drop pixels that do not differ by at least minDiversion between color values (white, gray or black)
            int dropped = 0; // keep track of dropped pixels
            long[] totals = new long[] { 0, 0, 0 };
            int bppModifier = bm.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images

            BitmapData srcData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            int stride = srcData.Stride;
            IntPtr Scan0 = srcData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * stride) + x * bppModifier;
                        red = p[idx + 2];
                        green = p[idx + 1];
                        blue = p[idx];
                        if (Math.Abs(red - green) > minDiversion || Math.Abs(red - blue) > minDiversion || Math.Abs(green - blue) > minDiversion)
                        {
                            totals[2] += red;
                            totals[1] += green;
                            totals[0] += blue;
                        }
                        else
                        {
                            dropped++;
                        }
                    }
                }
            }

            int count = width * height - dropped;
            int avgR = count > 0 ? (int)(totals[2] / count) : 0;
            int avgG = count > 0 ? (int)(totals[1] / count) : 0;
            int avgB = count > 0 ? (int)(totals[0] / count) : 0;

            return Color.FromArgb(avgR, avgG, avgB);
        }
    }

    public enum LogType
    {
        ERROR, WARNING, OK, PENDING
    }

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
        private float soundLevel = 0;

        /// <summary>
        /// It initializes everything. You can call GetSoundLevel() right after
        /// instanciating this class.
        /// </summary>
        public SoundListener()
        {
            InitCapture();

            MediaDevice.DefaultAudioRenderDeviceChanged += (sender, newDevice) =>
            {
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

            capture.DataAvailable += (object sender, WaveInEventArgs args) =>
            {
                // Interprets the sample as 32 bit floating point audio (-> FloatBuffer)
                soundLevel = 0;
                var buffer = new WaveBuffer(args.Buffer);

                for (int index = 0; index < args.BytesRecorded / 4; index++)
                {
                    var sample = buffer.FloatBuffer[index];
                    if (sample < 0) sample = -sample; // abs
                    if (sample > soundLevel) soundLevel = sample;
                }
            };

            capture.StartRecording();
        }

        /// <summary>
        /// The audio is taken from the default output device
        /// </summary>
        /// <returns>The current system sound level between 0.0f and 1.0f</returns>
        public float GetSoundLevel()
        {
            return soundLevel;
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
