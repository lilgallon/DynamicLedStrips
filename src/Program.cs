using MSFTHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace AudioBleLedsController
{
    class Program
    {
        #region error codes
        readonly static int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly static int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly static int E_ACCESSDENIED = unchecked((int)0x80070005);
        #endregion

        static BluetoothLEDevice bluetoothLeDevice = null;

        static void Main(string[] args)
        {
            Run(args).Wait();
        }

        static async Task Run(string[] args)
        {
            Console.WriteLine("Connection");
            Console.WriteLine("-");

            #region connection

            String deviceId;

            if (args.Length == 0)
            {
                deviceId = "be:89:d0:01:7b:9c";
                Log("No arguments given, will use the id " + deviceId, LogType.WARNING);
            } 
            else
            {
                deviceId = args[0];
            }

            // BluetoothLE#BluetoothLE00:15:83:ed:e4:12-be:89:d0:01:7b:9c
            deviceId = "BluetoothLE#BluetoothLE00:15:83:ed:e4:12-" + deviceId;

            Log("Looking for BLE device of id " + deviceId, LogType.PENDING);
            bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (bluetoothLeDevice == null)
            {
                Log("Failed to connect to device", LogType.ERROR);
            }

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Services");
            Console.WriteLine("-");

            #region services

            GattDeviceService targettedService = null;

            if (bluetoothLeDevice != null)
            {
                Log("Looking for services", LogType.PENDING);
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    Log(String.Format("Found {0} services", services.Count), LogType.OK);
                    foreach (var service in services)
                    {
                        Log(String.Format("Service: {0}", DisplayHelpers.GetServiceName(service)), LogType.OK);
                    }

                    // TODO: select service

                    targettedService = services[1];
                }
                else
                {
                    Log("Device unreachable", LogType.ERROR);
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
                                Log(DisplayHelpers.GetCharacteristicName(c), LogType.OK);
                            }

                            // TODO: select

                            characteristic = characteristics[0];
                        }
                        else
                        {
                            Log("Error accessing service.", LogType.ERROR);
                        }
                    }
                    else
                    {
                        // Not granted access
                        Log("Error accessing service.", LogType.ERROR);
                    }
                }
                catch (Exception ex)
                {
                    Log("Restricted service. Can't read characteristics: " + ex.Message, LogType.ERROR);
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
                    Log("Correct properties!", LogType.OK);

                    String textToWrite = "7e00038703000000ef"; // jump rgb

                    var writeBuffer = CryptographicBuffer.DecodeFromHexString(textToWrite);
                    bool writeSuccessful = await WriteBufferToSelectedCharacteristicAsync(characteristic, writeBuffer);
                }
                else
                {
                    Log("These properties don't have 'Write' or 'WriteWithoutResponse'", LogType.ERROR);
                }
            }

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Cleanup");
            Console.WriteLine("-");

            #region cleanup

            Log("Exiting properly", LogType.PENDING);
            bluetoothLeDevice?.Dispose();
            Log("Done. Type a key to exit", LogType.OK);
            Console.ReadKey(true);

            #endregion
        }

        static void Log(String msg, LogType logLevel)
        {
            String prefix = "";
            switch (logLevel)
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

        private async static Task<bool> WriteBufferToSelectedCharacteristicAsync(GattCharacteristic characteristic, IBuffer buffer)
        {
            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await characteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    Log("Successfully wrote value to device", LogType.OK);
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
    }

    enum LogType
    {
        ERROR, WARNING, OK, PENDING
    }
}
