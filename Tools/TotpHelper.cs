using System;
using System.Security.Cryptography;

namespace MedicalReportsApp.Tools
{
    public static class TotpHelper
    {
        public static string GenerateSecretKey()
        {
            byte[] data = new byte[20];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            return Base32Helper.Encode(data);
        }

        public static bool VerifyCode(string secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            string cleanCode = code.Trim();
            long currentInterval = GetCurrentInterval();

            for (int i = -1; i <= 1; i++)
            {
                string expected = GenerateCode(secretKey, currentInterval + i);
                if (expected == cleanCode)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GenerateCode(string secretKey)
        {
            return GenerateCode(secretKey, GetCurrentInterval());
        }

        private static string GenerateCode(string secretKey, long counter)
        {
            byte[] key = Base32Helper.Decode(secretKey);
            byte[] counterBytes = BitConverter.GetBytes(counter);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using (HMACSHA1 hmac = new HMACSHA1(key))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 15;
                int binaryCode = ((hash[offset] & 127) << 24)
                               | ((hash[offset + 1] & 255) << 16)
                               | ((hash[offset + 2] & 255) << 8)
                               | (hash[offset + 3] & 255);
                int otp = binaryCode % 1000000;
                return otp.ToString("D6");
            }
        }

        private static long GetCurrentInterval()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        }
    }
}
