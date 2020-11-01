using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
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

        public static void Log(String msg, LogType logLevel)
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


        public async static Task<bool> WriteHex(String hex, GattCharacteristic characteristic)
        {
            return await WriteBufferToSelectedCharacteristicAsync(characteristic, CryptographicBuffer.DecodeFromHexString(hex));
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
    public enum LogType
    {
        ERROR, WARNING, OK, PENDING
    }
}
