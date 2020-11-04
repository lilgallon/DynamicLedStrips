using System;

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
            OK, PENDING, WARNING, ERROR, QUESTION, EMPTY
        }

        private static int  indentLevel = 0;
        private static bool overwrite = false;
        private static bool newLine = true;

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

        public static void Question(String msg)
        {
            Log(msg, LogType.QUESTION);
        }

        public static void Log(String msg)
        {
            Log(msg, LogType.EMPTY);
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
                case LogType.QUESTION:
                    prefix = "[?]: ";
                    break;
            }

            for (int i = 0; i < indentLevel; i++)
            {
                prefix = " |  " + prefix;
            }

            if (overwrite)
            {
                // Clear line
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentLineCursor);

                // Write over it
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(prefix + msg);
            }
            else
            {
                if (newLine) Console.WriteLine(prefix + msg);
                else Console.Write(prefix + msg);
            }
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

        public static void NewLine(bool newLine)
        {
            LogHelper.newLine = newLine;
        }

        public static int AskUserToChoose(String question, String[] choices)
        {
            LogHelper.Question(question);
            LogHelper.IncrementIndentLevel();
            for (int i = 0; i < choices.Length; i++)
            {
                LogHelper.Log("[" + i + "]: " + choices[i]);
            }
            LogHelper.DecrementIndentLevel();

            LogHelper.Log("");
            LogHelper.NewLine(false);
            int choice = -1;
            do
            {
                LogHelper.Question("Choice: ");
                LogHelper.Overwrite(true); // order important
            }
            while (!Int32.TryParse(Console.ReadLine(), out choice) || choice < 0 || choice >= choices.Length);

            LogHelper.Overwrite(false);
            LogHelper.NewLine(true);

            return choice;
        }

        public static void PrintHeader()
        {
            LogHelper.Log("______  _      _____ ");
            LogHelper.Log("|  _  \\| |    /  ___|");
            LogHelper.Log("| | | || |    \\ `--. ");
            LogHelper.Log("| | | || |     `--. \\  Dynamic LED Strips");
            LogHelper.Log("| |/ / | |____/\\__/ /  Version 0.1.0");
            LogHelper.Log("|___/  \\_____/\\____/   MIT License, (c) Lilian Gallon 2020");
            LogHelper.Log("");
        }

        public static void PrintTitle(String title)
        {
            LogHelper.Log(title);
            LogHelper.Log("-");
        }
    }
}
