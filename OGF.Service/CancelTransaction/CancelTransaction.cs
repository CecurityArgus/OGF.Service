using ARGUS.ADMS;
using ARGUS.ADMS.Common;
using ARGUS.ADMS.Query;
using ARGUS.ADMS.Security;
using ARGUS.EAS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace OGF.Service
{
    internal class CancelTransaction
    {
        internal Config _config = null;
        private  NLog.Logger logger = null;
        
        internal CancelTransaction(Config config)
        {
            _config = config;
            logger = NLog.LogManager.GetLogger(config.Name);
        }

        internal void Process(SqlConnection connection, string uniqueId, string universignId)
        {
            bool excecute = false;
            int retry = 0;

            while (!excecute)
            {
                try
                {
                    logger.Trace("Cancel transaction - Start");
                    logger.Trace("Cancel transaction - UniqueId: " + uniqueId);
                    logger.Trace("Cancel transaction - UniversignId: " + universignId);
                    ARGUS.Diagnostics.Logger.Log("UniqueId: " + uniqueId);
                    ARGUS.Diagnostics.Logger.Log("UniversignId: " + universignId);

                    //Cancel transaction universign
                    logger.Trace("Cancel transaction Universign");
                    ARGUS.Diagnostics.Logger.Log("Cancel transaction Universign");
                    UniversignRequests reqs = new UniversignRequests(_config.UniversignEmail, _config.UniversignPassword);
                    reqs.CencelTransaction(_config, universignId);
                   
                    //Set state to finish in OGF database
                    SetState(connection, uniqueId, Common.CancelTransactionState.Finished);

                    //Update Archive
                    logger.Trace("Cancel transaction :  Update UniversignState in archive to 'Expired'");
                    ARGUS.Diagnostics.Logger.Log("Update UniversignState in archive to 'Expired'");
                    IPrincipal saveCurrentPrincipal = Thread.CurrentPrincipal;
                    try
                    {
                        using (Eas eas = Eas.CreateInstance())
                        {
                            var domain = eas.Domains[_config.DomainName];

                            var issuers = new ARGUS.ADMS.Issuers(domain);

                            ArgusPrincipal argusPrincipal = issuers.BuiltIn.ValidateUser(_config.UserName, _config.Password);
                            Thread.CurrentPrincipal = argusPrincipal;

                            using (DomainQuery domainQuery = DomainQuery.CreateInstance(domain))
                            {
                                Query query = new Query();
                                query.IndexFilters = new List<IndexFilter>();
                                query.IndexFilters.Add(new IndexFilter { Name = "UniqueId", Filter = uniqueId });
                                query.IndexFilters.Add(new IndexFilter { Name = "UniversignState", Filter = "Pending" });
                                query.IndexFilters.Add(new IndexFilter { Name = "Signed", Filter = "False" });

                                var records = domainQuery.DocumentTypes[_config.DocumenttypeName].Query.ExecuteQuery(query).ToList();

                                if (records.Count == 1)
                                {
                                    List<NameValuePair> values = new List<NameValuePair>();
                                    values.Add(new ARGUS.ADMS.Common.NameValuePair { Name = "UniversignState", Value = "Expired" });

                                    domainQuery.DocumentTypes[_config.DocumenttypeName].Records.UpdateRecord(records[0].RecordRef, values);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Thread.CurrentPrincipal = saveCurrentPrincipal;
                    }

                    //send mail ??

                    logger.Trace("Cancel transaction  - End");

                    excecute = true;

                }
                catch(Exception exception)
                {
                    
                    ARGUS.Diagnostics.Logger.Log(exception);
                    ExceptionHandler.Log(exception);

                    string errorMessage = exception.Message;
                    Exception innerException = exception.InnerException;

                    while (innerException != null)
                    {
                        errorMessage += innerException.Message;
                        innerException = innerException.InnerException;
                    }

                    logger.Error(errorMessage);

                    if (retry > 3)
                    {

                        SetState(connection, uniqueId, Common.CancelTransactionState.Error);
                        throw;
                    }

                    Thread.Sleep(1000);
                }
                finally
                {
                    retry++; 
                }
            }
        }

        private  void SetState(SqlConnection connection, string uniqueId, Common.CancelTransactionState state)
        {
            //Update State OGF database
            logger.Trace("Cancel transaction :  Update CancelTransactionState to " + state.ToString());
            ARGUS.Diagnostics.Logger.Log("Update CancelTransactionState to " + state.ToString());

            
            string sql = "UPDATE Transactions SET CancelTransactionState = @State WHERE UniqueId = @UniqueId";
            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = connection;
                sqlCommand.CommandText = sql;

                sqlCommand.Parameters.Clear();
                sqlCommand.Parameters.Add("@State", SqlDbType.Int).Value = (int)state;
                sqlCommand.Parameters.Add("@UniqueId", SqlDbType.NVarChar).Value = uniqueId;

                sqlCommand.ExecuteNonQuery();

            }

        }
    }
}
