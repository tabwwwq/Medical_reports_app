using System.Security.Cryptography;
using System.Text;

namespace MedicalReportsApp.Tools
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            string hash = HashPassword(password);
            return hash == storedHash;
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 5851b048ca403db4e02813fa44c00bd41d75c69f
