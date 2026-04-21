using System;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Tools;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class TwoFactorAuthService
    {
        private Data data = new Data();
        private AuditLogService auditLogService = new AuditLogService();

        public TwoFactorStatus GetPatientStatus(string email)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection);

                int patientId = GetPatientId(connection, email);
                if (patientId <= 0)
                {
                    throw new Exception("Patient was not found.");
                }

                using (MySqlCommand command = new MySqlCommand(@"SELECT SecretKey, IsEnabled FROM PatientTwoFactorAuth WHERE PatientId = @PatientId LIMIT 1;", connection))
                {
                    command.Parameters.AddWithValue("@PatientId", patientId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TwoFactorStatus
                            {
                                SecretKey = reader.GetString("SecretKey"),
                                IsEnabled = reader.GetBoolean("IsEnabled")
                            };
                        }
                    }
                }
            }

            return new TwoFactorStatus
            {
                SecretKey = "",
                IsEnabled = false
            };
        }

        public string StartSetup(string email)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection);

                int patientId = GetPatientId(connection, email);
                if (patientId <= 0)
                {
                    throw new Exception("Patient was not found.");
                }

                string secretKey = TotpHelper.GenerateSecretKey();

                using (MySqlCommand command = new MySqlCommand(@"
INSERT INTO PatientTwoFactorAuth (PatientId, SecretKey, IsEnabled, CreatedAt)
VALUES (@PatientId, @SecretKey, 0, NOW())
ON DUPLICATE KEY UPDATE
SecretKey = VALUES(SecretKey),
IsEnabled = 0;", connection))
                {
                    command.Parameters.AddWithValue("@PatientId", patientId);
                    command.Parameters.AddWithValue("@SecretKey", secretKey);
                    command.ExecuteNonQuery();
                }

                return secretKey;
            }
        }


        public string BuildOtpAuthUri(string email, string secretKey)
        {
            string issuer = "Medical Reports App";
            string label = Uri.EscapeDataString(issuer + ":" + email);
            string encodedIssuer = Uri.EscapeDataString(issuer);
            return $"otpauth://totp/{label}?secret={secretKey}&issuer={encodedIssuer}";
        }

        public bool Enable(string email, string code)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection);

                int patientId = GetPatientId(connection, email);
                if (patientId <= 0)
                {
                    throw new Exception("Patient was not found.");
                }

                string secretKey = GetSecretKey(connection, patientId);
                if (secretKey == "")
                {
                    throw new Exception("Two-factor setup was not started.");
                }

                if (!TotpHelper.VerifyCode(secretKey, code))
                {
                    return false;
                }

                using (MySqlCommand command = new MySqlCommand("UPDATE PatientTwoFactorAuth SET IsEnabled = 1 WHERE PatientId = @PatientId;", connection))
                {
                    command.Parameters.AddWithValue("@PatientId", patientId);
                    command.ExecuteNonQuery();
                }

                auditLogService.Log(
                    connection,
                    null,
                    "Patient",
                    patientId,
                    email,
                    "Patient",
                    patientId,
                    email,
                    "2FA_ENABLED",
                    "Security",
                    "TwoFactorAuth",
                    "Disabled",
                    "Enabled",
                    "Patient enabled two-factor authentication"
                );

                return true;
            }
        }

        public void Disable(string email)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection);
                int patientId = GetPatientId(connection, email);

                if (patientId <= 0)
                {
                    throw new Exception("Patient was not found.");
                }

                using (MySqlCommand command = new MySqlCommand("DELETE FROM PatientTwoFactorAuth WHERE PatientId = @PatientId;", connection))
                {
                    command.Parameters.AddWithValue("@PatientId", patientId);
                    command.ExecuteNonQuery();
                }

                auditLogService.Log(
                    connection,
                    null,
                    "Patient",
                    patientId,
                    email,
                    "Patient",
                    patientId,
                    email,
                    "2FA_DISABLED",
                    "Security",
                    "TwoFactorAuth",
                    "Enabled",
                    "Disabled",
                    "Patient disabled two-factor authentication"
                );
            }
        }

        public bool IsTwoFactorEnabled(string email)
        {
            return GetPatientStatus(email).IsEnabled;
        }

        public bool VerifyLoginCode(string email, string code)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection);
                int patientId = GetPatientId(connection, email);

                if (patientId <= 0)
                {
                    return false;
                }

                string secretKey = GetSecretKey(connection, patientId);
                if (secretKey == "")
                {
                    return false;
                }

                return TotpHelper.VerifyCode(secretKey, code);
            }
        }

        private int GetPatientId(MySqlConnection connection, string email)
        {
            using (MySqlCommand command = new MySqlCommand("SELECT PatientId FROM Patients WHERE Email = @Email AND IsActive = 1 LIMIT 1;", connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                object result = command.ExecuteScalar();
                return result == null ? 0 : Convert.ToInt32(result);
            }
        }

        private string GetSecretKey(MySqlConnection connection, int patientId)
        {
            using (MySqlCommand command = new MySqlCommand("SELECT SecretKey FROM PatientTwoFactorAuth WHERE PatientId = @PatientId AND IsEnabled = 1 LIMIT 1;", connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                object result = command.ExecuteScalar();
                if (result != null)
                {
                    return result.ToString();
                }
            }

            using (MySqlCommand command = new MySqlCommand("SELECT SecretKey FROM PatientTwoFactorAuth WHERE PatientId = @PatientId LIMIT 1;", connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                object result = command.ExecuteScalar();
                return result == null ? "" : result.ToString();
            }
        }

        private void EnsureTable(MySqlConnection connection)
        {
            using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS PatientTwoFactorAuth
(
    PatientId INT NOT NULL PRIMARY KEY,
    SecretKey VARCHAR(64) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_PatientTwoFactorAuth_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) ON DELETE CASCADE
);", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
