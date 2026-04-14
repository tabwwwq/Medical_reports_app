using System;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class PatientDashboardService
    {
        private Data data = new Data();

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
            string query = @"
UPDATE Patients
SET Email = @NewEmail
WHERE Email = @CurrentEmail AND IsActive = 1;";

            data.ExecuteNonQuery(query, command =>
            {
                command.Parameters.AddWithValue("@NewEmail", newEmail);
                command.Parameters.AddWithValue("@CurrentEmail", currentEmail);
            });
        }

        public void UpdatePatientProfile(string email, string firstName, string lastName, string phone, DateTime birthDate, string gender, string address, string city)
        {
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
        }

    }
}
