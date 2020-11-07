using MSFTHelpers;
using GallonHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading;
using System.Drawing;
using Windows.Devices.Enumeration;

namespace AudioBleLedsController
{
    struct Configuration
    {
        public enum SmoothingMode { NONE = 0, DYNAMIC = 1, VALUE = 2 };
        public enum AudioSensibility { BASS_LEVEL = 0, SOUND_LEVEL = 1, NONE = 2 };
        public enum ColorSensibility { COLOR_AVG = 0, NONE = 1 };

        public static SmoothingMode smoothingMode;
        public static AudioSensibility audioSensibility;
        public static ColorSensibility colorSensibility;

        public static string deviceId;
        public static double smoothingValue;

        /// <summary>
        /// Creates a string containing all the arguments needed to run
        /// the program with the current configuration
        /// </summary>
        /// <returns></returns>
        public static String SaveSettings()
        {
            return "todo";
        }
    }

    class Program
    {
        static volatile bool keepRunning = true;
        static BluetoothLEDevice device = null;

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Configure();
            //Run(args).Wait();
        }

        /// <summary>
        /// The actual program, needs to be waited
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Run(string[] args)
        {
            LogHelper.PrintHeader();

            #region configuration
            
            LogHelper.PrintTitle("Configuration");
            Configure();

            #endregion

            #region connection

            LogHelper.PrintTitle("Connection");

            String deviceId;

            if (args.Length == 0)
            {
                deviceId = "be:89:d0:01:7b:9c";
                LogHelper.Warn("No arguments given, will use the id " + deviceId);
            } 
            else
            {
                deviceId = args[0];
            }

            LogHelper.Pending("Looking for BLE device of id " + deviceId);
            device = await BleUtility.Connect(deviceId);
            if (device == null)
            {
                LogHelper.Error("Failed to connect to device");
            }

            #endregion

            #region services

            LogHelper.PrintTitle("Services");

            GattDeviceService targettedService = null;

            if (device != null)
            {
                LogHelper.Pending("Looking for services");

                IReadOnlyList<GattDeviceService> services = await BleUtility.GetServices(device);

                if (services != null)
                {
                    LogHelper.Ok(String.Format("Found {0} service(s)", services.Count));
                    LogHelper.IncrementIndentLevel();
                    foreach (var service in services)
                    {
                        LogHelper.Ok(String.Format("Service: {0}", DisplayHelpers.GetServiceName(service)));
                    }
                    LogHelper.DecrementIndentLevel();

                    // TODO: select service
                    targettedService = services[1];
                }
                else
                {
                    LogHelper.Error("Device unreachable");
                    LogHelper.ResetIndentLevel();
                }
            }

            #endregion

            #region caracteristics

            LogHelper.PrintTitle("Caracteristics");

            GattCharacteristic characteristic = null;

            if (targettedService != null)
            {
                LogHelper.Pending("Looking for characteristics");
                // Error messages are handled in the function
                IReadOnlyList<GattCharacteristic> characteristics = await BleUtility.GetCharacteristics(targettedService);

                LogHelper.Ok(String.Format("Found {0} characteristic(s)", characteristics.Count));
                LogHelper.IncrementIndentLevel();
                foreach (var charact in characteristics)
                {
                    LogHelper.Ok(String.Format("Characteristic: {0}", DisplayHelpers.GetCharacteristicName(charact)));
                }
                LogHelper.DecrementIndentLevel();

                if (characteristics != null)
                {
                    characteristic = characteristics[0];
                }
            }

            #endregion

            #region communication

            LogHelper.PrintTitle("Communication");

            if (characteristic != null)
            {
                
                if (BleUtility.IsWriteableCharateristic(characteristic))
                {
                    LogHelper.Ok("Correct properties!");
                    Loop(characteristic);
                }
                else
                {
                    LogHelper.Error("This characteristic does not have the 'Write' or 'WriteWithoutResponse' properties");
                }
            }

            #endregion

            #region cleanup

            LogHelper.PrintTitle("Cleanup");
            LogHelper.Pending("Exiting properly");
            device?.Dispose();
            LogHelper.Ok("Done. Type a key to exit");
            Console.ReadKey(true);

            #endregion
        }

        static void Configure()
        {
            BleUtility.Discovery bleDiscovery = null;

            int finder = LogHelper.AskUserToChoose(
                "Device finder: ",
                new string[] {"Automatic (will find the compatible devices)", "Manual (you know the device's id)"}
            );

            if (finder == 0)
            {
                bleDiscovery = new BleUtility.Discovery();
                bleDiscovery.Start();

                LogHelper.Overwrite(true);
                LogHelper.NewLine(false);

                while (!bleDiscovery.HasEnded())
                {
                    Thread.Sleep(50);
                }
                LogHelper.Overwrite(false);
                LogHelper.NewLine(true);
                LogHelper.Log("");

                LogHelper.Ok("Devices:");
                LogHelper.IncrementIndentLevel();
                List<DeviceInformation> devices = bleDiscovery.GetDevices();
                foreach (DeviceInformation device in devices)
                {
                    LogHelper.Ok(device.Id + " " + device.Name);
                }
                LogHelper.DecrementIndentLevel();

                // TODO: check for compatible devices by connecting and checking if they're connectable
                // TODO: ask for the user to select a device if one or more were found

                bleDiscovery.Dispose();
            }
            else if (finder == 1)
            {
                Configuration.deviceId = LogHelper.AskUserForString("Device id: ");
            }

            int settings = LogHelper.AskUserToChoose(
                "Settings: ",
                new string[] { "Default (recommended settings)", "Manual (let me choose)" }
            );

            if (settings == 0)
            {
                // Default settings
                Configuration.smoothingMode = Configuration.SmoothingMode.NONE;
                Configuration.audioSensibility = Configuration.AudioSensibility.BASS_LEVEL;
                Configuration.colorSensibility = Configuration.ColorSensibility.COLOR_AVG;
            }
            else
            {
                Configuration.smoothingMode = (Configuration.SmoothingMode) LogHelper.AskUserToChoose(
                    "Smoothing: ",
                    new string[] { "None (default: no smoothing)", "Dynamic (automatic smooth)", "Value (you define the smoothness)" }
                );

                if (Configuration.smoothingMode == Configuration.SmoothingMode.VALUE)
                {
                    Configuration.smoothingValue = LogHelper.AskUserForDouble("Value for smoothing (>0.1 & <100)", 0.1d, 100d);
                }

                Configuration.audioSensibility = (Configuration.AudioSensibility) LogHelper.AskUserToChoose(
                    "Audio sensibility: ",
                    new string[] { "Bass (default)", "Audio level", "None (brightness won't change)" }
                );

                Configuration.colorSensibility = (Configuration.ColorSensibility) LogHelper.AskUserToChoose(
                    "Color sensibility: ",
                    new string[] { "Average screen color (default)", "None (color won't change)" }
                );
            }
        }

        /// <summary>
        /// The program Loop: CTRL+C to stop it properly
        /// </summary>
        /// <param name="characteristic"></param>
        static void Loop(GattCharacteristic characteristic)
        {
            SoundListener soundListener = new SoundListener();

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                keepRunning = false;
            };

            LogHelper.Ok("Program running. Press CTRL+C to stop");

            // Rectangle in the middle of the screen
            Rectangle rect = new Rectangle(1920 / 4, 1080 / 4, 1920 / 4 * 2, 1080 / 4 * 2);
            String colorCode = "";
            Color color;
            int soundLevel;
            String brightness;
            String textToWrite;

            // Smooth brightness variation
            int current = -1;
            int smoothness = 0;
            bool dynamicSmoothing = false;

            // Sound type selector
            bool sensitiveToBass = true; // false -> sensitive to sound level in general
            soundListener.ListenForBass(sensitiveToBass);

            int cpt = 0;
            while (keepRunning)
            {
                soundLevel = (int)((sensitiveToBass ? soundListener.GetBassLevel() : soundListener.GetSoundLevel()) * 100f); // Between 0.0f and 100.0f

                if (current == -1) current = soundLevel;

                if (dynamicSmoothing)
                {
                    current = (current + soundLevel) / 2;
                }
                else if (current < soundLevel && smoothness > 0)
                {
                    current = Math.Min(current + 100 / smoothness, soundLevel);
                }
                else if (smoothness > 0)
                {
                    current = Math.Max(current - 100 / smoothness, soundLevel);
                }
                else
                {
                    current = soundLevel;
                }


                // Format: 7e 00 01 brightness 00 00 00 00 ef
                // brightness: 0x00-0x64 (0-100)
                // So we need to convert the soundLevel to hex so that 100.0f is 0x64 and 0.0f is 0x00
                brightness = (current).ToString("X");
                textToWrite = "7e0001" + brightness + "00000000ef";
                _ = BleUtility.WriteHex(textToWrite, characteristic); // we don't want it to be blocking

                // We don't want to analyze pixels as fast as we check for the sound level
                cpt++;
                if (cpt == 10)
                {
                    new Thread(() =>
                    {
                        color = ScreenUtils.CalculateAverageScreenColorAt(rect);

                        if (color.GetBrightness() >= 0.85f)
                        {
                            // white
                            colorCode = "86";
                        }
                        else if (color.GetHue() < 25 || color.GetHue() >= 330)
                        {
                            // red
                            colorCode = "80";
                        }
                        else if (color.GetHue() >= 25 && color.GetHue() < 65)
                        {
                            // yellow
                            colorCode = "84";
                        }
                        else if (color.GetHue() >= 65 && color.GetHue() < 180)
                        {
                            // green
                            colorCode = "82";
                        }
                        else if (color.GetHue() >= 180 && color.GetHue() < 200)
                        {
                            // cyan
                            colorCode = "83";
                        }
                        else if (color.GetHue() >= 200 && color.GetHue() < 250)
                        {
                            // blue
                            colorCode = "81";
                        }
                        else if (color.GetHue() >= 250 && color.GetHue() < 330)
                        {
                            // magenta
                            colorCode = "85";
                        }

                        _ = BleUtility.WriteHex("7e0003" + colorCode + "03000000ef", characteristic);
                    }).Start();

                    cpt = 0;
                }

                Thread.Sleep(50);
            }

            soundListener.Dispose();
        }
    }
}
