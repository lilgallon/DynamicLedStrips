using MSFTHelpers;
using GallonHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading;
using System.Drawing;

namespace AudioBleLedsController
{
    class Program
    {
        static volatile bool keepRunning = true;
        static BluetoothLEDevice device = null;

        static void Main(string[] args)
        {
            Run(args).Wait();
        }

        static async Task Run(string[] args)
        {
            PrintHeader();

            Console.WriteLine("Connection");
            Console.WriteLine("-");

            #region connection

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

            Console.WriteLine("");
            Console.WriteLine("Services");
            Console.WriteLine("-");

            #region services

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

            Console.WriteLine("");
            Console.WriteLine("Caracteristics");
            Console.WriteLine("-");

            #region caracteristics

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

            Console.WriteLine("");
            Console.WriteLine("Communication");
            Console.WriteLine("-");

            #region communication

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

            Console.WriteLine("");
            Console.WriteLine("Cleanup");
            Console.WriteLine("-");

            #region cleanup

            LogHelper.Pending("Exiting properly");
            device?.Dispose();
            LogHelper.Ok("Done. Type a key to exit");
            Console.ReadKey(true);

            #endregion
        }

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

        static void PrintHeader()
        {
            Console.WriteLine("______  _      _____ ");
            Console.WriteLine("|  _  \\| |    /  ___|");
            Console.WriteLine("| | | || |    \\ `--. ");
            Console.WriteLine("| | | || |     `--. \\  Dynamic LED Strips");
            Console.WriteLine("| |/ / | |____/\\__/ /  Version 0.1.0");
            Console.WriteLine("|___/  \\_____/\\____/   MIT License, (c) Lilian Gallon 2020");
            Console.WriteLine("");
        }
    }
}
