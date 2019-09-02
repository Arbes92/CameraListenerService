using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Configuration;

namespace CameraListenerService.Utils
{
    /// <summary>
    /// Singleton. Deals with Applicatiuon Event Log etc.
    /// Add functionality to log to console for command line file import versions?
    /// </summary>
    public class Logger
    {
#if !(DEBUG)
        //private readonly String LogPath = "R:\\ListenerLogs\\" + new AssemblyName(Assembly.GetExecutingAssembly().FullName).Name;
        //private readonly String LogPath = "C:\\ListenerLogs\\" + new AssemblyName(Assembly.GetExecutingAssembly().FullName).Name;
        private readonly String LogPath = "\\\\192.168.53.12\\Listener Logs\\" + new AssemblyName(Assembly.GetExecutingAssembly().FullName).Name;
#else
        private readonly String LogPath = "C:\\Workplace\\Temp\\ListenerLogs\\";
#endif

        private static String logSuffix;
        public void SetSuffixForFlatFileLogging(String suffix)
        {
            logSuffix = suffix;
        }

        private static string m_ServiceLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        /// <summary>
        /// The instance of the class
        /// </summary>
        private static Logger m_Instance;

        /// <summary>
        /// Gets the instance of this Singleton
        /// </summary>
        public static Logger GetInstance()
        {
            return m_Instance ?? (m_Instance = new Logger());
        }

        /// <summary>
        /// Add a message to the application event log
        /// </summary>
        public void Message(string message, string logSuffix)
        {
            var log = new EventLog();
            try
            {
                var logName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                log.Log = "Application";
                if (!EventLog.SourceExists(logName + logSuffix))
                {
                    EventLog.CreateEventSource(logName + logSuffix, log.Log);
                }
                log.Source = logName + logSuffix;
                log.WriteEntry(message, EventLogEntryType.Information);
            }
            catch
            {
            }
            finally
            {
                log.Dispose();
            }
        }
        public void Warning(string message, string logSuffix)
        {
            var log = new EventLog();
            try
            {
                var logName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                log.Log = "Application";
                if (!EventLog.SourceExists(logName + logSuffix))
                {
                    EventLog.CreateEventSource(logName + logSuffix, log.Log);
                }
                log.Source = logName + logSuffix;
                log.WriteEntry(message, EventLogEntryType.Warning);
            }
            catch
            {
            }
            finally
            {
                log.Dispose();
            }
        }
        /// <summary>
        /// Add an exception to the application event log
        /// </summary>
        public void Exception(string message, Exception e, string logSuffix, List<NotificationChannel> channels = null)
        {
            var log = new EventLog();  // should creat this object once and keep it?
            try
            {
                var logName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                log.Log = "Application";
                if (!EventLog.SourceExists(logName + logSuffix))
                {
                    EventLog.CreateEventSource(logName + logSuffix, log.Log);
                }
                var suf = logName + logSuffix;
                var inner = e.InnerException == null ? string.Empty : e.InnerException.Message;

                log.Source = suf;
                var msg = message + Environment.NewLine
                    + "Message: " + e.Message + Environment.NewLine
                    + "Source: " + e.Source + Environment.NewLine
                    + "Inner Exception Message: " + inner + Environment.NewLine
                    + "Stack Trace: " + e.StackTrace + Environment.NewLine;
                log.WriteEntry(msg, EventLogEntryType.Error);

                var config = (ServiceConfigurationSection)ConfigurationManager.GetSection("CameraListenerService.Configs");
                Notify.Error(config, string.Format("{0} error", suf), msg, channels);
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                log.Dispose();
            }
        }
        public void WriteToLogFile(string trackerId, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder);
            using (var w = File.AppendText(logFile))
            {
                w.WriteLine("{0}: {1}",
                            DateTime.Now.ToLongTimeString(),
                            trackerId);
            }
        }
        public void WriteFullFile(string fileContents, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder);
            File.AppendAllText(logFile, fileContents);
        }
        public void WritePayloadToLogFile(string logMessage, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder);
            using (var w = File.AppendText(logFile))
            {
                w.WriteLine(logMessage);
            }
        }


        internal void WritePayloadBinaryToLogFile(byte[] logMessage, int length, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder);
            using (var binWriter = new BinaryWriter(File.Open(logFile, FileMode.Append)))
            {
                // Write string 
                binWriter.Write(logMessage);
                binWriter.Write(Environment.NewLine);
            }
        }
        public void WritePayloadToLogFile(byte[] logMessage, int length, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder);
            using (var w = File.AppendText(logFile))
            {
                var payload = GetPayloadString(logMessage, length);
                w.WriteLine("{0}: {1}",
                            DateTime.Now.ToLongTimeString(),
                            payload);
            }
        }
        public void WritePayloadToLogFile(string trackerId, byte[] logMessage, int length, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder, trackerId);
            using (var w = File.AppendText(logFile))
            {
                var payload = GetPayloadString(logMessage, length);
                w.WriteLine("{0}: {1}",
                            DateTime.Now.ToLongTimeString(),
                            payload);
            }
        }
        public void WritePayloadToLogFile(string trackerId, string logMessage, LogType logType, bool writeToListenerFolder = false)
        {
            var logFile = GetLogFilePath(logType, writeToListenerFolder, trackerId);
            using (var w = File.AppendText(logFile))
            {
                w.WriteLine(logMessage);
            }
        }
        private string GetPayloadString(byte[] logMessage, int length)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                sb.AppendFormat("{0:x2} ", logMessage[i]);
            }

            return sb.ToString();
        }
        private string GetLogFilePath(LogType logType, bool writeToListenerFolder, string trackerId = null)
        {
            string prefix;
            var timestampWithTime = false;

            switch (logType)
            {
                case LogType.RawData:
                    prefix = "RawData";
                    break;
                case LogType.RawBinary:
                    prefix = "RawBinary";
                    break;
                case LogType.UsageStatistics:
                    prefix = "UsageStatistics";
                    timestampWithTime = true;
                    break;
                default:
                    prefix = string.Empty;
                    break;
            }
            if (!string.IsNullOrEmpty(trackerId))
            {
                prefix = string.Format("{0}-{1}", prefix, trackerId);
            }


            var ext = logType == LogType.RawBinary ? "dat" : "txt";
            var filename = string.Format("{0}-{1}-{2}.{3}",
                prefix,
                logSuffix,
                timestampWithTime
                    ? DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")
                    : DateTime.Now.Date.ToString("yyyy-MM-dd"),
                ext);
            //var filePath = string.Format("{0}{1}", LogPath, filename);
            var filePath = string.Format("{0}\\{1}", writeToListenerFolder ? m_ServiceLocation : LogPath, filename);
            if (!File.Exists(filePath))
            {
                var stream = File.Create(filePath);
                stream.Close();
            }

            return filePath;
        }
    }

    public enum LogType
    {
        RawData,
        RawBinary,
        UsageStatistics
    }
}
