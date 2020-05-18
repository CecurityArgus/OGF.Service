using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace OGF.Service
{
    public class Mail
    {
        private List<string> _to;
        private List<string> _CC;
        private List<string> _BCC;
        private string _from;
        private string _subject;
        private string _bodyHtml;
        private string _bodyPlainText;
        private List<string> _attachments;
        private string _host;
        private int _port;
        private bool _ssl;
        private string _fromDisplay;
        private string _user;
        private string _password;
        private string _logo;

        public Mail()
        {
            _to = null;
            _CC = null;
            _BCC = null;
            _attachments = null;
            _from = "";
            _subject = "";
            _bodyHtml = "";
            _bodyPlainText = "";
            _port = 25;
            _host = "";
            _ssl = false;
            _logo = "";

        }

        public string Logo
        {
            get
            {
                return _logo;
            }
            set
            {
                _logo = value;
            }
        }
        
        public List<string> Attachments
        {
            set
            {
                _attachments = value;
            }
            get
            {
                return _attachments;
            }

        }
        
        public List<string> To
        {

            set
            {
                _to = value;
            }
        }

        public List<string> CC
        {
            set
            {
                _CC = value;
            }
        }

        public List<string> BCC
        {
            set
            {
                _BCC = value;
            }
        }

        public string From
        {
            set
            {
                _from = value;
            }
        }

        public string Subject
        {
            set
            {
                _subject = value;
            }
        }

        public string BodyHtml
        {
            set
            {
                _bodyHtml = value;
            }
        }

        public string BodyPlainText
        {
            set
            {
                _bodyPlainText = value;
            }
        }

        public string Host
        {
            set
            {
                _host = value;
            }
        }

        public int Port
        {
            set
            {
                _port = value;
            }
        }

        public string UserName
        {

            set
            {
                _user = value;

            }
        }

        public string Password
        {
            set
            {

                _password = value;
            }

        }

        public bool Ssl
        {
            set
            {
                _ssl = value;

            }

        }

        public string FromDisplay
        {
            set
            {
                _fromDisplay = value;
            }
        }
        
        public void SendMail()
        {
            using (MailMessage mailMessage = new MailMessage())
            {
                //To
                if (_to != null)
                {
                    foreach (string address in _to)
                    {
                        if (!string.IsNullOrEmpty(address))
                            mailMessage.To.Add(address);
                    }
                }

                //CC
                if (_CC != null)
                {
                    foreach (string address in _CC)
                    {
                        if (!string.IsNullOrEmpty(address))
                            mailMessage.CC.Add(address);
                    }
                }

                //BCC
                if (_BCC != null)
                {
                    foreach (string address in _BCC)
                    {
                        if (!string.IsNullOrEmpty(address))
                            mailMessage.Bcc.Add(address);
                    }
                }

                //From
                if (string.IsNullOrEmpty(_fromDisplay))
                    mailMessage.From = new MailAddress(_from);
                else
                    mailMessage.From = new MailAddress(_from, _fromDisplay);
                           

                //Subject 
                mailMessage.Subject = _subject;

                //Body
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                AlternateView plainView = AlternateView.CreateAlternateViewFromString(_bodyPlainText, null, "text/plain");
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(_bodyHtml, null, "text/html");
                mailMessage.AlternateViews.Add(plainView);
                mailMessage.AlternateViews.Add(htmlView);


                List<FileStream> attachmentsStreams = new List<FileStream>();
                //Attachment
                if (_attachments != null)
                {
                    foreach (string attachment in _attachments)
                    {
                        FileStream f = new FileStream(attachment, FileMode.Open, FileAccess.Read);

                        mailMessage.Attachments.Add(new Attachment(f, Path.GetFileName(attachment)));

                        attachmentsStreams.Add(f);
                    }
                }

                /*
                var logoAttachment = new Attachment(@"C:\Users\FrankDescheerder\source\repos\OGF.API\OGF.API\Config\DIGNITE\Logo\LogoDignite2017322CP.jpg");
                logoAttachment.ContentId = "mylogo";
                logoAttachment.ContentDisposition.Inline = true;
                logoAttachment.ContentType.MediaType = "image/jpeg";
                

                mailMessage.Attachments.Add(logoAttachment);
                */
                if (!string.IsNullOrEmpty(_logo))
                {
                    LinkedResource inline = new LinkedResource(_logo, MediaTypeNames.Image.Jpeg);
                    inline.ContentId = "mylogo";
                    
                    htmlView.LinkedResources.Add(inline);
                }
                

                using (SmtpClient smtpClient = new SmtpClient(_host, _port))
                {
                    smtpClient.EnableSsl = _ssl;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_user, _password);

                    smtpClient.Send(mailMessage);
                }

                foreach (FileStream f in attachmentsStreams)
                {
                    f.Close();
                    f.Dispose();
                }
            }
        }

        private string GetHtmlBody(string body)
        {
            string htmlBody = "";

            string[] lines = body.Split(new char[] { '\r', '\n' });

            foreach (string line in lines)
                htmlBody += "<p>" + line + "</p>";

            return htmlBody;
        }

    }
}