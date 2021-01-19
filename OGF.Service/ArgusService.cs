using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ARGUS.Diagnostics;
using ARGUS.FrameWork.Exceptions;


namespace OGF.Service
{
    public class ArgusService : ServiceBase
    {

        public ArgusService()
        {
            // LogUnhandled Exception
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                var exception = new Exception("Unexpected service thread exception", eventArgs.ExceptionObject as Exception);
                if (OnFatalException != null)
                    OnFatalException.Invoke(exception);
            };
        }               
        
        private static Mutex _mutex;
        public static bool IsOnlyInstance()
        {

            // Get Application GUID
            string appGuid = "";
            object[] objects = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
            if (objects.Length > 0)
                appGuid = ((System.Runtime.InteropServices.GuidAttribute)objects[0]).Value;


            // Prevent multiple instances
            try
            {
                // Raises exception
                _mutex = new Mutex(false, "Global\\" + appGuid);

                if (!_mutex.WaitOne(0, false))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }


        public static void WaitForDatabase(Func<bool> stopRequested)
        {
            WaitForDatabase(stopRequested, ARGUS.EAS.Eas.DefaultConnectionString, 10, 30);
        }

        public static void WaitForDatabase(Func<bool> stopRequested, string connectionString)
        {
            WaitForDatabase(stopRequested, connectionString, 10, 30);
        }

        public static void WaitForDatabase(Func<bool> stopRequested, string connectionString, int retries, int waitInSeconds)
        {

            using (Logger.LogAndIndent("Waiting for database connection..."))
            {
                // Wait until DB Ready
                var retry = 0;
                var dbReady = false;
                DateTime startTime = DateTime.MinValue;

                while (!dbReady && !stopRequested())
                {                    
                    try
                    {
                        using (Logger.LogAndIndent("Checking database connection..."))
                        {
                            startTime = DateTime.Now;
                            using (var connection = new SqlConnection(connectionString))
                            {
                                connection.Open();

                                using (var command = connection.CreateCommand())
                                {
                                    var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                                    command.CommandText = string.Format("SELECT state_desc FROM sys.databases WHERE name = '{0}'", connectionStringBuilder.InitialCatalog);
                                    var result = command.ExecuteScalar();
                                    if (result != null && result.ToString() == "ONLINE")
                                    {
                                        dbReady = true;
                                        Logger.Log("Connection and database state are OK");
                                    }                                        
                                    else
                                        Logger.Log(() => string.Format("Connection OK, database state is not ONLINE: '{0}'", result));

                                }
                                dbReady = true;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        retry++; 
                        
                        Logger.Log(exception);
                                                                               
                        if (retry <= retries)
                        {
                            Logger.Log(() => string.Format("Retrying {0}/{1} in {2} seconds...", retry, retries, Math.Round((startTime.AddSeconds(waitInSeconds) - DateTime.Now).TotalSeconds)));
                            while (DateTime.Now < startTime.AddSeconds(waitInSeconds) && !stopRequested.Invoke())
                                Thread.Sleep(1000);
                            
                        }
                        else
                        {
                            throw new Exception(string.Format("Error connecting to database (tried {0} times).", retry), exception);
                        }
                    }
                
                }
            }
        }

        public static void CheckEASConnection(Func<bool> stopRequested)
        {
            try
            {
                using (Logger.LogAndIndent("Checking EAS connection..."))
                {
                    WaitForDatabase(stopRequested); // use EAS Default connectionstring
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Opening EAS connection failed.", exception);
            }
        }


        #region Update Custom Commands
        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);

            try
            {
                if (command == (int)CustomCommands.NoLogging)
                {
                    Logger.Log("Trace logging stopped.");
                    var traceLogger = Logger.Loggers.OfType<TraceLogger>().FirstOrDefault();
                    if (traceLogger != null)
                        Logger.Loggers.Remove(traceLogger);
                }

                else if (command == (int)CustomCommands.MinimumLogging)
                {
                    var traceLogger = Logger.Loggers.OfType<TraceLogger>().FirstOrDefault();
                    if (traceLogger == null)
                    {
                        traceLogger = new TraceLogger();
                        Logger.Loggers.Add(traceLogger);
                    }
                    traceLogger.LogAssembly = true;
                    traceLogger.LogFunc = false;
                    traceLogger.LogThread = false;
                    traceLogger.LogTime = true;
                    traceLogger.LogDuration = false;
                    traceLogger.LogStyles = LoggerStyles.Error | LoggerStyles.HightLight;
                    Logger.Log("Minimum Trace logging started.", LoggerStyles.HightLight);
                }

                else if (command == (int)CustomCommands.MediumLogging)
                {
                    var traceLogger = Logger.Loggers.OfType<TraceLogger>().FirstOrDefault();
                    if (traceLogger == null)
                    {
                        traceLogger = new TraceLogger();
                        Logger.Loggers.Add(traceLogger);
                    }
                    traceLogger.LogAssembly = true;
                    traceLogger.LogFunc = false;
                    traceLogger.LogThread = false;
                    traceLogger.LogTime = true;
                    traceLogger.LogDuration = false;
                    traceLogger.LogStyles = LoggerStyles.Normal | LoggerStyles.Warning | LoggerStyles.Error | LoggerStyles.HightLight;
                    Logger.Log("Medium Trace logging started.");
                }

                else if (command == (int)CustomCommands.MaximumLogging)
                {
                    var traceLogger = Logger.Loggers.OfType<TraceLogger>().FirstOrDefault();
                    if (traceLogger == null)
                    {
                        traceLogger = new TraceLogger();
                        Logger.Loggers.Add(traceLogger);
                    }
                    traceLogger.LogAssembly = true;
                    traceLogger.LogFunc = false;
                    traceLogger.LogThread = false;
                    traceLogger.LogTime = true;
                    traceLogger.LogDuration = false;
                    traceLogger.LogStyles = (LoggerStyles)1023;
                    Logger.Log("Maximum Trace logging started.");
                }
            }
            catch (Exception exception)
            {
                exception = new Exception(string.Format("Error processing service custom command '{0}'", command), exception);
                ExceptionHandler.Log(exception);
            }
        }

        public enum CustomCommands
        {
            // 128 -> 255
            NoLogging = 128,
            MinimumLogging = 129,
            MediumLogging = 130,
            MaximumLogging = 131
        }
        #endregion

               
        /// <summary>
        /// Start Thread for Service with exception handling (Set OnFatalException on service)
        /// </summary>
        /// <param name="argusServiceThread"></param>
        /// <returns></returns>
        internal Thread StartNewServiceThread(IArgusServiceThread argusServiceThread)
        {
            if (argusServiceThread == null)
                return null;
            
            var thread = new Thread(() => {               
                try
                {                    
                    argusServiceThread.Start();                    
                }
                catch (Exception exception)
                {
                    // Handle Exception
                    if (OnFatalException != null)
                        OnFatalException.Invoke(exception);
                    // Don't retrow -> GPF
                }                
            });

            thread.Start();
            return thread;
        }

        /*
        public event EventHandler Stopped;

        internal static Thread StartInThread(IEnumerable<IArgusServiceThread> argusServiceThreads)
        {
            if (argusServiceThreads == null)
                return null;

            foreach (var serviceThread in argusServiceThreads)
                StartInThread(serviceThread);

        }
        
        protected override void OnStop()
        {
            base.OnStop();

            if (Stopped != null)
                Stopped(this, EventArgs.Empty);
        }*/


        #region Error Handling


        /// <summary>
        /// Code to exceute on Fatal Exception (Log Event and Stop Service: ArgusService.StopService);
        /// </summary>
        internal Action<Exception> OnFatalException { get; set; }

        /// <summary>
        /// Default Method to logging and stopping the Service
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="form"></param>
        internal void LogAndStopService(Exception exception, Form form)
        {
            
            // Stop in new thread, reason else waiting in same thread as code that must stop the service
            new Thread(() =>
            {
                try
                {
                    // Log Event if it is running as a Service
                    if (form == null)
                    {
                        using (Logger.LogAndIndent("Critical Service Error: Log Entry in EventViewer"))
                        {
                            try
                            {
                                var message = "";
                                if (exception != null)
                                {
                                    // Get Messages
                                    message = string.Join(Environment.NewLine, exception.GetExceptions().Select(ex => "- " + ex.Message).ToArray());

                                    // Add Details Information                                    
                                    foreach (var ex in exception.GetExceptions())
                                    {
                                        message += Environment.NewLine + Environment.NewLine;
                                        message += ex.GetType().Name + ": " + ex.Message;

                                        var argusException = ex as ArgusException;
                                        if (argusException != null)
                                            message += string.Format(" (Code: {0}, Ref: {1})", argusException.Code, argusException.GetHashCode());
                                        else
                                            message += string.Format(" (Ref: {0})", ex.GetHashCode());                                        
                                        
                                        message += Environment.NewLine + ex.StackTrace;
                                    }
                                    
                                }
                                var serviceEventLog = new EventLog("Application", ".", ServiceName);
                                serviceEventLog.WriteEntry(message, EventLogEntryType.Error, 1);
                            }
                            catch (Exception exception2)
                            {
                                // Exception if to much items in Log Book
                                Logger.Log(exception); 
                                ExceptionHandler.Log(exception2);
                            }
                        }
                    }

                    // Stop Service                
                    Stop();
                }
                catch (Exception exception2)
                {
                    // don't throw exception, else UnhandledException will cause infinitive loop?
                    Logger.Log(exception);
                    ExceptionHandler.Log(exception2);
                }

            }).Start();
        }

        #endregion
    }
    
    internal interface IArgusServiceThread
    {
        void Start();
        //void Stop();
        //bool IsRunning { get;} 
    }



    
}
