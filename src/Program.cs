using MSFTHelpers;
using GallonHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using System.Threading;
using System.Drawing;

namespace AudioBleLedsController
{
    class Program
    {
        static volatile bool keepRunning = true;
        static BluetoothLEDevice bluetoothLeDevice = null;

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

            // BluetoothLE#BluetoothLE00:15:83:ed:e4:12-be:89:d0:01:7b:9c
            deviceId = "BluetoothLE#BluetoothLE00:15:83:ed:e4:12-" + deviceId;

            LogHelper.Pending("Looking for BLE device of id " + deviceId);
            bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (bluetoothLeDevice == null)
            {
                LogHelper.Error("Failed to connect to device");
            }

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Services");
            Console.WriteLine("-");

            #region services

            GattDeviceService targettedService = null;

            if (bluetoothLeDevice != null)
            {
                LogHelper.Pending("Looking for services");
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    LogHelper.Ok(String.Format("Found {0} services", services.Count));
                    foreach (var service in services)
                    {
                        LogHelper.Ok(String.Format("Service: {0}", DisplayHelpers.GetServiceName(service)));
                    }

                    // TODO: select service

                    targettedService = services[1];
                }
                else
                {
                    LogHelper.Error("Device unreachable");
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
                IReadOnlyList<GattCharacteristic> characteristics = null;

                try
                {
                    // Ensure we have access to the device.
                    var accessStatus = await targettedService.RequestAccessAsync();
                    if (accessStatus == DeviceAccessStatus.Allowed)
                    {
                        // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                        // and the new Async functions to get the characteristics of unpaired devices as well. 
                        var result = await targettedService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            characteristics = result.Characteristics;

                            foreach (GattCharacteristic c in characteristics)
                            {
                                LogHelper.Ok(DisplayHelpers.GetCharacteristicName(c));
                            }

                            // TODO: select

                            characteristic = characteristics[0];
                        }
                        else
                        {
                            LogHelper.Error("Error accessing service.");
                        }
                    }
                    else
                    {
                        // Not granted access
                        LogHelper.Error("Error accessing service.");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error("Restricted service. Can't read characteristics: " + ex.Message);
                }
            }

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Communication");
            Console.WriteLine("-");

            #region communication

            if (characteristic != null)
            {
                GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

                if (
                    properties.HasFlag(GattCharacteristicProperties.Write) || 
                    properties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)
                    )
                {
                    LogHelper.Ok("Correct properties!");

                    SoundListener soundListener = new SoundListener();

                    Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                        e.Cancel = true;
                        keepRunning = false;
                    };

                    LogHelper.Ok("Program running. Press CTRL+C to stop");

                    // Rectangle in the middle of the screen
                    Rectangle rect = new Rectangle(1920/4, 1080/4, 1920 / 4 * 2, 1080 / 4 * 2);
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
                        soundLevel = (int) ((sensitiveToBass ? soundListener.GetBassLevel() : soundListener.GetSoundLevel()) * 100f); // Between 0.0f and 100.0f

                        if (current == -1) current = soundLevel;

                        if (dynamicSmoothing)
                        {
                            current = (current + soundLevel) / 2;
                        }
                        else if (current < soundLevel && smoothness > 0)
                        {
                            current = Math.Min(current + 100/smoothness, soundLevel);
                        } 
                        else if (smoothness > 0)
                        {
                            current = Math.Max(current - 100/smoothness, soundLevel);
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
                        BleUtility.WriteHex(textToWrite, characteristic); // result ignored yes, we don't want for it to be blocking

                        // We don't want to analyze pixels as fast as we check for the sound level
                        cpt++;
                        if (cpt == 10)
                        {
                            new Thread(async () =>
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

                                await BleUtility.WriteHex("7e0003" + colorCode + "03000000ef", characteristic);
                            }).Start();

                            cpt = 0;
                        }
                        
                        Thread.Sleep(50);
                    }

                    soundListener.Dispose();
                }
                else
                {
                    LogHelper.Error("These properties don't have 'Write' or 'WriteWithoutResponse'");
                }
            }

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Cleanup");
            Console.WriteLine("-");

            #region cleanup

            LogHelper.Pending("Exiting properly");
            bluetoothLeDevice?.Dispose();
            LogHelper.Ok("Done. Type a key to exit");
            Console.ReadKey(true);

            #endregion
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
