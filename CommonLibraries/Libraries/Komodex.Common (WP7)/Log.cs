using System;
using System.Diagnostics;
using System.IO;

namespace Komodex.Common
{
    public class Log
    {
        protected const string DefaultLogFilename = "ApplicationLog.txt";

        public Log(string name = null)
        {
            Name = name;
        }

        #region Main Log Instance

        private static Log _main = new Log();
        public static Log Main
        {
            get { return _main; }
        }

        #endregion

        #region Static Properties

        /// <summary>
        /// Gets or sets the default minimum message level for all log instances.
        /// </summary>
        public static LogLevel DefaultLogLevel { get; set; }

        public static bool LogToFile { get; set; }

        private static string _logFilename = DefaultLogFilename;
        public static string LogFilename
        {
            get { return _logFilename; }
            set { _logFilename = value; }
        }

        #endregion

        #region Properties

        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimum message level for this instance.
        /// If set, this value overrides the default minimum message level.
        /// </summary>
        public LogLevel? Level { get; set; }

        /// <summary>
        /// Gets the effective minimum message level for this instance.
        /// </summary>
        public LogLevel EffectiveLevel
        {
            get
            {
#if DEBUG
                if (Level.HasValue)
                    return Level.Value;
                return DefaultLogLevel;
#else
                return LogLevel.None;
#endif
            }
        }

        #endregion

        #region WriteMessage

        [Conditional("DEBUG")]
        private void WriteMessage(LogLevel level, string message)
        {
            if (EffectiveLevel > level)
                return;

            // Format the message
            if (!string.IsNullOrEmpty(Name))
                message = string.Format("[{0}] ", Name) + message;

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

            message = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff] ") + message;

            // Write to the debug output
            var lines = message.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                System.Diagnostics.Debug.WriteLine(((i > 0) ? "    " : "") + lines[i].Trim('\r'));

            // Write to the log file

            if (Log.LogToFile)
            {
                LogMessageToFile(message);
            }
        }

        [Conditional("DEBUG")]
        private void WriteMessage(LogLevel level, string format, params object[] args)
        {
            WriteMessage(level, string.Format(format, args));
        }

#if WINDOWS_PHONE
        [Conditional("DEBUG")]
        private void LogMessageToFile(string message)
        {
            lock (Log.LogFilename)
            {
                using (var store = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
                using (TextWriter writer = new StreamWriter(store.OpenFile(Log.LogFilename, FileMode.Append)))
                {
                    writer.WriteLine(message);
                }
            }
        }
#else
        [Conditional("DEBUG")]
        private async void LogMessageToFile(string message)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var logFile = await localFolder.CreateFileAsync(Log.LogFilename, Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.Storage.FileIO.AppendTextAsync(logFile, message + Environment.NewLine);
        }
#endif

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
