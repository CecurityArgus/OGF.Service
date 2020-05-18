using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using ARGUS.Diagnostics;

namespace OGF.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            int maxIndentLevel = 100;
            LoggerStyles logStyles = LoggerStyles.Error | LoggerStyles.HightLight | LoggerStyles.Normal | LoggerStyles.Warning;
            bool logFunc = false;
            bool logChilds = false;

            // Read Arguments
            string[] rest = args;
            try
            {
                rest = ReadLogArguments(args, ref maxIndentLevel, ref logStyles, ref logFunc, ref logChilds);
            }
            catch (Exception exception)
            {
                LogException("Syntax Error", exception);
                Usage();
                Environment.Exit(1);
            }

            // Create Service Instance
            if (Environment.UserInteractive)
            {
                // Console
                try
                {
                    if (args.Contains("/?") || args.Contains("-?"))
                    {
                        Usage();
                        Environment.Exit(2);
                    }

                    // Execute Service Arguments
                    var startServiceInGui = true;
                    var rest2 = ARGUS.Service.ServiceInstaller.ExecuteCommandLine(rest, () => new ProjectInstaller().serviceInstaller, Console.WriteLine);
                    if (rest2.Length != rest.Length)
                    {
                        rest = rest2;
                        startServiceInGui = false;
                    }

                    // Unhandled Arguments
                    if (rest.Length > 0)
                    {
                        LogException("Syntax Error", new Exception(string.Format("Unknown argument '{0}'.\n", rest[0])));
                        Usage();
                        Environment.Exit(1);
                    }

                    if (!startServiceInGui)
                        Environment.Exit(0);

                }
                catch (Exception exception)
                {
                    LogException(null, exception);
                    ExceptionHandler.Log(exception);
                    Environment.Exit(1);
                }

                try
                {
                    ARGUS.Controls.Console.HideConsole();

                   
                    var service = new MainService();

                    // Logger Window
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var form = new ThreadWindowLogger();
                    form.MaxIndentLevel = maxIndentLevel;
                    form.LogStyles = logStyles;
                    form.LogFunc = logFunc;
                    form.LogChilds = logChilds;
                    form.Tag = service;    // Added to stop at close
                    form.Closing += new CancelEventHandler(ConsolesLogForm_Stopping);
                    var assembly = Assembly.GetEntryAssembly().GetName();
                    form.Text = "OGF service v" + assembly.Version;
                    Logger.Loggers.Add(form);

                    // What to do on fatal exception
                    service.OnFatalException = (exception) => { service.LogAndStopService(exception, form); };

                    // Create Service Thread (Run GUI on Main thread and service in new thread to reduce UI Thread problems)                    
                    var thread = new Thread(() => { Thread.Sleep(500); StartService(service); });    // Delay so not logged while GUI not yet availabale
                    thread.Name = "OGF service"; // Added for Logger
                    thread.Start();

                    // Start
                    Application.Run(form);

                }
                catch (Exception exception)
                {
                    ExceptionHandler.Show(exception);
                }
            }
            else
            {
                try
                {

#if DEBUG
                    // Attach Log method
                    Logger.Loggers.Add(new TraceLogger() { LogThread = true, LogAssembly = true, LogTime = true });
#endif

                    // Run normally as service in release mode.
                    var service = new MainService();

                    // What to do on fatal exception
                    service.OnFatalException = (exception) => { service.LogAndStopService(exception, null); };

                    ServiceBase.Run(service);
                }
                catch (Exception exception)
                {
                    ExceptionHandler.Log(exception);
                    throw;
                }
            }
        }

        /// <summary>
        /// Read te Arguments for Logging and return the remaining arguments
        /// (/VERBOSE, /LOGSTYLES, /LOGFUNC, /LOGCHILDS)
        /// </summary>
        /// <param name="args"></param>
        /// <param name="maxIndentLevel"></param>
        /// <param name="logStyles"></param>
        /// <param name="logFunction"></param>
        /// <returns></returns>
        private static string[] ReadLogArguments(string[] args, ref int maxIndentLevel, ref LoggerStyles logStyles, ref bool logFunction, ref bool logChilds)
        {
            var rest = new List<string>();

            if (args == null || args.Length == 0)
                return new string[0];

            int argNbr = 0;
            while (argNbr < args.Length)
            {
                var argName = args[argNbr++];
                if (argName != null)
                {
                    switch (argName.ToUpper())
                    {
                        case "-LOGDEPTH":
                        case "/LOGDEPTH":
                            if (argNbr >= args.Length)
                                throw new Exception(string.Format("Missing value for argument '{0}'", argName));
                            var temp = Convert.ToInt32(args[argNbr++]);
                            if (temp < 0 || temp > 100)
                                throw new Exception(string.Format("Invalid value '{0}' for argument '{1}'. Value must be between 0 and 100.", temp, argName));
                            maxIndentLevel = temp;

                            break;

                        case "-LOGSTYLES":
                        case "/LOGSTYLES":
                            if (argNbr >= args.Length)
                                throw new Exception(string.Format("Missing value for argument '{0}'", argName));
                            logStyles = (LoggerStyles)Enum.Parse(typeof(LoggerStyles), args[argNbr++]);
                            break;

                        case "-LOGFUNC":
                        case "/LOGFUNC":
                            logFunction = true;
                            break;

                        case "-LOGCHILDS":
                        case "/LOGCHILDS":
                            logChilds = true;
                            break;

                        default:
                            rest.Add(argName);
                            break;
                    }
                }
            }
            return rest.ToArray();
        }

        private static void Usage()
        {

            var appName = Path.GetFileName(Assembly.GetCallingAssembly().Location);

            var oriColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("SYNTAX:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Format("  " + appName + " [/?] [/install] [/uninstall] [/logchilds] [/logstyles NBR]"));
                Console.WriteLine("");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("ARGUMENTS:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("  /?:\t\tSyntax help");
                Console.WriteLine("  /install:\tInstall the service");
                Console.WriteLine("  /start:\tStart the service");
                Console.WriteLine("  /stop:\tStop the service");
                Console.WriteLine("  /uninstall:\tUninstall the service");
                Console.WriteLine("  /send:\tSend a command to the service");
                Console.WriteLine("        \t  128: No Trace logging");
                Console.WriteLine("        \t  129: Minimum Trace logging");
                Console.WriteLine("        \t  130: Medium Trace logging");
                Console.WriteLine("        \t  131: Maximum Trace logging");
                Console.WriteLine("  /logchilds:\tLog child DLL's in interactive mode");
                Console.WriteLine("  /logstyles:\tSum of message kinds to log in interactive mode (Default: 27)");
                foreach (var style in Enum.GetNames(typeof(LoggerStyles)))
                {
                    var nbr = (int)Enum.Parse(typeof(LoggerStyles), style);
                    Console.WriteLine(string.Format("             \t  {0}: {1}", nbr, style));
                }
                Console.WriteLine("");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("EXAMPLE:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("  " + appName + " /logstyles 63 /logchilds");
            }
            finally
            {
                Console.ForegroundColor = oriColor;
            }
        }

        private static void LogException(string title, Exception exception)
        {
            var oriColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                if (!string.IsNullOrEmpty(title))
                    Console.Error.WriteLine(title);

                var innerException = exception;
                while (innerException != null)
                {
                    Console.Error.WriteLine(string.Format("- {0}", innerException.Message));
                    innerException = innerException.InnerException;
                }
            }
            finally
            {
                Console.ForegroundColor = oriColor;
            }
        }

        private static void StartService(ServiceBase service)
        {
            try
            {
                // Can be started without being installed
                Type type = service.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo method = type.GetMethod("OnStart", flags);
                method.Invoke(service, new object[] { null });
            }
            catch (Exception exception)
            {
                ExceptionHandler.Log(exception);
                Logger.Log(exception);

                if (!Environment.UserInteractive)
                    throw;
            }
        }
        
        private static void ConsolesLogForm_Stopping(object sender, CancelEventArgs e)
        {
            try
            {
                var service = (MainService)(((Form)sender).Tag);

                // Stop, Do via reflection (kweet niet meer waarom)
                var type = service.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var method = type.GetMethod("OnStop", flags);
                method.Invoke(service, null);

                System.Threading.Thread.Sleep(1000); // Wait so last message is still visible for 1 sec.}
            }
            catch (Exception exception)
            {
                // Application must be configured as Console Application!
                if (exception is TargetInvocationException)
                    exception = exception.InnerException;

                //Capture Exception
                ExceptionHandler.Log(exception);
                ExceptionHandler.Show(exception);
            }
        }
    }

}
