using ARGUS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace OGF.Service
{
    internal class CancelTransactionThread
    {
        private bool _stop = false;
        private Config _config = null;
        private NLog.Logger logger = null;

        internal CancelTransactionThread(Config config)
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
                while (!_stop)
                {
                    try
                    {
                        using (SqlConnection dbConnection = new SqlConnection())
                        {
                            dbConnection.ConnectionString = _config.DatabaseConnectionstring;
                            dbConnection.Open();

                            string sql = "";
                            sql  = "SELECT UniqueId, UniversignId FROM Transactions ";
                            sql += "WHERE CancelTransactionState = @State ";
                            sql += "AND CancelTransactionDate < @CurrentDate";

                            using (var dataSet = new DataSet())
                            {
                                using (SqlCommand sqlCommand = new SqlCommand())
                                {
                                    sqlCommand.Connection = dbConnection;
                                    sqlCommand.CommandText = sql;
                                    sqlCommand.Parameters.Clear();
                                    sqlCommand.Parameters.Add("@State", SqlDbType.Int).Value = (int)Common.CancelTransactionState.Waiting;
                                    sqlCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                                    {
                                        sqlDataAdapter.Fill(dataSet);
                                    }
                                }

                                if(dataSet.Tables[0].Rows.Count > 0)
                                {
                                    CancelTransaction cancelTransaction = new CancelTransaction(_config);
                                    foreach (DataRow row in dataSet.Tables[0].Rows)
                                        cancelTransaction.Process(dbConnection, row["UniqueId"].ToString(), row["UniversignId"].ToString());
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {                       
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
                        Logger.Log("Wait during pollingsInterval ...");
                        for(int i=0; i< 60 && !_stop; i++)
                            Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception exception)
            {
                IsRunning = false;
                                
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
                IsRunning = false;
            }
        }

        internal void Stop()
        {
            Logger.Log("Stopping...", LoggerStyles.HightLight);
            _stop = true;
        }

        internal bool IsRunning { get; private set; }
    }
}
