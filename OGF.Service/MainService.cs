using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;


namespace OGF.Service
{
    public partial class MainService : ArgusService
    {
        private OGFService  _ogfService;
        private Thread _ogfServiceThread;

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Check multiple instances
                if (!IsOnlyInstance())
                    throw new Exception("Another instance is already running.");

                // Start Service
                _ogfService = new OGFService();
                _ogfServiceThread = StartNewServiceThread(_ogfService);

            }
            catch (Exception exception)
            {
                ExceptionHandler.Log(exception);
                throw;
            }
        }

        protected override void OnStop()
        {
             try
            {
                if (_ogfService != null)    // When service couldn't be started (Already running)
                {
                    _ogfService.Stop();

                    // Wait until closed
                    while (_ogfService.IsRunning)
                    {
                        System.Windows.Forms.Application.DoEvents(); // Needed, else GUI blocks when logging
                        Thread.Sleep(20);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionHandler.Log(exception);
                throw;
            }
        }
        
    }
}
