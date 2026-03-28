using System;
using MedicalReportsApp.Tools;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class AuthService
    {
        private Data data = new Data();
        private EmailService emailService = new EmailService();

        public void StartRegistration(string email, string password)
        {
            if (EmailExistsInPatients(email))
            {
                throw new Exception("User with this email already exists.");
            }

            string code = VerificationCodeHelper.GenerateCode();
            string passwordHash = PasswordHelper.HashPassword(password);
            DateTime expiresAt = DateTime.Now.AddMinutes(10);

            SaveOrUpdatePendingPatient(email, passwordHash, code, expiresAt);
            emailService.SendVerificationCode(email, code);
        }

        public bool VerifyCodeAndCreatePatient(string email, string code)
        {
            PendingPatient pendingPatient = GetPendingPatient(email);

            if (pendingPatient == null)
            {
                throw new Exception("Verification request was not found.");
            }

            if (pendingPatient.ExpiresAt < DateTime.Now)
            {
                throw new Exception("Verification code expired. Please register again.");
            }

            if (pendingPatient.VerificationCode != code)
            {
                return false;
            }

            CreatePatientFromPending(pendingPatient);
            DeletePendingPatient(email);
            return true;
        }

        private bool EmailExistsInPatients(string email)
        {
            string query = "SELECT COUNT(*) FROM Patients WHERE Email = @Email;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            return Convert.ToInt32(result) > 0;
        }

        private void SaveOrUpdatePendingPatient(string email, string passwordHash, string code, DateTime expiresAt)
        {
            string query = @"
INSERT INTO PendingPatients (Email, PasswordHash, VerificationCode, ExpiresAt, CreatedAt, IsVerified)
VALUES (@Email, @PasswordHash, @VerificationCode, @ExpiresAt, NOW(), 0)
ON DUPLICATE KEY UPDATE
    PasswordHash = @PasswordHash,
    VerificationCode = @VerificationCode,
    ExpiresAt = @ExpiresAt,
    CreatedAt = NOW(),
    IsVerified = 0;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@VerificationCode", code);
                command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
            });
        }

        private PendingPatient GetPendingPatient(string email)
        {
            string query = @"SELECT PendingPatientId, Email, PasswordHash, VerificationCode, ExpiresAt, CreatedAt, IsVerified
FROM PendingPatients
WHERE Email = @Email;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        PendingPatient pendingPatient = new PendingPatient();
                        pendingPatient.PendingPatientId = reader.GetInt32("PendingPatientId");
                        pendingPatient.Email = reader.GetString("Email");
                        pendingPatient.PasswordHash = reader.GetString("PasswordHash");
                        pendingPatient.VerificationCode = reader.GetString("VerificationCode");
                        pendingPatient.ExpiresAt = reader.GetDateTime("ExpiresAt");
                        pendingPatient.CreatedAt = reader.GetDateTime("CreatedAt");
                        pendingPatient.IsVerified = reader.GetBoolean("IsVerified");
                        return pendingPatient;
                    }
                }
            }

            return null;
        }

        private void CreatePatientFromPending(PendingPatient pendingPatient)
        {
            string query = @"
INSERT INTO Patients
(FirstName, LastName, Email, PasswordHash, Address, Gender, BirthDate, Phone, EmergencyContact, EmergencyPhone, FamilyDoctorId, CreatedAt, IsActive)
VALUES
(@FirstName, @LastName, @Email, @PasswordHash, @Address, @Gender, @BirthDate, @Phone, @EmergencyContact, @EmergencyPhone, @FamilyDoctorId, NOW(), 1);";

            data.ExecuteInsert(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", "New");
                command.Parameters.AddWithValue("@LastName", "Patient");
                command.Parameters.AddWithValue("@Email", pendingPatient.Email);
                command.Parameters.AddWithValue("@PasswordHash", pendingPatient.PasswordHash);
                command.Parameters.AddWithValue("@Address", DBNull.Value);
                command.Parameters.AddWithValue("@Gender", "Other");
                command.Parameters.AddWithValue("@BirthDate", new DateTime(2000, 1, 1));
                command.Parameters.AddWithValue("@Phone", DBNull.Value);
                command.Parameters.AddWithValue("@EmergencyContact", DBNull.Value);
                command.Parameters.AddWithValue("@EmergencyPhone", DBNull.Value);
                command.Parameters.AddWithValue("@FamilyDoctorId", DBNull.Value);
            });
        }

        private void DeletePendingPatient(string email)
        {
            string query = "DELETE FROM PendingPatients WHERE Email = @Email;";
            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });
        }
    }
}
