using System;
using System.Security.Cryptography;
using System.Text;

namespace DemonLord.Domain
{
    public static class PayloadChecksum
    {
        public static string ComputeSha256(string payloadJson)
        {
            if (payloadJson == null)
            {
                throw new ArgumentNullException(nameof(payloadJson));
            }

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(payloadBytes);
                StringBuilder result = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    result.Append(value.ToString("x2"));
                }

                return result.ToString();
            }
        }

        public static bool Matches(string payloadJson, string expectedSha256)
        {
            return expectedSha256 != null
                && string.Equals(ComputeSha256(payloadJson), expectedSha256, StringComparison.Ordinal);
        }
    }
}
