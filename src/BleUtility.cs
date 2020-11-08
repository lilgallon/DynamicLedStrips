using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Haptics;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace GallonHelpers
{
    /// <summary>
    /// Created by Lilian Gallon, 11/01/2020
    /// 
    /// Functions to communicate with Bluetooth LE devices.
    /// Does not interact with regular Bluetooth devices. So
    /// for example it is possible to use a gaming controller
    /// at the same time.
    /// 
    /// </summary>
    public static class BleUtility
    {
        #region error codes
        readonly static int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly static int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly static int E_ACCESSDENIED = unchecked((int)0x80070005);
        #endregion

        #region discovery

        public class Discovery
        {
            #region settings
            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            private string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };
            // Showw paired and non-paired in a single query.
            string allBLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            #endregion

            private DeviceWatcher deviceWatcher = null;

            // This list may contain devices that are no longer reacheable
            private List<DeviceInformation> devicesWithNames = new List<DeviceInformation>();
            // This list contains the currently reacheable devices
            private List<DeviceInformation> devices = new List<DeviceInformation>();
            // We need these two Lists to give names to devices when the discovery ends
            // because sometime, when updating, the name of a device becomes "" instead of
            // its name

            private bool ended = true;

            ~Discovery()
            {
                Dispose();
            }

            /// <summary>
            /// Starts discovery for BLE devices.
            /// HasEnded() will return true once finished.
            /// If you want to stop during the discovery,
            /// you can call Stop().
            /// 
            /// Handles logging.
            /// </summary>
            public void Start()
            {
                if (deviceWatcher == null)
                {
                    deviceWatcher = DeviceInformation.CreateWatcher(allBLEDevices, requestedProperties, DeviceInformationKind.AssociationEndpoint);

                    deviceWatcher.Added += DeviceWatcher_Added;
                    deviceWatcher.Updated += DeviceWatcher_Updated;
                    deviceWatcher.Removed += DeviceWatcher_Removed;
                    deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                    deviceWatcher.Stopped += DeviceWatcher_Stopped;
                }

                LogHelper.Pending("Discovery in progress...");
                LogHelper.IncrementIndentLevel();

                LogHelper.Overwrite(true);
                LogHelper.NewLine(false);

                ended = false;
                devices.Clear();
                deviceWatcher.Start();
            }

            /// <summary>
            /// Stops the discovery properly.
            /// The devices are saved.
            /// </summary>
            public void Stop()
            {
                if (deviceWatcher != null)
                {
                    deviceWatcher.Stop();

                    deviceWatcher.Added -= DeviceWatcher_Added;
                    deviceWatcher.Updated -= DeviceWatcher_Updated;
                    deviceWatcher.Removed -= DeviceWatcher_Removed;
                    deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                    deviceWatcher.Stopped -= DeviceWatcher_Stopped;
                    
                    deviceWatcher = null;
                }

                End();
            }

            /// <summary>
            /// You need to start the discovery first by using Start()
            /// Then wait for it to finish using HasEnded()
            /// Then you can get the devices
            /// 
            /// GetDevices() returns the devices found during the last
            /// discovery. If you start a discovery, it clears the list.
            /// 
            /// </summary>
            /// <returns>BLE devices found during the discovery</returns>
            public List<DeviceInformation> GetDevices()
            {
                return devices;
            }

            public bool HasEnded()
            {
                return ended;
            }

            private void End()
            {
                ended = true;
                LogHelper.Overwrite(false);
                LogHelper.NewLine(true);

                devices.AddRange(devicesWithNames);
                devices = devices.Distinct().ToList();
                devicesWithNames.Clear();
                LogHelper.DecrementIndentLevel();
            }

            private DeviceInformation FindDevice(string id, List<DeviceInformation> devices)
            {
                foreach (DeviceInformation deviceInfo in devices)
                {
                    if (deviceInfo.Id == id)
                    {
                        return deviceInfo;
                    }
                }
                return null;
            }

            private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
            {
                if (ended) return;

                if (FindDevice(deviceInfo.Id, devices) == null)
                {
                    LogHelper.Log("Device \"" + (deviceInfo.Name == "" ? "?" : deviceInfo.Name) + "\" added, id=" + deviceInfo.Id.Split("-").Last());
                    devices.Add(deviceInfo);
                }

                if (deviceInfo.Name != "" && FindDevice(deviceInfo.Id, devicesWithNames) == null)
                {
                    devicesWithNames.Add(deviceInfo);
                }
            }

            private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
            {
                if (ended) return;

                DeviceInformation deviceInfo = FindDevice(deviceInfoUpdate.Id, devices);
                if (deviceInfo != null)
                {
                    LogHelper.Log("Device \"" + (deviceInfo.Name == "" ? "?" : deviceInfo.Name) + "\" updated, id=" + deviceInfo.Id.Split("-").Last());
                    deviceInfo.Update(deviceInfoUpdate);
                }

                if (deviceInfo.Name != "" && FindDevice(deviceInfoUpdate.Id, devicesWithNames) == null)
                {
                    devicesWithNames.Add(deviceInfo);
                }
            }

            private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
            {
                if (ended) return;

                DeviceInformation deviceInfo = FindDevice(deviceInfoUpdate.Id, devices);
                if (deviceInfo != null)
                {
                    LogHelper.Log("Device \"" + (deviceInfo.Name == "" ? "?" : deviceInfo.Name) + "\" removed, id=" + deviceInfo.Id.Split("-").Last());
                    devices.Remove(deviceInfo);
                }
            }

            private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
            {
                LogHelper.Ok("Discovery ended, " + devices.Count + " device(s) found");
                End();
            }

            private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
            {
                if (!ended) // on dispose it will log that otherwise
                    LogHelper.Ok("Discovery stopped, " + devices.Count + " device(s) found");
                End();
            }

            public void Dispose()
            {
                Stop();
                devices.Clear();
            }
        }
        
        #endregion

        #region connection

        /// <summary>
        /// Tries to connect to the given device
        /// </summary>
        /// <param name="id">Device id (found during discovery)</param>
        /// <returns>BluetoothLEDevice: succed, null otherwise</returns>
        public static async Task<BluetoothLEDevice> Connect(String id)
        {
            return await BluetoothLEDevice.FromIdAsync(id);
        }

        #endregion

        #region services

        /// <summary>
        /// Tries to retrieve the device's services.
        /// </summary>
        /// <param name="device">the connected device</param>
        /// <returns>List of services if succeed, null otherwise</returns>
        public static async Task<IReadOnlyList<GattDeviceService>> GetServices(BluetoothLEDevice device)
        {
            GattDeviceServicesResult result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            return result.Status == GattCommunicationStatus.Success ? result.Services : null;
        }

        #endregion

        #region caracteristics

        /// <summary>
        /// Retrieves the characteristics of the given service.
        /// The error logging is handled in the function.
        /// </summary>
        /// <param name="service"></param>
        /// <returns>The list of characteristic if it succeed, null otherwise</returns>
        public static async Task<IReadOnlyList<GattCharacteristic>> GetCharacteristics(GattDeviceService service)
        {
            try
            {
                DeviceAccessStatus status = await service.RequestAccessAsync();

                if (status == DeviceAccessStatus.Allowed)
                {
                    GattCharacteristicsResult result = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        return result.Characteristics;
                    }
                    else
                    {
                        LogHelper.Error("Error accessing device");
                        return null;
                    }
                } 
                else
                {
                    LogHelper.Error("Error accessing device: access not granted");
                    return null;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("Restricted service. Can't read characteristics: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Returns true if the given characteristic is writeable.
        /// </summary>
        /// <param name="characteristic"></param>
        /// <returns></returns>
        public static bool IsWriteableCharateristic(GattCharacteristic characteristic)
        {
            return (
                characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) ||
                characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)
            );
        }



        #endregion

        #region writing utilities

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
                    LogHelper.Warn("Write failed: " + result.Status);
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                LogHelper.Error("Write failed: " + ex.Message);
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                // This usually happens when a device reports that it support writing, but it actually doesn't.
                LogHelper.Error("Write failed: " + ex.Message);
                return false;
            }
        }

        #endregion
    
        
    }
}
