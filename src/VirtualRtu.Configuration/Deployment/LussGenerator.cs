using System;
using System.Text;

namespace VirtualRtu.Configuration.Deployment
{
    public class LussGenerator
    {
        private static readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcefghijklmnopqrtstuvwxyz0123456789";

        public static string Create()
        {
            int len = alphabet.Length - 1;
            Random ran = new Random();
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < 32; i++)
            {
                int id = ran.Next(0, len);
                builder.Append(alphabet[id]);
            }

            return builder.ToString();
        }
    }
}