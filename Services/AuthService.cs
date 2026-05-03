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
        private TwoFactorAuthService twoFactorAuthService = new TwoFactorAuthService();
        private AuditLogService auditLogService = new AuditLogService();

        public bool Login(string email, string password, string role)
        {
            string table = GetTableNameByRole(role);
            string query = $"SELECT PasswordHash FROM {table} WHERE Email = @Email AND IsActive = 1 LIMIT 1;";

            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (result == null) return false;

            string storedHash = result.ToString();
            return PasswordHelper.VerifyPassword(password, storedHash);
        }

        public string GetBlockedAccountMessage(string email, string role)
        {
            if (role != "Doctor")
            {
                return "";
            }

            object result = data.ExecuteScalar("SELECT COUNT(*) FROM Doctors WHERE Email = @Email AND IsActive = 0;", command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (Convert.ToInt32(result) > 0)
            {
                return "Your account has been blocked. Please contact administration.";
            }

            return "";
        }

        public bool IsTwoFactorRequired(string email, string role)
        {
            if (role != "Patient")
            {
                return false;
            }

            return twoFactorAuthService.IsTwoFactorEnabled(email);
        }

        public bool VerifyTwoFactorCode(string email, string role, string code)
        {
            if (role != "Patient")
            {
                return true;
            }

            return twoFactorAuthService.VerifyLoginCode(email, code);
        }

        public void StartRegistration(string email, string password)
        {
            StartRegistration(email, password, "Patient");
        }

        public void StartRegistration(string email, string password, string role)
        {
            role = "Patient";

            if (EmailExistsByRole(email, role))
            {
                throw new Exception("User with this email already exists.");
            }

            string code = VerificationCodeHelper.GenerateCode();
            string passwordHash = PasswordHelper.HashPassword(password);
            DateTime expiresAt = DateTime.Now.AddMinutes(10);

            SaveOrUpdatePendingPatient(email, passwordHash, code, expiresAt, role);
            emailService.SendVerificationCode(email, code);
        }

        public bool VerifyCodeAndCreatePatient(string email, string code)
        {
            return VerifyCodeAndCreateAccount(email, code) == "Patient";
        }

        public string VerifyCodeAndCreateAccount(string email, string code)
        {
            PendingPatient pendingPatient = GetPendingPatient(email);

            if (pendingPatient == null)
                throw new Exception("Verification request was not found.");

            if (pendingPatient.ExpiresAt < DateTime.Now)
                throw new Exception("Verification code expired. Please register again.");

            if (pendingPatient.VerificationCode != code)
                return "";

            if (pendingPatient.RoleName == "Doctor")
            {
                CreateDoctorFromPending(pendingPatient);
            }
            else
            {
                CreatePatientFromPending(pendingPatient);
            }

            DeletePendingPatient(email);
            return pendingPatient.RoleName;
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

            string oldPasswordHash = GetPasswordHashByRole(email, role);
            string passwordHash = PasswordHelper.HashPassword(newPassword);
            UpdatePasswordByRole(email, role, passwordHash);
            DeletePendingPasswordReset(email, role);

            int userId = GetUserIdByRole(email, role);
            auditLogService.Log(
                role,
                userId,
                email,
                role,
                userId,
                email,
                "PASSWORD_CHANGED",
                "Account",
                "PasswordHash",
                oldPasswordHash,
                passwordHash,
                role + " changed password for account " + email
            );

            return true;
        }

        public int GetPatientIdByEmail(string email)
        {
            string query = "SELECT PatientId FROM Patients WHERE Email = @Email AND IsActive = 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (result == null)
            {
                throw new Exception("Patient was not found.");
            }

            return Convert.ToInt32(result);
        }

        public int GetDoctorIdByEmail(string email)
        {
            string query = "SELECT DoctorId FROM Doctors WHERE Email = @Email AND IsActive = 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (result == null)
            {
                throw new Exception("Doctor was not found.");
            }

            return Convert.ToInt32(result);
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
            string table = GetTableNameByRole(role);
            string query = $"SELECT COUNT(*) FROM {table} WHERE Email = @Email AND IsActive = 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });
            return Convert.ToInt32(result) > 0;
        }

        private int GetUserIdByRole(string email, string role)
        {
            string idColumn = GetIdColumnByRole(role);
            string table = GetTableNameByRole(role);
            string query = $"SELECT {idColumn} FROM {table} WHERE Email = @Email AND IsActive = 1 LIMIT 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            return result == null ? 0 : Convert.ToInt32(result);
        }

        private string GetPasswordHashByRole(string email, string role)
        {
            string table = GetTableNameByRole(role);
            string query = $"SELECT PasswordHash FROM {table} WHERE Email = @Email AND IsActive = 1 LIMIT 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            return result == null ? "" : result.ToString();
        }

        private string GetTableNameByRole(string role)
        {
            if (role == "Doctor")
            {
                return "Doctors";
            }

            if (role == "Admin")
            {
                EnsureAdminTable();
                return "Admins";
            }

            return "Patients";
        }

        private string GetIdColumnByRole(string role)
        {
            if (role == "Doctor")
            {
                return "DoctorId";
            }

            if (role == "Admin")
            {
                return "AdminId";
            }

            return "PatientId";
        }

        private void EnsureAdminTable()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS Admins
(
    AdminId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void SaveOrUpdatePendingPatient(string email, string passwordHash, string code, DateTime expiresAt, string role)
        {
            string query = @"
INSERT INTO PendingPatients (Email, PasswordHash, VerificationCode, RoleName, ExpiresAt, CreatedAt, IsVerified)
VALUES (@Email, @PasswordHash, @VerificationCode, @RoleName, @ExpiresAt, NOW(), 0)
ON DUPLICATE KEY UPDATE
    PasswordHash = @PasswordHash,
    VerificationCode = @VerificationCode,
    RoleName = @RoleName,
    ExpiresAt = @ExpiresAt,
    CreatedAt = NOW(),
    IsVerified = 0;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@VerificationCode", code);
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
            });
        }

        private PendingPatient GetPendingPatient(string email)
        {
            string query = @"SELECT PendingPatientId, Email, PasswordHash, VerificationCode, RoleName, ExpiresAt, CreatedAt, IsVerified
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
                        pendingPatient.RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? "Patient" : reader.GetString("RoleName");
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
(FirstName, LastName, Email, PasswordHash, Address, City, BloodType, Gender, BirthDate, Phone, FamilyDoctorId, CreatedAt, IsActive)
VALUES
(@FirstName, @LastName, @Email, @PasswordHash, @Address, @City, @BloodType, @Gender, @BirthDate, @Phone, @FamilyDoctorId, NOW(), 1);";

            data.ExecuteInsert(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", "New");
                command.Parameters.AddWithValue("@LastName", "Patient");
                command.Parameters.AddWithValue("@Email", pendingPatient.Email);
                command.Parameters.AddWithValue("@PasswordHash", pendingPatient.PasswordHash);
                command.Parameters.AddWithValue("@Address", DBNull.Value);
                command.Parameters.AddWithValue("@City", DBNull.Value);
                command.Parameters.AddWithValue("@BloodType", DBNull.Value);
                command.Parameters.AddWithValue("@Gender", "Other");
                command.Parameters.AddWithValue("@BirthDate", new DateTime(2000, 1, 1));
                command.Parameters.AddWithValue("@Phone", DBNull.Value);
                command.Parameters.AddWithValue("@FamilyDoctorId", DBNull.Value);
            });
        }

        private void CreateDoctorFromPending(PendingPatient pendingPatient)
        {
            string query = @"
INSERT INTO Doctors
(FirstName, LastName, Email, PasswordHash, Gender, BirthDate, Specialization, CreatedAt, IsActive)
VALUES
(@FirstName, @LastName, @Email, @PasswordHash, @Gender, @BirthDate, @Specialization, NOW(), 1);";

            data.ExecuteInsert(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", "New");
                command.Parameters.AddWithValue("@LastName", "Doctor");
                command.Parameters.AddWithValue("@Email", pendingPatient.Email);
                command.Parameters.AddWithValue("@PasswordHash", pendingPatient.PasswordHash);
                command.Parameters.AddWithValue("@Gender", "Other");
                command.Parameters.AddWithValue("@BirthDate", new DateTime(1990, 1, 1));
                command.Parameters.AddWithValue("@Specialization", "General doctor");
            });
        }

        public void UpdatePatientProfileAfterRegistration(string email, string firstName, string lastName, string phone, string address, string city, string bloodType, string gender, DateTime birthDate)
        {
            string query = @"
UPDATE Patients
SET FirstName = @FirstName,
    LastName = @LastName,
    Phone = @Phone,
    Address = @Address,
    City = @City,
    BloodType = @BloodType,
    Gender = @Gender,
    BirthDate = @BirthDate
WHERE Email = @Email;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@Address", address);
                command.Parameters.AddWithValue("@City", city);
                command.Parameters.AddWithValue("@BloodType", bloodType);
                command.Parameters.AddWithValue("@Gender", gender);
                command.Parameters.AddWithValue("@BirthDate", birthDate);
                command.Parameters.AddWithValue("@Email", email);
            });
        }

        public void UpdateDoctorProfileAfterRegistration(string email, string firstName, string lastName, string gender, DateTime birthDate, string specialization)
        {
            string query = @"
UPDATE Doctors
SET FirstName = @FirstName,
    LastName = @LastName,
    Gender = @Gender,
    BirthDate = @BirthDate,
    Specialization = @Specialization
WHERE Email = @Email AND IsActive = 1;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@Gender", gender);
                command.Parameters.AddWithValue("@BirthDate", birthDate);
                command.Parameters.AddWithValue("@Specialization", specialization);
                command.Parameters.AddWithValue("@Email", email);
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
            string table = GetTableNameByRole(role);
            string idColumn = GetIdColumnByRole(role);
            string query = $"UPDATE {table} SET PasswordHash = @PasswordHash WHERE Email = @Email AND {idColumn} > 0;";

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
