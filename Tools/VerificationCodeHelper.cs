using System;

namespace MedicalReportsApp.Tools
{
    public static class VerificationCodeHelper
    {
        public static string GenerateCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
