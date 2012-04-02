using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.IO;

namespace Komodex.Common
{
    public static class Log
    {
        static Log()
        {
            LogFilename = "ApplicationLog.txt";
        }

        private static LogInstance _logger = new LogInstance(null);

        public static LogLevel Level { get; set; }

        public static bool LogToFile { get; set; }

        public static string LogFilename { get; set; }

        #region Debug

        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            _logger.Debug(message);
        }

        [Conditional("DEBUG")]
        public static void Debug(string format, params object[] args)
        {
            _logger.Debug(format, args);
        }

        #endregion

        #region Info

        [Conditional("DEBUG")]
        public static void Info(string message)
        {
            _logger.Info(message);
        }

        [Conditional("DEBUG")]
        public static void Info(string format, params object[] args)
        {
            _logger.Info(format, args);
        }

        #endregion

        #region Warning

        [Conditional("DEBUG")]
        public static void Warning(string message)
        {
            _logger.Warning(message);
        }

        [Conditional("DEBUG")]
        public static void Warning(string format, params object[] args)
        {
            _logger.Warning(format, args);
        }

        #endregion

        #region Error

        [Conditional("DEBUG")]
        public static void Error(string message)
        {
            _logger.Error(message);
        }

        [Conditional("DEBUG")]
        public static void Error(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        #endregion

        #region LogInstance

        public static LogInstance GetInstance(string source)
        {
            return new LogInstance(source);
        }

        public class LogInstance
        {
            public LogInstance(string source)
            {
                Source = source;
            }

            public string Source { get; set; }

            #region WriteMessage

            [Conditional("DEBUG")]
            private void WriteMessage(LogLevel level, string message)
            {
                if (Level > level)
                    return;

                // Format the message
                switch (level)
                {
                    case LogLevel.Debug:
                        message = "[DEBUG] " + message;
                        break;
                    case LogLevel.Info:
                        message = "[INFO] " + message;
                        break;
                    case LogLevel.Warning:
                        message = "[WARNING] " + message;
                        break;
                    case LogLevel.Error:
                        message = "[ERROR] " + message;
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(Source))
                    message = string.Format("[{0}] ", Source) + message;

                message = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff] ") + message;

                // Write to the debug output
                var lines = message.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    System.Diagnostics.Debug.WriteLine(lines[i].Trim('\r'));

                // Write to the log file
                if (Log.LogToFile)
                {
                    lock (Log.LogFilename)
                    {
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            using (TextWriter writer = new StreamWriter(store.OpenFile(Log.LogFilename, FileMode.Append)))
                            {
                                writer.WriteLine(message);
                            }
                        }
                    }
                }
            }

            [Conditional("DEBUG")]
            private void WriteMessage(LogLevel level, string format, params object[] args)
            {
                if (Level > level)
                    return;

                WriteMessage(level, string.Format(format, args));
            }

            #endregion

            #region Debug

            [Conditional("DEBUG")]
            public void Debug(string message)
            {
                WriteMessage(LogLevel.Debug, message);
            }

            [Conditional("DEBUG")]
            public void Debug(string format, params object[] args)
            {
                WriteMessage(LogLevel.Debug, format, args);
            }

            #endregion

            #region Info

            [Conditional("DEBUG")]
            public void Info(string message)
            {
                WriteMessage(LogLevel.Info, message);
            }

            [Conditional("DEBUG")]
            public void Info(string format, params object[] args)
            {
                WriteMessage(LogLevel.Info, format, args);
            }

            #endregion

            #region Warning

            [Conditional("DEBUG")]
            public void Warning(string message)
            {
                WriteMessage(LogLevel.Warning, message);
            }

            [Conditional("DEBUG")]
            public void Warning(string format, params object[] args)
            {
                WriteMessage(LogLevel.Warning, format, args);
            }

            #endregion

            #region Error

            [Conditional("DEBUG")]
            public void Error(string message)
            {
                WriteMessage(LogLevel.Error, message);
            }

            [Conditional("DEBUG")]
            public void Error(string format, params object[] args)
            {
                WriteMessage(LogLevel.Error, format, args);
            }

            #endregion
        }

        #endregion
    }

    public enum LogLevel
    {
        All = int.MinValue,
        Debug = -1,
        Info = 0,
        Warning = 1,
        Error = 2,
        None = int.MaxValue,
    }
}
