using System;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WebServer.Utils.Requests
{
	public class SMS
	{


        public string Send2(string phoneNumber, string sender, string text)
        {
            APIRequest api;
            api = new APIRequest("https://api.easysendsms.app/bulksms", "GET", $"username=tompralpc2cf2024&password=WgxwSMpV&to={phoneNumber}&from={sender}&text={text}&type=0");
            //var resp = api.GetResponse();
            return api.Status;
        }
        public string Send(string phoneNumber, string sender, string text)
        {
            APIRequest api;
            //api = new APIRequest("https://api3.mobilipay.com:8209/omn/api/v2/init", "POST", "{\n\t\"messageText\":\"VGVzdCBNZXNzYWdl\",\n\t\"msisdn\":\"35795506767\",\n\t\"partnerId\":\"takecharger\"\n}", "Signature: c1b845685898dc9718faadc06e35b2d582dcaf35d366b533ebae97e557d33569");
            api = new APIRequest("https://api3.mobilipay.com:8209/omn/api/v2/init", "POST", $"{{\n\t\"messageText\":\"{Base64Encode(text)}\",\n\t\"msisdn\":\"{phoneNumber}\",\n\t\"partnerId\":\"{sender}\"\n}}", $"Signature: {HmacSha256("vaepheexaithah3Oozei3Shi", $"{Base64Encode(text)}{phoneNumber}{sender}")}");
            return api.Status;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string HmacSha256(string secret, string message)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            System.Security.Cryptography.HMACSHA256 cryptographer = new System.Security.Cryptography.HMACSHA256(keyBytes);

            byte[] bytes = cryptographer.ComputeHash(messageBytes);

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public string TunePhoneNumber(string phoneNumber)
        {
            string num = phoneNumber.Trim();
            num = Regex.Replace(num, "(?i)[ ()+_-]", "");
            //num = Regex.Replace(num, "^(357|357 | 357)\\s+","");
            if (num.Length == 8)
            {
                num = $"357{num}";
            }
            return num;
        }
        public string Gen4Code()
        {
            int _min = 10000;
            int _max = 99999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max).ToString();

        }

        //private static string HashHMACHex(string key, string message)
        //{
        //   // var key = Convert.FromHexString(keyHex);
        //   // var message = Convert.FromHexString(messageHex);
        //    var hash = new HMACSHA256(key);
        //    var hashb = hash.ComputeHash(message);
        //    return ByteToString(hashb);
        //    //return BitConverter.ToString(hashb).Replace("-", "").ToLower();
        //}

        //private static byte[] StringEncode(string text)
        //{
        //    var encoding = new System.Text.ASCIIEncoding();
        //    return encoding.GetBytes(text);
        //}

        //private static string ByteToString(byte[] buff)
        //{
        //    string sbinary = "";

        //    for (int i = 0; i < buff.Length; i++)
        //    {
        //        sbinary += buff[i].ToString("X2"); // hex format
        //    }
        //    return (sbinary);
        //}
    }
}

