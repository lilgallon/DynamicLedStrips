using NAudio.Dsp;
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
    }
}
