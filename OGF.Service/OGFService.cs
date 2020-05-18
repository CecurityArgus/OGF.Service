using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Collections;
using System.Data.SqlClient;
using System.Globalization;
using System.Data;
using System.Configuration;
using ARGUS.Diagnostics;
using Newtonsoft.Json;

namespace OGF.Service
{
    public class OGFService : IArgusServiceThread
    {
        private bool _stop;
        private List<OGFServer> _ogfServers = null;
     
        internal OGFService()
        {
            _ogfServers = new List<OGFServer>();
        }

        #region IArgusServiceThread Members

        public void Start()
        {
            List<Config> configs= null;

            try
            {
                ReadSettings(out configs);

                IsRunning = true;

                //check EAS connection
                ArgusService.CheckEASConnection(() => _stop);
                
                foreach(var config in configs)
                {
                    var logger = NLog.LogManager.GetLogger(config.Name);
                    logger.Trace(string.Format("Starting {0} - OGF server", config.Name));
                    ARGUS.Diagnostics.Logger.Log(string.Format("Starting {0} - OGF server", config.Name)); 

                    OGFServer ogfServer = new OGFServer(config);
                    ThreadStart threadStart = new ThreadStart(ogfServer.Start);
                    Thread ogfServerWorkerThread = new Thread(threadStart);
                    ogfServerWorkerThread.Name = config.Name + " - OGF server";
                    ogfServerWorkerThread.Start();
                    _ogfServers.Add(ogfServer);
                }

                //check if all docflow server running
                while (!_stop)
                {
                    Thread.Sleep(1000);
                    foreach (OGFServer ogfServer in _ogfServers)
                    {
                        if (!ogfServer.IsRunning)
                            _stop = true;
                    }
                }
            }
            catch (Exception exception)
            {
                IsRunning = false;

                // Error Starting
                Logger.Log(exception);
                ExceptionHandler.Log(exception);

            }
            finally
            {
                //stop all docflow server
                foreach (OGFServer ogfServer in  _ogfServers)
                        ogfServer.Stop();

                //wait until all threads are stopped
                while (_ogfServers.Count > 0)
                {
                    if (_ogfServers[0].IsRunning)
                        Thread.Sleep(1000);
                    else
                        _ogfServers.RemoveAt(0);
                }

                IsRunning = false;
            }
        }

        private void ReadSettings(out List<Config> configs)
        {
            configs = null;
            
            try
            {
                string settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
                string json = File.ReadAllText(settingsFile);
                configs = JsonConvert.DeserializeObject<List<Config>>(json); 
            }
            catch(Exception ex)
            {
                throw new Exception("Reading settings file failed.", ex);
            }
        }

        #endregion

        internal void Stop()
        {
            Logger.Log("Stopping OGF Service...", LoggerStyles.HightLight);
            _stop = true;
        }

        internal bool IsRunning { get; private set; }
    }

   
}
