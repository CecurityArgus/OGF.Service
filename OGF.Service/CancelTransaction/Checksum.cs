using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace OGF.Service
{
    internal class Checksum
    {
        internal static void Validate(string id, string crc)
        {
            var iCRC_Time = 5;
            var iNow = DateTime.Now;
            var  y = iNow.Year * 60 * 24 * 365.25;
            var m = iNow.Month * 60 * 24 * (365.25/12);
            var d = iNow.Day * 60 * 24;
            var h = iNow.Hour * 60;
            var min = iNow.Minute;
            var t = (int) Math.Round((y + m + d + h + min) / iCRC_Time);

            var calcCRC = CreateMD5(id + t.ToString());

            if (string.Compare(calcCRC, crc, true) != 0)
                throw new Exception("Invalid checksum");
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static string CreateSHA256(string input)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public  static string CreateCheckSum(string passPhrase, string docType, string uniqueId, string docId, string eventId)
        {
            var iCRC_Time = 5;
            var iNow = DateTime.Now;
            var y = iNow.Year * 60 * 24 * 365.25;
            var m = iNow.Month * 60 * 24 * (365.25 / 12);
            var d = iNow.Day * 60 * 24;
            var h = iNow.Hour * 60;
            var min = iNow.Minute;
            var t = (int)Math.Round((y + m + d + h + min) / iCRC_Time);

            string param =docType + uniqueId + docId + eventId;

            return CreateSHA256(passPhrase + t.ToString() + param);
        }
    }
}