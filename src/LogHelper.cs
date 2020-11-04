using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace GallonHelpers
{
    /// <summary>
    /// Created by Lilian Gallon, 11/04/2020
    /// 
    /// Simple Helper class to log messages to console.
    /// 
    /// </summary>
    public static class LogHelper
    {
        public enum LogType
        {
            OK, PENDING, WARNING, ERROR, 
        }

        private static int  indentLevel = 0;
        private static bool overwrite = false;
        private static int lastLineLength = 0;

        public static void Ok(String msg)
        {
            Log(msg, LogType.OK);
        }

        public static void Pending(String msg)
        {
            Log(msg, LogType.PENDING);
        }

        public static void Warn(String msg)
        {
            Log(msg, LogType.WARNING);
        }

        public static void Error(String msg)
        {
            Log(msg, LogType.ERROR);
        }

        /// <summary>
        /// Writes the message to the console according to the loglevel,
        /// and the indent level.
        /// 
        /// Will overwrite the current line if overwrite is set to true.
        /// 
        /// </summary>
        /// <param name="msg">The message to log</param>
        /// <param name="logLevel">The type of the message</param>

        public static void Log(String msg, LogType logType)
        {
            String prefix = "";
            switch (logType)
            {
                case LogType.OK:
                    prefix = "[+]: ";
                    break;
                case LogType.PENDING:
                    prefix = "[~]: ";
                    break;
                case LogType.WARNING:
                    prefix = "[!]: ";
                    break;
                case LogType.ERROR:
                    prefix = "[-]: ";
                    break;
            }

            for (int i = 0; i < indentLevel; i++)
            {
                prefix = " |  " + prefix;
            }

            if (overwrite)
            {
                String erase = "";
                for (int i = 0; i < lastLineLength; i++)
                {
                    erase += " ";
                }

                Console.Write("\r" + erase);
                Console.Write("\r" + prefix + msg);
            }
            else
            {
                Console.WriteLine(prefix + msg);
            }

            lastLineLength = (prefix + msg).Length;
        }

        /// <summary>
        /// Resets the log level to 0
        /// 
        /// Example:
        /// 
        /// [+] Services
        ///  |  [+] Service A
        ///  |  [+] Service B
        ///  |   |  [+] Property X
        ///  
        /// </summary>
        /// <param name="overwrite"></param>
        public static void ResetIndentLevel()
        {
            LogHelper.indentLevel = 0;
        }

        /// <summary>
        /// Increments the log indent level
        /// 
        /// Example:
        /// 
        /// [+] Services
        ///  |  [+] Service A
        ///  |  [+] Service B
        ///  |   |  [+] Property X
        ///  
        /// </summary>
        /// <param name="overwrite"></param>
        public static void IncrementIndentLevel()
        {
            LogHelper.indentLevel++;
        }

        /// <summary>
        /// Decrements the log indent level
        /// 
        /// Example:
        /// 
        /// [+] Services
        ///  |  [+] Service A
        ///  |  [+] Service B
        ///  |   |  [+] Property X
        ///  
        /// </summary>
        /// <param name="overwrite"></param>
        public static void DecrementIndentLevel()
        {
            LogHelper.indentLevel--;
        }

        public static int GetIndentLevel()
        {
            return LogHelper.indentLevel;
        }

        /// <summary>
        /// Will overwrite the current line if overwrite is set to true.
        /// </summary>
        /// <param name="overwrite"></param>
        public static void Overwrite(bool overwrite)
        {
            LogHelper.overwrite = overwrite;
        }
    }
}
