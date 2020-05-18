using ARGUS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OGF.Service
{
    internal class OGFServer
    {
        private bool _stop;
        private SendReminderThread _sendReminderThread = null;
        private CancelTransactionThread _cancelTransactionThread = null;
        private Config _config;
        private NLog.Logger logger = null;

        internal OGFServer(Config config)
        {
            _stop = false;
            _config = config;
            logger = NLog.LogManager.GetLogger(config.Name);
        }

        internal void Start()
        {
            try
            {
                IsRunning = true;

                //start reminder thread
                Logger.Log("Starting sendreminder thread... ");
                logger.Trace("Starting sendreminder thread... "); 
                _sendReminderThread = new SendReminderThread(_config);
                ThreadStart threadStartSendReminder = new ThreadStart(_sendReminderThread.Start);
                Thread sendReminderWorkerThread = new Thread(threadStartSendReminder);
                sendReminderWorkerThread.Name = _config.Name + " - SendReminders";
                sendReminderWorkerThread.Start();

                Logger.Log("Starting canceltransaction thread...");
                logger.Trace("Starting canceltransaction thread...");
                _cancelTransactionThread = new CancelTransactionThread(_config);
                ThreadStart threadStartCancelTransaction = new ThreadStart(_cancelTransactionThread.Start);
                Thread cancelTransactionWorkerThread = new Thread(threadStartCancelTransaction);
                cancelTransactionWorkerThread.Name = _config.Name + " - CancelTransactions";
                cancelTransactionWorkerThread.Start();

                while (!_stop)
                {
                    Thread.Sleep(1000);

                    if (!_sendReminderThread.IsRunning || !_cancelTransactionThread.IsRunning)
                        _stop = true;
                }
            }
            catch(Exception exception)
            {

                // Error Starting
                Logger.Log(exception);
                ExceptionHandler.Log(exception);

                string errorMessage = exception.Message;
                Exception innerException = exception.InnerException;

                while (innerException != null)
                {
                    errorMessage += innerException.Message;
                    innerException = innerException.InnerException;
                }

                logger.Error(errorMessage);

            }
            finally
            {
                //stop all threads
                _sendReminderThread.Stop();

                _cancelTransactionThread.Stop();

                //wait until all threads are stopped
                if (_sendReminderThread.IsRunning || _cancelTransactionThread.IsRunning)
                    Thread.Sleep(1000);

                IsRunning = false;
            }
        }

        internal void Stop()
        {
            if (_sendReminderThread != null)
                _sendReminderThread.Stop();

            if (_cancelTransactionThread != null)
                _cancelTransactionThread.Stop();

            Logger.Log("Stopping...", LoggerStyles.HightLight);
        }

        internal bool IsRunning { get; private set; }
    }
}
