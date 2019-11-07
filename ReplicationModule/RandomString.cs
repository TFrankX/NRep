using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace MessageQueue
{
    public class RandomString
    {

        private  Random random = new Random();
        public string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }



    }
}
