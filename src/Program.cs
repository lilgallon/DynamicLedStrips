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
using System.Linq;

namespace AudioBleLedsController
{
    struct Configuration
    {
        public enum SmoothingMode { NONE = 0, DYNAMIC = 1, VALUE = 2 };
        public enum AudioSensibility { BASS_LEVEL = 0, SOUND_LEVEL = 1, NONE = 2 };
        public enum ColorSensibility { COLOR_AVG = 0, NONE = 1 };

        public static SmoothingMode smoothingMode;
        public static double smoothingValue;
        public static AudioSensibility audioSensibility;
        public static ColorSensibility colorSensibility;
        public static CompatibleEndPoint device;

        /// <summary>
        /// Creates a string containing all the arguments needed to run
        /// the program with the current configuration
        /// </summary>
        /// <returns></returns>
        public static String ToArgument()
        {
            return smoothingMode + ";" +
                smoothingValue + ";" +
                audioSensibility + ";" +
                colorSensibility + ";" +
                device;
        }

        /// <summary>
        /// Populates the Configuration using the argument.
        /// It takes what ToArgument() returns.
        /// </summary>
        /// <param name="argument">Command line argument (see ToArgument())</param>
        /// <returns>true if it succeed, false otherwise (nothing is validated)</returns>
        public static bool LoadArgument(String argument)
        {
            try
            {
                String[] arguments = argument.Split(";");
                smoothingMode    = (SmoothingMode) Int32.Parse(arguments[0]);
                smoothingValue   = Double.Parse(arguments[1]);
                audioSensibility = (AudioSensibility) Int32.Parse(arguments[2]);
                colorSensibility = (ColorSensibility) Int32.Parse(arguments[3]);
                device           = new CompatibleEndPoint(arguments[4], arguments[5], arguments[6], arguments[7]);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    struct CompatibleEndPoint
    {
        public String deviceId;
        public String deviceName;
        public String deviceServiceName;
        public String deviceCharacteristicName;

        public CompatibleEndPoint(String id, String name, String service, String characteristic)
        {
            deviceId = id;
            deviceName = name;
            deviceServiceName = service;
            deviceCharacteristicName = characteristic;
        }
        
        public override String ToString()
        {
            return deviceId + ";" + deviceName + ";" + deviceServiceName + ";" + deviceCharacteristicName;
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
            Run(args).Wait();
        }

        /// <summary>
        /// The actual program, needs to be waited
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Run(string[] args)
        {
            LogHelper.PrintHeader();

            LogHelper.PrintTitle("Configuration");
            if (!await Configure(args))
            
            {
                LogHelper.Error("Something went wrong during configuration");
            }
            else
            {

                #region connection

                LogHelper.PrintTitle("Connection");
                LogHelper.Pending("Looking for BLE device of id " + Configuration.device.deviceId);
                device = await BleUtility.Connect(Configuration.device.deviceId);
                if (device == null)
                {
                    LogHelper.Error("Failed to connect to device");
                }

                #endregion

                #region services

                GattDeviceService targettedService = null;
                if (device != null)
                {
                    LogHelper.PrintTitle("Services");
                    LogHelper.Pending("Looking for service " + Configuration.device.deviceName + "...");

                    IReadOnlyList<GattDeviceService> services = await BleUtility.GetServices(device);

                    if (services != null)
                    {
                        LogHelper.Ok(String.Format("Found {0} service(s)", services.Count));
                        foreach (var service in services)
                        {
                            if (DisplayHelpers.GetServiceName(service) == Configuration.device.deviceServiceName)
                            {
                                LogHelper.Ok("Found service");
                                targettedService = service;
                                break;
                            }
                        }

                        if (targettedService == null)
                        {
                            LogHelper.Error("Couldn't find service " + Configuration.device.deviceServiceName);
                        }
                    }
                    else
                    {
                        LogHelper.Error("Device unreachable");
                    }
                }

                

                #endregion

                #region caracteristics

                GattCharacteristic characteristic = null;
                if (targettedService != null)
                {
                    LogHelper.PrintTitle("Caracteristics");
                    LogHelper.Pending("Looking for characteristic " + Configuration.device.deviceCharacteristicName + "...");
                    IReadOnlyList<GattCharacteristic> characteristics = await BleUtility.GetCharacteristics(targettedService);
                    foreach (var charact in characteristics)
                    {
                        if (DisplayHelpers.GetCharacteristicName(charact) == Configuration.device.deviceCharacteristicName)
                        {
                            LogHelper.Ok("Found characteristic");
                            characteristic = charact;
                        }
                    }

                    if (characteristic == null)
                    {
                        LogHelper.Error("Could not find characteristic " + Configuration.device.deviceCharacteristicName);
                    }
                }

                #endregion

                #region communication

                if (characteristic != null)
                {
                    LogHelper.PrintTitle("Communication");

                    if (BleUtility.IsWriteableCharateristic(characteristic))
                    {
                        Loop(characteristic);
                    }
                    else
                    {
                        LogHelper.Error("This characteristic does not have either the 'Write' or 'WriteWithoutResponse' properties");
                    }
                }

                #endregion

            }

            #region cleanup

            LogHelper.PrintTitle("Cleanup");
            LogHelper.Pending("Exiting properly");
            device?.Dispose();
            LogHelper.Ok("Done. Type a key to exit");
            Console.ReadKey(true);

            #endregion
        }

        /// <summary>
        /// Configures the program. If the arguments are passed, it will
        /// check if it's valid. If it's valid, it will populate the
        /// Configuration static struct.
        /// 
        /// Otherwise, it will ask the user to configure everything. At
        /// the end, the Configuration static struct is populated.
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true if it succeeded, false otherwise</returns>
        static async Task<bool> Configure(string[] args)
        {
            if (args.Count() != 0)
            {
                if (args.Count() == 1)
                {
                    LogHelper.Pending("Reading argument given");
                    if (Configuration.LoadArgument(args[0]))
                    {
                        LogHelper.Ok("Loaded argument"); 
                        return true;
                    }
                    else
                    {
                        LogHelper.Error("Failed to load argument");
                        return false;
                    }
                }
                else
                {
                    LogHelper.Error("Too many arguments passed");
                    return false;
                }
            } 
            else
            {
                int finder = LogHelper.AskUserToChoose(
                    "Device finder: ",
                    new string[] {"Automatic (will find the compatible devices)", "Manual (you know the device's id)"}
                );

                Configuration.device = finder == 0 ? await AutomaticScan() : AskForEndPoint();

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

            return true;
        }

        /// <summary>
        /// Ask for the user to specify the endpoint to use:
        /// id, service and characteristic.
        /// 
        /// The name is also asked, but it is not needed to
        /// communicate with the device.
        /// 
        /// </summary>
        /// <returns>the compatible end point given by the user</returns>
        static CompatibleEndPoint AskForEndPoint()
        {
            CompatibleEndPoint endPoint;

            endPoint.deviceId = LogHelper.AskUserForString("Device id: ");
            endPoint.deviceServiceName = LogHelper.AskUserForString("Service name: ");
            endPoint.deviceCharacteristicName = LogHelper.AskUserForString("Characteristic name: ");
            endPoint.deviceName = LogHelper.AskUserForString("Device name (just visual): ");

            return endPoint;
        }

        /// <summary>
        /// It looks for the compatible devices (discovery + analysis). Then, it asks
        /// for the user to choose the device. The selected device is returned
        /// through a CompatibleEndPoint object. It contains the id, service and
        /// characteristic name.
        /// </summary>
        /// <returns>The complete device id (or -1 if none were selected)</returns>
        static async Task<CompatibleEndPoint> AutomaticScan()
        {
            BleUtility.Discovery bleDiscovery = new BleUtility.Discovery();
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

            List<CompatibleEndPoint> compatibleEndPoints = new List<CompatibleEndPoint>();
            LogHelper.Pending("Checking compatibility of each device");
            LogHelper.IncrementIndentLevel();
            foreach (DeviceInformation device in devices)
            {
                LogHelper.Pending("Checking compatibility of device " + device.Id.Split("-").Last());
                LogHelper.IncrementIndentLevel();

                LogHelper.Pending("Connecting to device...");
                BluetoothLEDevice connection = await BleUtility.Connect(device.Id);
                if (connection == null)
                {
                    LogHelper.DecrementIndentLevel();
                    LogHelper.Warn("Failed to connect (doesn't mean that it's incompatible)");
                    continue;
                }
                LogHelper.Ok("Connected");

                LogHelper.Pending("Looking for services...");
                IReadOnlyList<GattDeviceService> services = await BleUtility.GetServices(connection);
                if (services == null)
                {
                    LogHelper.DecrementIndentLevel();
                    LogHelper.Warn("Gatt communication failed");
                    continue;
                }
                else if (services.Count == 0)
                {
                    LogHelper.DecrementIndentLevel();
                    LogHelper.Warn("No services found");
                    continue;
                }
                LogHelper.Ok(services.Count + " service(s) found");

                LogHelper.Pending("Looking for characteristics for each service...");
                LogHelper.IncrementIndentLevel();
                foreach (GattDeviceService service in services)
                {
                    LogHelper.Pending("Looking for characteristics of service " + DisplayHelpers.GetServiceName(service));
                    LogHelper.IncrementIndentLevel();

                    IReadOnlyList<GattCharacteristic> characteristics = await BleUtility.GetCharacteristics(service);
                    if (characteristics == null)
                    {
                        LogHelper.DecrementIndentLevel();
                        LogHelper.Warn("Failed to retrieve characteristics");
                        continue;
                    }
                    else if (characteristics.Count == 0)
                    {
                        LogHelper.DecrementIndentLevel();
                        LogHelper.Warn("No characteristics found");
                        continue;
                    }
                    LogHelper.Ok(characteristics.Count + " characteristic(s) found");

                    LogHelper.Pending("Checking compatibility of each characteristic...");
                    LogHelper.IncrementIndentLevel();
                    int compatibleCpt = 0;
                    foreach (GattCharacteristic characteristic in characteristics)
                    {
                        LogHelper.Pending("Checking characteristic " + DisplayHelpers.GetCharacteristicName(characteristic) + "...");

                        if (BleUtility.IsWriteableCharateristic(characteristic))
                        {
                            CompatibleEndPoint endPoint;
                            endPoint.deviceId = device.Id;
                            endPoint.deviceName = device.Name;
                            endPoint.deviceServiceName = DisplayHelpers.GetServiceName(service);
                            endPoint.deviceCharacteristicName = DisplayHelpers.GetCharacteristicName(characteristic);

                            compatibleCpt++;
                            compatibleEndPoints.Add(endPoint);
                            LogHelper.Ok("Compatible!");
                        }
                        else
                        {
                            LogHelper.Warn("Not compatible");
                        }
                    }
                    LogHelper.DecrementIndentLevel();
                    LogHelper.Ok(compatibleCpt + " compatible endpoint(s) found");
                    LogHelper.DecrementIndentLevel();
                }
                LogHelper.Ok("Finished looking for characteristics");
                LogHelper.DecrementIndentLevel();

                LogHelper.Ok("Finished compatibility check of device " + device.Id.Split('-').Last());
                LogHelper.DecrementIndentLevel();

            }
            LogHelper.DecrementIndentLevel();
            LogHelper.Ok("Finished analyzing devices");

            

            LogHelper.Ok("Compatible device(s):");
            LogHelper.IncrementIndentLevel();
            string[] ids = new string[compatibleEndPoints.Count];
            foreach (CompatibleEndPoint compatibleEndPoint in compatibleEndPoints)
            {
                LogHelper.Ok("name = '" + compatibleEndPoint.deviceName + "' id = '" + compatibleEndPoint.deviceId + "'");
                ids.Append(
                    compatibleEndPoint.deviceId + (compatibleEndPoint.deviceName != "" ? (" " + compatibleEndPoint.deviceName) : "")
                );
            }
            LogHelper.DecrementIndentLevel();

            int choice = LogHelper.AskUserToChoose("Choose the device to use: ", ids);
            bleDiscovery.Dispose();
            return compatibleEndPoints.ElementAt(choice);
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
