using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading;
using ARGUS.EAS;


namespace ARGUS
{
    internal static class LogFile
    {
        private static readonly Mutex MutexLog = new Mutex();
        private static string _logFilePrefix;

        /// <summary>
        /// File: First Executig assembly path + name, if not ARGUS --> This assembly path + name
        /// </summary>

        public static string File
        {
            get
            {
                try
                {
                    if (_logFilePrefix == null)
                    {
                        // TODO: Choose Path

                        // http://stackoverflow.com/questions/269893/best-place-to-store-config-files-and-log-files-on-windows-for-my-program

                        //var logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        //var logDir = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "ARGUS");    // Adminstrator
                        //var logDir = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "ARGUS");
                        var logDir = Path.Combine(Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ARGUS"), "Log");
                        if (!Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);

                        _logFilePrefix = Path.Combine(logDir, Assembly.GetExecutingAssembly().GetName().Name);
                    }

                    var date = DateTime.Now;
                    return string.Format("{0}_{1:0000}{2:00}.log", _logFilePrefix, date.Year, date.Month);
                }
                catch (Exception exception)
                {
                    Trace.Write("Error getting Log Directory: " + exception.Message);
                    return null;
                }
            }
        }

        public static bool WriteException(Exception exception)
        {
            return WriteException(exception, null);
        }

        public static bool WriteException(Exception exception, string fileName)
        {
            // If no file specified, or Directory does not exist: use default
            if (string.IsNullOrEmpty(fileName) || !Directory.Exists(Path.GetDirectoryName(fileName)))
                fileName = File;


            if (fileName != null)
            {
                MutexLog.WaitOne();
                try
                {
                    bool newFile = false;
                    if (!System.IO.File.Exists(fileName))
                        newFile = true;

                    // Write Message
                    using (var streamWriter = new StreamWriter(fileName, true))
                    {
                        string message = exception.ToLogLines(newFile);
                        streamWriter.Write(message);
                        streamWriter.Close();
                    }
                    return true;
                }
                catch (Exception exception2)
                {
                    Trace.WriteLine(exception.ToLogLines(true));
                    Trace.WriteLine(exception2.ToLogLines(true));
                    return false;
                }
                finally
                {
                    MutexLog.ReleaseMutex();
                }
            }
            else
            {
                Trace.WriteLine(exception.ToLogLines(true));
                return false;
            }
        }

    }
}
