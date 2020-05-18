using ARGUS.ADMS;
using ARGUS.ADMS.Query;
using ARGUS.ADMS.Security;
using ARGUS.EAS;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;

namespace OGF.Service
{
    internal class SendReminder
    {
        private  Logger logger = null;
        private  Config _config = null;
        
        internal SendReminder(Config config)
        {
            _config = config;
            logger = LogManager.GetLogger(config.Name);
        }

        internal  void Process(SqlConnection sqlConnection, string uniqueId, string universignId)
        {
            int retry = 0;
            bool execute = false;

            while (!execute)
            {
                try
                {
                    logger.Trace("Send reminder - Start");
                    logger.Trace("Send reminder - UniqueId : " + uniqueId);

                    ARGUS.Diagnostics.Logger.Log("Processing uniqueId: " +  uniqueId);

                    //Send reminder
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
                                query.IndexFilters.Add(new IndexFilter { Name = "UniqueId", Filter = uniqueId });               //0
                                query.IndexFilters.Add(new IndexFilter { Name = "UniversignState", Filter = "Pending" });       //1    
                                query.IndexFilters.Add(new IndexFilter { Name = "Signed", Filter = "False" });                  //2
                                query.IndexFilters.Add(new IndexFilter { Name = "UniversignURL", Filter = "" });                //3
                                query.IndexFilters.Add(new IndexFilter { Name = "CustomerEmailAddress", Filter = "" });         //4
                                query.IndexFilters.Add(new IndexFilter { Name = "BackOfficeEmailAddress", Filter = "" });       //5
                                query.IndexFilters.Add(new IndexFilter { Name = "Civility", Filter = "" });                     //6   
                                query.IndexFilters.Add(new IndexFilter { Name = "FirstName", Filter = "" });                    //7
                                query.IndexFilters.Add(new IndexFilter { Name = "LastName", Filter = "" });                     //8
                                query.IndexFilters.Add(new IndexFilter { Name = "Trademark", Filter = "" });                    //9
                                query.IndexFilters.Add(new IndexFilter { Name = "DocID", Filter = "" });                        //10 
                                query.IndexFilters.Add(new IndexFilter { Name = "DocType", Filter = "" });                      //11

                                var records = domainQuery.DocumentTypes[_config.DocumenttypeName].Query.ExecuteQuery(query).ToList();

                                if (records.Count == 1)
                                {
                                    string univerSignURL = records[0].Values[3];
                                    string customerEmailAdress = records[0].Values[4];
                                    string backofficeEmailAdress = records[0].Values[5];
                                    string civility = records[0].Values[6];
                                    string firstName = records[0].Values[7];
                                    string lastName = records[0].Values[8];
                                    string docID = records[0].Values[10];
                                    string docType = records[0].Values[11];
                                    string tradeMark = records[0].Values[9];

                                    logger.Trace("Send reminder - CustomerEmailAdress: " + customerEmailAdress);
                                    logger.Trace("Send reminder - BackofficeEmailAdress: " + backofficeEmailAdress);

                                    ARGUS.Diagnostics.Logger.Log("CustomerEmailAdress: " + customerEmailAdress);
                                    ARGUS.Diagnostics.Logger.Log("BackofficeEmailAdress: " + backofficeEmailAdress);

                                    var tradeConfig = _config.TradeMarkConfigs.FirstOrDefault(t => t.Trademark.ToUpper() == tradeMark.ToUpper());

                                    if (tradeConfig == null)
                                    {
                                        //take default
                                        tradeConfig = _config.TradeMarkConfigs.FirstOrDefault(t => t.Trademark.ToUpper() == "DEFAULT");
                                        if (tradeConfig == null)
                                            throw new Exception(string.Format("Trademark '{0}' not found in the configuration.", tradeConfig.Trademark));
                                    }

                                    if (tradeConfig != null)
                                    {
                                        var mailTemplateRequest = tradeConfig.MailTemplates.FirstOrDefault(m => m.Id == MailId.MAILTEMPLATE_SIGNDOC_REQUEST_REMINDER);
                                        if (mailTemplateRequest != null)
                                        {
                                            if (!Common.UniqueIds.Contains(uniqueId))
                                            {
                                                UniversignRequests reqs = new UniversignRequests(_config.UniversignEmail, _config.UniversignPassword);
                                                var transactionInfo =  reqs.GetTransactionInfo(universignId);

                                                if (string.Compare(transactionInfo.status, "ready", true) == 0)
                                                {
                                                    logger.Trace("Send reminder - Send mail");
                                                    ARGUS.Diagnostics.Logger.Log("Send mail}");

                                                    List<string> attachments = null;
                                                    if (!string.IsNullOrEmpty(tradeConfig.CancellationDocument) &&
                                                        string.Compare(docType, "COMMANDE", true) == 0)
                                                    {
                                                        attachments = new List<string>();
                                                        attachments.Add(tradeConfig.CancellationDocument);
                                                    }

                                                    SendMail(_config.SMTP, mailTemplateRequest, customerEmailAdress.Split(';').ToList(), null, backofficeEmailAdress.Split(';').ToList(),
                                                            civility, firstName, lastName, univerSignURL, attachments, tradeConfig.Logo, tradeMark, docType + "_" + docID);

                                                    Common.UniqueIds.Add(uniqueId);
                                                }
                                            }
                                        }
                                        else
                                            throw new Exception(string.Format("The mailtemplate 'Request sign document' not found for trademark {0}.", tradeMark));
                                    }
                                    else
                                        throw new Exception(string.Format("Trademark '{0}' not found in the configuration.", tradeMark));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Thread.CurrentPrincipal = saveCurrentPrincipal;
                    }

                    //Update state in OGF Database to finished
                    SetState(sqlConnection, uniqueId, Common.SendReminderState.Finished);

                    Common.UniqueIds.Remove(uniqueId);

                    logger.Trace("Send reminder - End");

                    execute = true;

                }
                catch (Exception exception)
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
                        //Update state in OGF Database to Error
                        SetState(sqlConnection, uniqueId, Common.SendReminderState.Error);
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

        private void SendMail(SMTP smtp, MailTemplate mailTemplate,
                                    List<string> to, List<string> cc, List<string> bcc,
                                    string civility, string firstName,
                                    string lastName, string univerSignUrl, List<string> attachments,
                                    string logo, string tradeMark, string documentName)
        {
            try
            {

                //Send mail 
                Mail mail = new Mail();
                mail.Host = smtp.Host;
                mail.Port = smtp.Port;
                mail.Ssl = smtp.EnableSsl;
                mail.UserName = smtp.UserName;
                mail.Password = smtp.Password;

                string htmlBody = File.ReadAllText(mailTemplate.HtmlBodyFile);

                htmlBody = htmlBody.Replace("{{Civility}}", HttpUtility.HtmlEncode(civility))
                                   .Replace("{{FirstName}}", HttpUtility.HtmlEncode(firstName))
                                   .Replace("{{LastName}}", HttpUtility.HtmlEncode(lastName))
                                   .Replace("{{UniversignRequestUrl}}", HttpUtility.HtmlEncode(univerSignUrl))
                                   .Replace("{{Trademark}}", HttpUtility.HtmlEncode(tradeMark))
                                   .Replace("{{DocumentName}}", HttpUtility.HtmlEncode(documentName));


                string plainTextBody = File.ReadAllText(mailTemplate.PlainTextFile);
                plainTextBody = plainTextBody.Replace("{{Civility}}", HttpUtility.HtmlEncode(civility))
                                             .Replace("{{FirstName}}", HttpUtility.HtmlEncode(firstName))
                                             .Replace("{{LastName}}", HttpUtility.HtmlEncode(lastName))
                                             .Replace("{{UniversignRequestUrl}}", HttpUtility.HtmlEncode(univerSignUrl))
                                             .Replace("{{Trademark}}", HttpUtility.HtmlEncode(tradeMark))
                                             .Replace("{{DocumentName}}", HttpUtility.HtmlEncode(documentName));

                mail.From = mailTemplate.FromEmailAddr;
                mail.FromDisplay = mailTemplate.DisplayName;
                mail.To = to;
                if (cc != null && cc.Count > 0)
                    mail.CC = cc;
                if (bcc != null && bcc.Count > 0)
                    mail.BCC = bcc;
                mail.Subject = mailTemplate.Subject.Replace("{{Trademark}}", HttpUtility.HtmlEncode(tradeMark));
                mail.BodyHtml = htmlBody;
                mail.BodyPlainText = plainTextBody;
                mail.Logo = logo;
                if (attachments != null && attachments.Count > 0)
                    mail.Attachments = attachments;


                mail.SendMail();
            }
            catch (Exception ex)
            {
                throw new Exception("Sending email failed.", ex);
            }
        }

        private void SetState(SqlConnection connection, string uniqueId, Common.SendReminderState state)
        {
            
            logger.Trace("Send reminder - Update SendReminderState to " + state.ToString());
            ARGUS.Diagnostics.Logger.Log("Update SendReminderState to " + state.ToString());
            
            //Update State
            using (SqlCommand sqlCommand = new SqlCommand())
            {
                string sql = "";
                sql  = "UPDATE Transactions ";
                sql += "SET SendReminderState = @State ";
                sql += "WHERE UniqueId = @UniqueId";

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
