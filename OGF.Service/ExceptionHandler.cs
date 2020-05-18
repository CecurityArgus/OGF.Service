using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ARGUS.FrameWork.Exceptions;
using ARGUS;


namespace OGF.Service
{
    internal static class ExceptionHandler
    {
        public static Exception Show(Exception exception)
        {
            string message = GetExceptionMessage(exception);

            string title;

            var frm = System.Windows.Forms.Form.ActiveForm;
            if (frm != null)
                title = frm.Text;
            else
                title = Assembly.GetAssembly(typeof (ExceptionHandler)).GetName().Name;

            // Sometimes empty
            if (string.IsNullOrEmpty(title))
                title = "Error occured";

            System.Windows.Forms.MessageBox.Show(message, title, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

            return exception;
        }

        public static string GetExceptionMessage(Exception exception)
        {
            string message = "";
            Exception innerException = exception;

            while (innerException != null)
            {
                if (message.Length > 0)
                    message += Environment.NewLine;
                message += "- " + innerException.Message;

                var argusException = innerException as ArgusException;
                if (argusException != null)
                    message += " (Code: " + argusException.Code + ")";
                message += "\nStack: " + exception.StackTrace;  // Added stack because no MicrosoftExceptionBox


                innerException = innerException.InnerException;
            }
            return message;
        }


        public static Exception Log(Exception exception)
        {
            LogFile.WriteException(exception);
            return exception;
        }

        public static IEnumerable<Exception> GetExceptions(this Exception exception)
        {
            var innerException = exception;
            while (innerException != null)
            {
                yield return innerException;
                innerException = innerException.InnerException;
            }
        }
        
    }
}
