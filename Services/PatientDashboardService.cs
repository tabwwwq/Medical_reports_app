using System;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class PatientDashboardService
    {
        private Data data = new Data();
        private AuditLogService auditLogService = new AuditLogService();

        public PatientDashboardData GetDashboardByEmail(string email)
        {
            PatientDashboardData dashboard = new PatientDashboardData();
            dashboard.Patient = GetPatientByEmail(email);

            if (dashboard.Patient == null)
            {
                throw new Exception("Patient data was not found.");
            }

            LoadDoctor(dashboard);
            LoadAllergies(dashboard);
            LoadChronicProblems(dashboard);
            LoadVisits(dashboard);
            LoadPrescriptions(dashboard);

            return dashboard;
        }

        public Patient GetPatientByEmail(string email)
        {
            string query = @"
SELECT PatientId, FirstName, LastName, Email, PasswordHash, Address, City, BloodType, Gender, BirthDate, Phone, FamilyDoctorId, CreatedAt, IsActive
FROM Patients
WHERE Email = @Email AND IsActive = 1;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Patient patient = new Patient();
                        patient.Id = reader.GetInt32("PatientId");
                        patient.FirstName = reader.GetString("FirstName");
                        patient.LastName = reader.GetString("LastName");
                        patient.Email = reader.GetString("Email");
                        patient.PasswordHash = reader.GetString("PasswordHash");
                        patient.Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "Not added" : reader.GetString("Address");
                        patient.City = reader.IsDBNull(reader.GetOrdinal("City")) ? "" : reader.GetString("City");
                        patient.BloodType = reader.IsDBNull(reader.GetOrdinal("BloodType")) ? "Not added" : reader.GetString("BloodType");
                        patient.Gender = reader.GetString("Gender");
                        patient.BirthDate = reader.GetDateTime("BirthDate");
                        patient.Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "Not added" : reader.GetString("Phone");
                        patient.FamilyDoctorId = reader.IsDBNull(reader.GetOrdinal("FamilyDoctorId")) ? null : (int?)reader.GetInt32("FamilyDoctorId");
                        patient.CreatedAt = reader.GetDateTime("CreatedAt");
                        patient.IsActive = reader.GetBoolean("IsActive");
                        return patient;
                    }
                }
            }

            return null;
        }

        private void LoadDoctor(PatientDashboardData dashboard)
        {
            dashboard.DoctorName = "Not assigned";
            dashboard.DoctorSpecialization = "No family doctor yet";

            if (dashboard.Patient.FamilyDoctorId == null)
            {
                return;
            }

            string query = @"
SELECT FirstName, LastName, Specialization
FROM Doctors
WHERE DoctorId = @DoctorId AND IsActive = 1;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DoctorId", dashboard.Patient.FamilyDoctorId.Value);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dashboard.DoctorName = "Dr. " + reader.GetString("FirstName") + " " + reader.GetString("LastName");
                        dashboard.DoctorSpecialization = reader.IsDBNull(reader.GetOrdinal("Specialization")) ? "General doctor" : reader.GetString("Specialization");
                    }
                }
            }
        }

        private void LoadAllergies(PatientDashboardData dashboard)
        {
            string query = @"
SELECT at.Name
FROM PatientAllergies pa
INNER JOIN AllergyTypes at ON pa.AllergyTypeId = at.AllergyTypeId
WHERE pa.PatientId = @PatientId
ORDER BY at.Name;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", dashboard.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dashboard.Allergies.Add(reader.GetString("Name"));
                    }
                }
            }
        }

        private void LoadChronicProblems(PatientDashboardData dashboard)
        {
            string query = @"
SELECT cdt.Name
FROM PatientChronicDiseases pcd
INNER JOIN ChronicDiseaseTypes cdt ON pcd.ChronicDiseaseTypeId = cdt.ChronicDiseaseTypeId
WHERE pcd.PatientId = @PatientId
ORDER BY cdt.Name;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", dashboard.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dashboard.ChronicProblems.Add(reader.GetString("Name"));
                    }
                }
            }
        }

        private void LoadVisits(PatientDashboardData dashboard)
        {
            string query = @"
SELECT d.FirstName, d.LastName, v.VisitDate, v.VisitType, v.Diagnosis, v.Notes
FROM Visits v
INNER JOIN Doctors d ON v.DoctorId = d.DoctorId
WHERE v.PatientId = @PatientId
ORDER BY v.VisitDate DESC, v.VisitId DESC
LIMIT 20;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", dashboard.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        VisitCard visit = new VisitCard();
                        visit.DoctorName = "Dr. " + reader.GetString("FirstName") + " " + reader.GetString("LastName");
                        visit.VisitDate = reader.GetDateTime("VisitDate");
                        visit.VisitType = reader.GetString("VisitType");
                        visit.Diagnosis = reader.IsDBNull(reader.GetOrdinal("Diagnosis")) ? "" : reader.GetString("Diagnosis");
                        visit.Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString("Notes");
                        dashboard.Visits.Add(visit);
                    }
                }
            }
        }

        private void LoadPrescriptions(PatientDashboardData dashboard)
        {
            string query = @"
SELECT m.Name, pi.Dosage, pi.Frequency, p.Status
FROM Prescriptions p
INNER JOIN PrescriptionItems pi ON p.PrescriptionId = pi.PrescriptionId
INNER JOIN Medicines m ON pi.MedicineId = m.MedicineId
WHERE p.PatientId = @PatientId
ORDER BY p.IssueDate DESC, pi.PrescriptionItemId DESC
LIMIT 20;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", dashboard.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        PrescriptionCard prescription = new PrescriptionCard();
                        prescription.MedicineName = reader.GetString("Name");
                        prescription.Dosage = reader.GetString("Dosage");
                        prescription.Frequency = reader.IsDBNull(reader.GetOrdinal("Frequency")) ? "No schedule" : reader.GetString("Frequency");
                        prescription.Status = reader.GetString("Status");
                        dashboard.Prescriptions.Add(prescription);
                    }
                }
            }
        }

        public bool PatientEmailExists(string email)
        {
            string query = "SELECT COUNT(*) FROM Patients WHERE Email = @Email AND IsActive = 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            return Convert.ToInt32(result) > 0;
        }

        public bool VerifyPatientPassword(string email, string password)
        {
            string query = "SELECT PasswordHash FROM Patients WHERE Email = @Email AND IsActive = 1;";
            object result = data.ExecuteScalar(query, command =>
            {
                command.Parameters.AddWithValue("@Email", email);
            });

            if (result == null)
            {
                return false;
            }

            return Tools.PasswordHelper.VerifyPassword(password, result.ToString());
        }

        public void ChangePatientEmail(string currentEmail, string newEmail)
        {
            Patient patient = GetPatientByEmail(currentEmail);
            if (patient == null)
            {
                throw new Exception("Patient was not found.");
            }

            string query = @"
UPDATE Patients
SET Email = @NewEmail
WHERE Email = @CurrentEmail AND IsActive = 1;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@NewEmail", newEmail);
                command.Parameters.AddWithValue("@CurrentEmail", currentEmail);
            });

            string patientName = auditLogService.BuildUserName(patient.FirstName, patient.LastName, newEmail);
            auditLogService.Log(
                "Patient",
                patient.Id,
                patientName,
                "Patient",
                patient.Id,
                patientName,
                "EMAIL_CHANGED",
                "Account",
                "Email",
                currentEmail,
                newEmail,
                "Patient changed email from " + currentEmail + " to " + newEmail
            );
        }

        public void UpdatePatientProfile(string email, string firstName, string lastName, string phone, DateTime birthDate, string gender, string address, string city)
        {
            Patient patient = GetPatientByEmail(email);
            if (patient == null)
            {
                throw new Exception("Patient was not found.");
            }

            string query = @"
UPDATE Patients
SET FirstName = @FirstName,
    LastName = @LastName,
    Phone = @Phone,
    BirthDate = @BirthDate,
    Gender = @Gender,
    Address = @Address,
    City = @City
WHERE Email = @Email AND IsActive = 1;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@Phone", phone == "" ? (object)DBNull.Value : phone);
                command.Parameters.AddWithValue("@BirthDate", birthDate);
                command.Parameters.AddWithValue("@Gender", gender);
                command.Parameters.AddWithValue("@Address", address == "" ? (object)DBNull.Value : address);
                command.Parameters.AddWithValue("@City", city == "" ? (object)DBNull.Value : city);
                command.Parameters.AddWithValue("@Email", email);
            });

            string patientName = auditLogService.BuildUserName(firstName, lastName, email);
            LogPatientFieldChange(patient.Id, patientName, "FirstName", patient.FirstName, firstName);
            LogPatientFieldChange(patient.Id, patientName, "LastName", patient.LastName, lastName);
            LogPatientFieldChange(patient.Id, patientName, "Phone", NormalizeValue(patient.Phone), NormalizeValue(phone));
            LogPatientFieldChange(patient.Id, patientName, "BirthDate", patient.BirthDate.ToString("yyyy-MM-dd"), birthDate.ToString("yyyy-MM-dd"));
            LogPatientFieldChange(patient.Id, patientName, "Gender", patient.Gender, gender);
            LogPatientFieldChange(patient.Id, patientName, "Address", NormalizeValue(patient.Address), NormalizeValue(address));
            LogPatientFieldChange(patient.Id, patientName, "City", NormalizeValue(patient.City), NormalizeValue(city));
        }

        private void LogPatientFieldChange(int patientId, string patientName, string fieldName, string oldValue, string newValue)
        {
            oldValue = NormalizeValue(oldValue);
            newValue = NormalizeValue(newValue);

            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                return;
            }

            auditLogService.Log(
                "Patient",
                patientId,
                patientName,
                "Patient",
                patientId,
                patientName,
                "PROFILE_UPDATED",
                "PatientProfile",
                fieldName,
                oldValue,
                newValue,
                "Patient updated field " + fieldName
            );
        }

        private string NormalizeValue(string value)
        {
            value = (value ?? "").Trim();
            if (value == "Not added")
            {
                return "";
            }
            return value;
        }

        public void StartDeleteProfile(string email, string code)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsurePendingDeleteTable(connection);

                string query = @"
INSERT INTO PendingProfileDeletions (Email, VerificationCode, ExpiresAt, CreatedAt)
VALUES (@Email, @VerificationCode, @ExpiresAt, @CreatedAt)
ON DUPLICATE KEY UPDATE
VerificationCode = VALUES(VerificationCode),
ExpiresAt = VALUES(ExpiresAt),
CreatedAt = VALUES(CreatedAt);";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@VerificationCode", code);
                    command.Parameters.AddWithValue("@ExpiresAt", DateTime.Now.AddMinutes(10));
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool VerifyDeleteProfileCode(string email, string code)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsurePendingDeleteTable(connection);

                string query = @"
SELECT VerificationCode, ExpiresAt
FROM PendingProfileDeletions
WHERE Email = @Email;";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }

                        string savedCode = reader.GetString("VerificationCode");
                        DateTime expiresAt = reader.GetDateTime("ExpiresAt");
                        return savedCode == code && expiresAt >= DateTime.Now;
                    }
                }
            }
        }

        public bool DeletePatientProfileWithCode(string email, string code)
        {
            if (!VerifyDeleteProfileCode(email, code))
            {
                return false;
            }

            DeletePatientProfile(email);
            return true;
        }

        public void DeletePatientProfile(string email)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsurePendingDeleteTable(connection);

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int patientId = GetPatientIdForDelete(connection, transaction, email);

                        if (patientId <= 0)
                        {
                            throw new Exception("Patient was not found.");
                        }

                        string patientName = GetPatientNameForDelete(connection, transaction, patientId, email);
                        auditLogService.Log(
                            connection,
                            transaction,
                            "Patient",
                            patientId,
                            patientName,
                            "Patient",
                            patientId,
                            patientName,
                            "ACCOUNT_DELETED",
                            "Account",
                            "IsActive",
                            "Active",
                            "Deleted",
                            "Patient deleted account"
                        );

                        ExecuteDelete(connection, transaction, @"
DELETE vd FROM VisitDocuments vd
INNER JOIN Visits v ON vd.VisitId = v.VisitId
WHERE v.PatientId = @PatientId;", patientId, email);

                        ExecuteDelete(connection, transaction, @"
DELETE pi FROM PrescriptionItems pi
INNER JOIN Prescriptions p ON pi.PrescriptionId = p.PrescriptionId
WHERE p.PatientId = @PatientId;", patientId, email);

                        ExecuteDelete(connection, transaction, "DELETE FROM PatientAllergies WHERE PatientId = @PatientId;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM PatientChronicDiseases WHERE PatientId = @PatientId;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM Prescriptions WHERE PatientId = @PatientId;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM Visits WHERE PatientId = @PatientId;", patientId, email);

                        if (TableExists(connection, transaction, "FamilyDoctorRequests"))
                        {
                            if (ColumnExists(connection, transaction, "FamilyDoctorRequests", "PatientId"))
                            {
                                ExecuteDelete(connection, transaction, "DELETE FROM FamilyDoctorRequests WHERE PatientId = @PatientId;", patientId, email);
                            }

                            if (ColumnExists(connection, transaction, "FamilyDoctorRequests", "RequestingPatientId"))
                            {
                                ExecuteDelete(connection, transaction, "DELETE FROM FamilyDoctorRequests WHERE RequestingPatientId = @PatientId;", patientId, email);
                            }
                        }

                        ExecuteDelete(connection, transaction, "DELETE FROM PendingPasswordResets WHERE Email = @Email AND RoleName = 'Patient';", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM PatientTwoFactorAuth WHERE PatientId = @PatientId;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM PendingPatients WHERE Email = @Email;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM PendingProfileDeletions WHERE Email = @Email;", patientId, email);
                        ExecuteDelete(connection, transaction, "DELETE FROM Patients WHERE PatientId = @PatientId;", patientId, email);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void EnsurePendingDeleteTable(MySqlConnection connection)
        {
            string query = @"
CREATE TABLE IF NOT EXISTS PendingProfileDeletions
(
    Email VARCHAR(255) NOT NULL PRIMARY KEY,
    VerificationCode VARCHAR(10) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL
);";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private int GetPatientIdForDelete(MySqlConnection connection, MySqlTransaction transaction, string email)
        {
            using (MySqlCommand command = new MySqlCommand("SELECT PatientId FROM Patients WHERE Email = @Email LIMIT 1;", connection, transaction))
            {
                command.Parameters.AddWithValue("@Email", email);
                object result = command.ExecuteScalar();

                if (result == null)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
        }


        private string GetPatientNameForDelete(MySqlConnection connection, MySqlTransaction transaction, int patientId, string email)
        {
            using (MySqlCommand command = new MySqlCommand("SELECT FirstName, LastName, Email FROM Patients WHERE PatientId = @PatientId LIMIT 1;", connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return email;
                    }

                    string firstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                    string lastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                    string savedEmail = reader.IsDBNull(reader.GetOrdinal("Email")) ? email : reader.GetString("Email");
                    return auditLogService.BuildUserName(firstName, lastName, savedEmail);
                }
            }
        }

        private void ExecuteDelete(MySqlConnection connection, MySqlTransaction transaction, string query, int patientId, string email)
        {
            string tableName = GetMainTableName(query);

            if (tableName != "" && !TableExists(connection, transaction, tableName))
            {
                return;
            }

            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@Email", email);
                command.ExecuteNonQuery();
            }
        }

        private string GetMainTableName(string query)
        {
            string normalized = query.Replace("\r", " ").Replace("\n", " ").Trim();
            string upper = normalized.ToUpper();

            if (upper.StartsWith("DELETE FROM "))
            {
                string[] parts = normalized.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 3)
                {
                    return parts[2].Trim().TrimEnd(';');
                }
            }

            if (upper.StartsWith("DELETE VD FROM VISITDOCUMENTS"))
            {
                return "VisitDocuments";
            }

            if (upper.StartsWith("DELETE PI FROM PRESCRIPTIONITEMS"))
            {
                return "PrescriptionItems";
            }

            return "";
        }

        private bool TableExists(MySqlConnection connection, MySqlTransaction transaction, string tableName)
        {
            string query = @"
SELECT COUNT(*)
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name = @TableName;";

            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool ColumnExists(MySqlConnection connection, MySqlTransaction transaction, string tableName, string columnName)
        {
            string query = @"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @TableName
  AND column_name = @ColumnName;";

            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

    }
}
