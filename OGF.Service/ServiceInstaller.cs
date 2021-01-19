using System;
using System.Collections;
using System.Configuration.Install;
using System.Globalization;
using System.ServiceProcess;
using System.Collections.Generic;

namespace ARGUS.Service
{

    // http://stackoverflow.com/questions/1195478/how-to-make-a-net-windows-service-start-right-after-the-installation


    internal static class ServiceInstaller
    {
        public static bool IsInstalled(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {
            using (var controller = new ServiceController(serviceInstaller.DisplayName))
            {
                try
                {
                    var status = controller.Status;                    
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        
        private static AssemblyInstaller GetInstaller()
        {
            var installer = new AssemblyInstaller(System.Reflection.Assembly.GetAssembly(typeof(ServiceInstaller)), null);
            installer.UseNewContext = true;
            return installer;
        }

        public static bool IsRunning(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {
            using (var controller = new ServiceController(serviceInstaller.DisplayName))
            {
                if (!IsInstalled(serviceInstaller)) return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        public static void InstallService(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {
            using (var installer = GetInstaller())
            {
                var state = new Hashtable();
                try
                {
                    installer.Install(state);
                    installer.Commit(state);
                    return;
                }
                catch 
                {                    
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch { }
                    throw;
                }
            }        
        }
        
        public static bool StartService(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {            
            using (var controller = new ServiceController(serviceInstaller.DisplayName))
            {
                if (controller.Status != ServiceControllerStatus.Running)
                {                    
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    return true;
                }
                return false;
            }
        }

        public static bool StopService(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {            
            using (var controller = new ServiceController(serviceInstaller.DisplayName))
            {
                if (controller.Status != ServiceControllerStatus.Stopped)
                {
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    return true;
                }
                return false;
            }
        }
       
        public static void UninstallService(this System.ServiceProcess.ServiceInstaller serviceInstaller)
        {            
            using (var installer = GetInstaller())
            {
                var state = new Hashtable();
                installer.Uninstall(state);
                return;
            }

        }

        public static bool SendCommand(this System.ServiceProcess.ServiceInstaller serviceInstaller, int sendCommand)
        {
            using (var controller = new ServiceController(serviceInstaller.DisplayName))
            {
                if (controller.Status != ServiceControllerStatus.Running)
                    throw new Exception("Service is not running");

                if (sendCommand < 128 || sendCommand > 256)
                    throw new Exception("Value must be >=128 and <=256");

                controller.ExecuteCommand(sendCommand);
                return false;
            }
        }
        
        public static string[] ReadCommandLineArguments(string[] args, out Dictionary<string, object> arguments)
        {
            // Used Dictionary so i can add and execute arguments in same order
            arguments = new Dictionary<string, object>();
            
            var rest = new List<string>();
            int argNbr = 0;
            while (argNbr < args.Length)
            {
                var argName = args[argNbr++];
                if (argName != null)
                {
                    if (argName.StartsWith("-") || argName.StartsWith("/"))
                    {
                        switch (argName.Substring(1).ToUpperInvariant())
                        {
                            case "INSTALL":
                                arguments.Add(argName.Substring(1).ToUpperInvariant(), true);
                                break;

                            case "UNINSTALL":
                                arguments.Add(argName.Substring(1).ToUpperInvariant(), true);
                                break;

                            case "START":
                                arguments.Add(argName.Substring(1).ToUpperInvariant(), true);
                                break;

                            case "STOP":
                                arguments.Add(argName.Substring(1).ToUpperInvariant(), true);
                                break;

                            case "SEND":
                                if (argNbr >= args.Length)
                                    throw new Exception(string.Format("Missing value for argument '{0}'", argName));

                                int sendCommand;
                                if (!Int32.TryParse(args[argNbr++], NumberStyles.Integer, CultureInfo.InvariantCulture, out sendCommand))
                                    throw new Exception(string.Format("Invalid number for argument '{0}'", argName));

                                arguments.Add(argName.Substring(1).ToUpperInvariant(), sendCommand);                            
                                break;

                            default:
                                rest.Add(argName);
                                break;
                        }
                    }
                    else
                        rest.Add(argName);
                }
            }
            return rest.ToArray();

        }

        private static void Log(Action<string> logCallback, string message)
        {
            if (logCallback != null)
                logCallback(message);
        }

        public static string[] ExecuteCommandLine(string[] args, Func<System.ServiceProcess.ServiceInstaller> getServiceInstaller, Action<string> logCallback)
        {

            Dictionary<string, object> arguments;
            var rest = ReadCommandLineArguments(args, out arguments);

            if (getServiceInstaller == null)
                throw new ArgumentNullException("getServiceInstaller");

            foreach (var argument in arguments)
            {
                System.ServiceProcess.ServiceInstaller serviceInstaller;
                switch (argument.Key.ToUpperInvariant())
                {
                    case "INSTALL":

                        serviceInstaller = getServiceInstaller.Invoke();
                        Log(logCallback, string.Format("Installing Service: '{0}'", serviceInstaller.DisplayName));
                        if (IsInstalled(serviceInstaller))
                            Log(logCallback, "Service is already installed");
                        else
                        {
                            InstallService(serviceInstaller);
                            Log(logCallback, "Installed successful");
                        }
                        break;

                    case "UNINSTALL":
                        serviceInstaller = getServiceInstaller.Invoke();
                        Log(logCallback, string.Format("Uninstalling Service: '{0}'", serviceInstaller.DisplayName));

                        if (!IsInstalled(serviceInstaller))
                            Log(logCallback, string.Format("Service already uninstalled"));
                        else
                        {
                            UninstallService(serviceInstaller);
                            Log(logCallback, "Uninstalled successful");
                        }
                        break;

                    case "START":
                        serviceInstaller = getServiceInstaller.Invoke();

                        Log(logCallback, string.Format("Starting Service: '{0}'", serviceInstaller.DisplayName));

                        if (!IsInstalled(serviceInstaller))
                            throw new Exception("Service not installed");

                        if (StartService(serviceInstaller))
                            Log(logCallback, "Service started");
                        else
                            Log(logCallback, "Service already started");
                        break;

                    case "STOP":
                        serviceInstaller = getServiceInstaller.Invoke();
                        Log(logCallback, string.Format("Stopping Service: '{0}'", serviceInstaller.DisplayName));

                        if (!IsInstalled(serviceInstaller))
                            Log(logCallback, "Service is not installed");
                        else
                        {
                            if (StopService(serviceInstaller))
                                Log(logCallback, "Service stopped");
                            else
                                Log(logCallback, string.Format("Service already stopped"));
                        }
                        break;

                    case "SEND":
                        serviceInstaller = getServiceInstaller.Invoke();
                        Log(logCallback, string.Format("Sending command '{1}' to Service: '{0}'", serviceInstaller.DisplayName, argument.Value));

                        if (!IsInstalled(serviceInstaller))
                            throw new Exception("Service not installed");

                        SendCommand(serviceInstaller, (int)argument.Value);
                        Log(logCallback, "Command send successful");
                        break;
                }
            }

            return rest;

        }

    }
}
