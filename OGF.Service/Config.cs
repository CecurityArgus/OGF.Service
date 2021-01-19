using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OGF.Service
{
    internal class Config
    {
        public string Name { get; set; }
        public string DatabaseConnectionstring { get; set; } 
        public string UniversignEmail { get; set; }
        public string UniversignPassword { get; set; }
        public string UniversignURL { get; set; }
        public string DomainName { get; set; }
        public string DocumenttypeName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool OGF_SendRequests { get; set; }
        public string OGF_Webservice { get; set; }
        public string OGF_PassPhrase { get; set; }
        public SMTP SMTP { get; set; }
        public List<TradeMarkConfig> TradeMarkConfigs { get; set; }
    }

    internal class SMTP
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class MailTemplate
    {
        public MailId Id { get; set; }
        public string Subject { get; set; }
        public string DisplayName { get; set; }
        public string FromEmailAddr { get; set; }
        public string HtmlBodyFile { get; set; }
        public string PlainTextFile { get; set; }
    }

    public class TradeMarkConfig
    {
        public string Trademark { get; set; }
        public List<MailTemplate> MailTemplates { get; set; }
        public string Logo { get; set; }
        public string Footer { get; set; }
        public string CancellationDocument { get; set; }
    }

    public enum MailId
    {
        MAILTEMPLATE_SIGNDOC_REQUEST = 1,
        MAILTEMPLATE_SIGNDOC_SIGNED = 2,
        MAILTEMPLATE_SIGNDOC_FAILED = 3,
        MAILTEMPLATE_SIGNDOC_CANCEL = 4,
        MAILTEMPLATE_ARCHIVE = 5,
        MAILTEMPLATE_SIGNDOC_REQUEST_REMINDER = 6
    }

    internal class Common
    {
        public static List<string> UniqueIds = new List<string>();

        public enum SendReminderState { Not_Used = 0, Waiting = 1, Finished = 2, Cancelled = 3, Error = 4 };

        public enum CancelTransactionState { Not_Used = 0, Waiting = 1, Finished = 2, Cancelled = 3, Error = 4 };
    }
}
