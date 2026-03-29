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
<<<<<<< HEAD
}
=======
}
>>>>>>> 5851b048ca403db4e02813fa44c00bd41d75c69f
