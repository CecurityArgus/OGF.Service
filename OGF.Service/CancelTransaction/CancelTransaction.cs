using ARGUS.ADMS;
using ARGUS.ADMS.Common;
using ARGUS.ADMS.Query;
using ARGUS.ADMS.Security;
using ARGUS.EAS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
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
                                query.IndexFilters.Add(new IndexFilter { Name = "UniqueId", Filter = uniqueId });                   //0
                                query.IndexFilters.Add(new IndexFilter { Name = "UniversignState", Filter = "Pending" });           //1
                                query.IndexFilters.Add(new IndexFilter { Name = "Signed", Filter = "False" });                      //2
                                query.IndexFilters.Add(new IndexFilter { Name = "DocID" });                                         //3
                                query.IndexFilters.Add(new IndexFilter { Name = "DocType" });                                       //4
                                query.IndexFilters.Add(new IndexFilter { Name = "EventID" });                                       //5

                                var records = domainQuery.DocumentTypes[_config.DocumenttypeName].Query.ExecuteQuery(query).ToList();

                                if (records.Count == 1)
                                {
                                    List<NameValuePair> values = new List<NameValuePair>();
                                    values.Add(new ARGUS.ADMS.Common.NameValuePair { Name = "UniversignState", Value = "Expired" });

                                    domainQuery.DocumentTypes[_config.DocumenttypeName].Records.UpdateRecord(records[0].RecordRef, values);
                                                                       
                                    string docId = records[0].Values[3];
                                    string docType = records[0].Values[4];
                                    string eventID = records[0].Values[5];
                                    UpdateOGF(_config.DatabaseConnectionstring, _config.OGF_SendRequests, _config.OGF_Webservice, _config.OGF_PassPhrase, docType, uniqueId, docId, eventID, "Expired");
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

        internal  void UpdateOGF(string connectionstringOGF, bool sendRequest, string baseUrl, string passPhrase, string docType, string uniqueId, string docId, string eventId, string state)
        {
            if (sendRequest)
            {
                //To do -> error -> Send mail -> message queue cfr Ludo
                logger.Trace($"Update OGF - DocType = {docType} - UniqueId = {uniqueId} - DocID = {docId} - EventId = {eventId} - State = {state}  ");

                string CRC = "";
                string result = "";

                try
                {
                    CRC = Checksum.CreateCheckSum(passPhrase, docType, uniqueId, docId, eventId);

                    using (HttpClient httpClient = new HttpClient())
                    {
                        StatutDocument statutDocument = new StatutDocument
                        {
                            EVENT_ID = Convert.ToInt32(eventId),
                            ID_DOC_CECURITY = uniqueId,
                            ID_DOC_OA = Convert.ToInt32(docId),
                            STATUT_DOC = state,
                            TYPE_DOC = docType
                        };

                        TransferStatutDocument transferStatutDocument = new TransferStatutDocument()
                        {
                            StatutDocument = statutDocument,
                            Checksum = Checksum.CreateCheckSum(passPhrase, docType, uniqueId, docId, eventId)
                        };

                        using (var content = new StringContent(JsonConvert.SerializeObject(transferStatutDocument), System.Text.Encoding.UTF8, "application/json"))
                        {
                            HttpResponseMessage httpResponseMessage = httpClient.PutAsync(baseUrl, content).Result;

                            result = Convert.ToInt32(httpResponseMessage.StatusCode).ToString();
                        }
                    }
                }
                finally
                {
                    AddAudit(connectionstringOGF, docType, uniqueId, docId, eventId, CRC, state, result);
                }
            }
        }

        internal  void AddAudit(string connectionstringOGF, string docType, string uniqueId, string docId, string eventId, string CRC, string state, string result)
        {
            try
            {

                string sql = "";
                sql = "INSERT INTO SendToOGF(SendDate, Doctype, DocId, EventId, UniqueId, CRC, State, Result)";
                sql += "VALUES(@SendDate, @Doctype, @DocId, @EventId, @UniqueId, @CRC, @State, @Result)";

                using (SqlConnection sqlConnection = new SqlConnection())
                {
                    sqlConnection.ConnectionString = connectionstringOGF;
                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandText = sql;

                        sqlCommand.Parameters.Clear();
                        sqlCommand.Parameters.Add("@SendDate", SqlDbType.DateTime).Value = DateTime.Now;
                        sqlCommand.Parameters.Add("@Doctype", SqlDbType.NVarChar).Value = docType;
                        sqlCommand.Parameters.Add("@DocId", SqlDbType.NVarChar).Value = docId;
                        sqlCommand.Parameters.Add("@EventId", SqlDbType.NVarChar).Value = eventId;
                        sqlCommand.Parameters.Add("@UniqueId", SqlDbType.NVarChar).Value = uniqueId;
                        sqlCommand.Parameters.Add("@CRC", SqlDbType.NVarChar).Value = CRC;
                        sqlCommand.Parameters.Add("@State", SqlDbType.NVarChar).Value = state;
                        sqlCommand.Parameters.Add("@Result", SqlDbType.NVarChar).Value = result;

                        sqlCommand.ExecuteNonQuery();

                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                Exception innerException = ex.InnerException;

                while (innerException != null)
                {
                    message += innerException.Message;

                    innerException = innerException.InnerException;
                }

                logger.Error(string.Format("Send data to OGF webservice failed.({0};{1};{2};{3};{4}). Reason: {5}", docType, docId, eventId, CRC, state, message));
            }
        }
    }

    internal class StatutDocument
    {
        public string TYPE_DOC { get; set; }
        public string ID_DOC_CECURITY { get; set; }
        public int ID_DOC_OA { get; set; }
        public string STATUT_DOC { get; set; }
        public int EVENT_ID { get; set; }
    }

    internal class TransferStatutDocument
    {
        public StatutDocument StatutDocument { get; set; }
        public string Checksum { get; set; }

    }
}
