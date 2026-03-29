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

        public bool Login(string email, string password, string role)
        {
            string table = role == "Doctor" ? "Doctors" : "Patients";
            string query = $"SELECT PasswordHash FROM {table} WHERE Email = @Email AND IsActive = 1;";

            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (result == null) return false;

            string storedHash = result.ToString();
            return PasswordHelper.VerifyPassword(password, storedHash);
        }

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
                throw new Exception("Verification request was not found.");

            if (pendingPatient.ExpiresAt < DateTime.Now)
                throw new Exception("Verification code expired. Please register again.");

            if (pendingPatient.VerificationCode != code)
                return false;

            CreatePatientFromPending(pendingPatient);
            DeletePendingPatient(email);
            return true;
        }

        public void StartPasswordReset(string email, string role)
        {
            if (!EmailExistsByRole(email, role))
            {
                throw new Exception("Account with this email was not found.");
            }

            string code = VerificationCodeHelper.GenerateCode();
            DateTime expiresAt = DateTime.Now.AddMinutes(10);

            SaveOrUpdatePendingPasswordReset(email, role, code, expiresAt);
            emailService.SendPasswordResetCode(email, code);
        }

        public bool VerifyCodeAndChangePassword(string email, string role, string code, string newPassword)
        {
            PendingPasswordReset reset = GetPendingPasswordReset(email, role);

            if (reset == null)
                throw new Exception("Password reset request was not found.");

            if (reset.ExpiresAt < DateTime.Now)
                throw new Exception("Verification code expired. Please request a new code.");

            if (reset.VerificationCode != code)
                return false;

            string passwordHash = PasswordHelper.HashPassword(newPassword);
            UpdatePasswordByRole(email, role, passwordHash);
            DeletePendingPasswordReset(email, role);
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

        private bool EmailExistsByRole(string email, string role)
        {
            string table = role == "Doctor" ? "Doctors" : "Patients";
            string query = $"SELECT COUNT(*) FROM {table} WHERE Email = @Email AND IsActive = 1;";
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

        private void SaveOrUpdatePendingPasswordReset(string email, string role, string code, DateTime expiresAt)
        {
            string query = @"
INSERT INTO PendingPasswordResets (Email, RoleName, VerificationCode, ExpiresAt, CreatedAt)
VALUES (@Email, @RoleName, @VerificationCode, @ExpiresAt, NOW())
ON DUPLICATE KEY UPDATE
    VerificationCode = @VerificationCode,
    ExpiresAt = @ExpiresAt,
    CreatedAt = NOW();";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@VerificationCode", code);
                command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
            });
        }

        private PendingPasswordReset GetPendingPasswordReset(string email, string role)
        {
            string query = @"SELECT PendingPasswordResetId, Email, RoleName, VerificationCode, ExpiresAt, CreatedAt
FROM PendingPasswordResets
WHERE Email = @Email AND RoleName = @RoleName;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@RoleName", role);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        PendingPasswordReset reset = new PendingPasswordReset();
                        reset.PendingPasswordResetId = reader.GetInt32("PendingPasswordResetId");
                        reset.Email = reader.GetString("Email");
                        reset.RoleName = reader.GetString("RoleName");
                        reset.VerificationCode = reader.GetString("VerificationCode");
                        reset.ExpiresAt = reader.GetDateTime("ExpiresAt");
                        reset.CreatedAt = reader.GetDateTime("CreatedAt");
                        return reset;
                    }
                }
            }

            return null;
        }

        private void UpdatePasswordByRole(string email, string role, string passwordHash)
        {
            string table = role == "Doctor" ? "Doctors" : "Patients";
            string query = $"UPDATE {table} SET PasswordHash = @PasswordHash WHERE Email = @Email AND IsActive = 1;";
            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Email", email);
            });
        }

        private void DeletePendingPasswordReset(string email, string role)
        {
            string query = "DELETE FROM PendingPasswordResets WHERE Email = @Email AND RoleName = @RoleName;";
            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@RoleName", role);
            });
        }
    }
}
