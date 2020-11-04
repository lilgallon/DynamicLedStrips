using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
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

        #region connection

        /// <summary>
        /// Tries to connect to the given device
        /// </summary>
        /// <param name="id">Device id (ex: "be:89:d0:01:7b:9c")</param>
        /// <returns>BluetoothLEDevice: succed, null otherwise</returns>
        public static async Task<BluetoothLEDevice> Connect(String id)
        {
            id = "BluetoothLE#BluetoothLE00:15:83:ed:e4:12-" + id;
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
                        LogHelper.ResetIndentLevel();
                        LogHelper.Error("Error accessing device");
                        return null;
                    }
                } 
                else
                {
                    LogHelper.ResetIndentLevel();
                    LogHelper.Error("Error accessing device: access not granted");
                    return null;
                }
            }
            catch (Exception e)
            {
                LogHelper.ResetIndentLevel();
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
