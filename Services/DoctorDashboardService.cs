using System;
using System.Collections.Generic;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class DoctorDashboardService
    {
        private Data data = new Data();
        private AuditLogService auditLogService = new AuditLogService();

        public DoctorDashboardData GetDashboardByEmail(string email, string searchText)
        {
            DoctorDashboardData dashboard = new DoctorDashboardData();
            dashboard.Doctor = GetDoctorByEmail(email);

            if (dashboard.Doctor == null)
            {
                throw new Exception("Doctor data was not found.");
            }

            dashboard.Patients = GetDoctorPatients(dashboard.Doctor.Id, searchText);
            return dashboard;
        }

        public Doctor GetDoctorById(int doctorId)
        {
            string query = @"
SELECT DoctorId, FirstName, LastName, Email, PasswordHash, Gender, BirthDate, Specialization, CreatedAt, IsActive
FROM Doctors
WHERE DoctorId = @DoctorId AND IsActive = 1;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Doctor doctor = new Doctor();
                        doctor.Id = reader.GetInt32("DoctorId");
                        doctor.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "New" : reader.GetString("FirstName");
                        doctor.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "Doctor" : reader.GetString("LastName");
                        doctor.Email = reader.GetString("Email");
                        doctor.PasswordHash = reader.GetString("PasswordHash");
                        doctor.Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? "Other" : reader.GetString("Gender");
                        doctor.BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(1990, 1, 1) : reader.GetDateTime("BirthDate");
                        doctor.Specialization = reader.IsDBNull(reader.GetOrdinal("Specialization")) ? "General doctor" : reader.GetString("Specialization");
                        doctor.CreatedAt = reader.GetDateTime("CreatedAt");
                        doctor.IsActive = reader.GetBoolean("IsActive");
                        return doctor;
                    }
                }
            }

            return null;
        }

        public Doctor GetDoctorByEmail(string email)
        {
            string query = @"
SELECT DoctorId, FirstName, LastName, Email, PasswordHash, Gender, BirthDate, Specialization, CreatedAt, IsActive
FROM Doctors
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
                        Doctor doctor = new Doctor();
                        doctor.Id = reader.GetInt32("DoctorId");
                        doctor.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "New" : reader.GetString("FirstName");
                        doctor.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "Doctor" : reader.GetString("LastName");
                        doctor.Email = reader.GetString("Email");
                        doctor.PasswordHash = reader.GetString("PasswordHash");
                        doctor.Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? "Other" : reader.GetString("Gender");
                        doctor.BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(1990, 1, 1) : reader.GetDateTime("BirthDate");
                        doctor.Specialization = reader.IsDBNull(reader.GetOrdinal("Specialization")) ? "General doctor" : reader.GetString("Specialization");
                        doctor.CreatedAt = reader.GetDateTime("CreatedAt");
                        doctor.IsActive = reader.GetBoolean("IsActive");
                        return doctor;
                    }
                }
            }

            return null;
        }

        public List<DoctorDashboardPatientCard> GetDoctorPatients(int doctorId, string searchText)
        {
            List<DoctorDashboardPatientCard> patients = new List<DoctorDashboardPatientCard>();

            string query = @"
SELECT p.PatientId,
       p.FirstName,
       p.LastName,
       p.BirthDate,
       MAX(v.VisitDate) AS LastVisitDate
FROM Patients p
LEFT JOIN Visits v ON v.PatientId = p.PatientId
WHERE p.IsActive = 1
  AND (@SearchText = '' OR CONCAT(IFNULL(p.FirstName, ''), ' ', IFNULL(p.LastName, '')) LIKE CONCAT('%', @SearchText, '%'))
GROUP BY p.PatientId, p.FirstName, p.LastName, p.BirthDate
ORDER BY CASE WHEN MAX(v.VisitDate) IS NULL THEN 1 ELSE 0 END, MAX(v.VisitDate) DESC, p.FirstName, p.LastName;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SearchText", searchText == null ? "" : searchText.Trim());
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime birthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(2000, 1, 1) : reader.GetDateTime("BirthDate");
                        DoctorDashboardPatientCard card = new DoctorDashboardPatientCard();
                        card.PatientId = reader.GetInt32("PatientId");
                        card.FullName = ((reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName")) + " " + (reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName"))).Trim();
                        card.Age = CalculateAge(birthDate);
                        card.LastVisitDate = reader.IsDBNull(reader.GetOrdinal("LastVisitDate")) ? (DateTime?)null : reader.GetDateTime("LastVisitDate");
                        if (string.IsNullOrWhiteSpace(card.FullName))
                        {
                            card.FullName = "Unnamed patient";
                        }
                        patients.Add(card);
                    }
                }
            }

            return patients;
        }

        public DoctorPatientDetailsData GetPatientDetails(int patientId)
        {
            DoctorPatientDetailsData dataModel = new DoctorPatientDetailsData();
            dataModel.Patient = GetPatientById(patientId);

            if (dataModel.Patient == null)
            {
                throw new Exception("Patient data was not found.");
            }

            LoadAllergies(dataModel);
            LoadChronicProblems(dataModel);
            LoadVisits(dataModel);
            LoadPrescriptions(dataModel);
            return dataModel;
        }

        public Patient GetPatientById(int patientId)
        {
            string query = @"
SELECT PatientId, FirstName, LastName, Email, PasswordHash, Address, City, BloodType, Gender, BirthDate, Phone, FamilyDoctorId, CreatedAt, IsActive
FROM Patients
WHERE PatientId = @PatientId AND IsActive = 1;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Patient patient = new Patient();
                        patient.Id = reader.GetInt32("PatientId");
                        patient.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                        patient.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                        patient.Email = reader.GetString("Email");
                        patient.PasswordHash = reader.GetString("PasswordHash");
                        patient.Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "Not added" : reader.GetString("Address");
                        patient.City = reader.IsDBNull(reader.GetOrdinal("City")) ? "" : reader.GetString("City");
                        patient.BloodType = reader.IsDBNull(reader.GetOrdinal("BloodType")) ? "Not added" : reader.GetString("BloodType");
                        patient.Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? "Other" : reader.GetString("Gender");
                        patient.BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(2000, 1, 1) : reader.GetDateTime("BirthDate");
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

        public void UpdatePatientBasicInfo(int patientId, string firstName, string lastName, string phone, string address, string city, string bloodType, string gender, DateTime birthDate)
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
WHERE PatientId = @PatientId AND IsActive = 1;";

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
                command.Parameters.AddWithValue("@PatientId", patientId);
            });
        }

        private void LoadAllergies(DoctorPatientDetailsData dataModel)
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
                command.Parameters.AddWithValue("@PatientId", dataModel.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dataModel.Allergies.Add(reader.GetString("Name"));
                    }
                }
            }
        }

        private void LoadChronicProblems(DoctorPatientDetailsData dataModel)
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
                command.Parameters.AddWithValue("@PatientId", dataModel.Patient.Id);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dataModel.ChronicProblems.Add(reader.GetString("Name"));
                    }
                }
            }
        }

        private void LoadVisits(DoctorPatientDetailsData dataModel)
        {
            string query = @"
SELECT d.FirstName, d.LastName, v.VisitDate, v.VisitType, v.Diagnosis, v.Notes
FROM Visits v
INNER JOIN Doctors d ON v.DoctorId = d.DoctorId
WHERE v.PatientId = @PatientId
ORDER BY v.VisitDate DESC
LIMIT 20;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", dataModel.Patient.Id);
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
                        dataModel.Visits.Add(visit);
                    }
                }
            }
        }

        private void LoadPrescriptions(DoctorPatientDetailsData dataModel)
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
                command.Parameters.AddWithValue("@PatientId", dataModel.Patient.Id);
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
                        dataModel.Prescriptions.Add(prescription);
                    }
                }
            }
        }


        public void SavePatientRecord(int patientId, int doctorId, string firstName, string lastName, DateTime visitDate, string reason, string diagnosis, string notes, List<string> allergies, List<string> chronicProblems, List<string> prescriptions)
        {
            Doctor doctor = GetDoctorById(doctorId);

            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        PatientAuditSnapshot beforeSnapshot = GetPatientAuditSnapshot(connection, transaction, patientId);

                        UpdatePatientBasicInfo(connection, transaction, patientId, firstName, lastName);
                        SaveAllergies(connection, transaction, patientId, doctorId, allergies);
                        SaveChronicProblems(connection, transaction, patientId, doctorId, chronicProblems);
                        SaveVisit(connection, transaction, patientId, doctorId, visitDate, reason, diagnosis, notes);
                        SavePrescriptions(connection, transaction, patientId, doctorId, visitDate, prescriptions);

                        WritePatientAuditLogs(connection, transaction, beforeSnapshot, doctorId, doctor, firstName, lastName, visitDate, reason, diagnosis, notes, allergies, chronicProblems, prescriptions);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }
                        throw new Exception("Database save error: " + ex.Message);
                    }
                }
            }
        }

        private void UpdatePatientBasicInfo(MySqlConnection connection, MySqlTransaction transaction, int patientId, string firstName, string lastName)
        {
            string query = @"
UPDATE Patients
SET FirstName = @FirstName,
    LastName = @LastName
WHERE PatientId = @PatientId AND IsActive = 1;";

            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.ExecuteNonQuery();
            }
        }

        private void SaveAllergies(MySqlConnection connection, MySqlTransaction transaction, int patientId, int doctorId, List<string> allergies)
        {
            using (MySqlCommand deleteCommand = new MySqlCommand("DELETE FROM PatientAllergies WHERE PatientId = @PatientId;", connection, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@PatientId", patientId);
                deleteCommand.ExecuteNonQuery();
            }

            foreach (string rawName in allergies)
            {
                string name = (rawName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                int allergyTypeId = EnsureLookupId(connection, transaction, "AllergyTypes", "AllergyTypeId", name);
                using (MySqlCommand insertCommand = new MySqlCommand(@"
INSERT INTO PatientAllergies (PatientId, AllergyTypeId, AddedByDoctorId)
VALUES (@PatientId, @AllergyTypeId, @DoctorId);", connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@PatientId", patientId);
                    insertCommand.Parameters.AddWithValue("@AllergyTypeId", allergyTypeId);
                    insertCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        private void SaveChronicProblems(MySqlConnection connection, MySqlTransaction transaction, int patientId, int doctorId, List<string> chronicProblems)
        {
            using (MySqlCommand deleteCommand = new MySqlCommand("DELETE FROM PatientChronicDiseases WHERE PatientId = @PatientId;", connection, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@PatientId", patientId);
                deleteCommand.ExecuteNonQuery();
            }

            foreach (string rawName in chronicProblems)
            {
                string name = (rawName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                int chronicDiseaseTypeId = EnsureLookupId(connection, transaction, "ChronicDiseaseTypes", "ChronicDiseaseTypeId", name);
                using (MySqlCommand insertCommand = new MySqlCommand(@"
INSERT INTO PatientChronicDiseases (PatientId, ChronicDiseaseTypeId, AddedByDoctorId)
VALUES (@PatientId, @ChronicDiseaseTypeId, @DoctorId);", connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@PatientId", patientId);
                    insertCommand.Parameters.AddWithValue("@ChronicDiseaseTypeId", chronicDiseaseTypeId);
                    insertCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        private void SaveVisit(MySqlConnection connection, MySqlTransaction transaction, int patientId, int doctorId, DateTime visitDate, string reason, string diagnosis, string notes)
        {
            using (MySqlCommand insertCommand = new MySqlCommand(@"
INSERT INTO Visits (PatientId, DoctorId, VisitDate, VisitType, Diagnosis, Notes)
VALUES (@PatientId, @DoctorId, @VisitDate, @VisitType, @Diagnosis, @Notes);", connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@PatientId", patientId);
                insertCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                insertCommand.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                insertCommand.Parameters.AddWithValue("@VisitType", reason);
                insertCommand.Parameters.AddWithValue("@Diagnosis", diagnosis);
                insertCommand.Parameters.AddWithValue("@Notes", notes);
                insertCommand.ExecuteNonQuery();
            }
        }

        private void SavePrescriptions(MySqlConnection connection, MySqlTransaction transaction, int patientId, int doctorId, DateTime issueDate, List<string> prescriptions)
        {
            using (MySqlCommand deleteItemsCommand = new MySqlCommand(@"
DELETE pi FROM PrescriptionItems pi
INNER JOIN Prescriptions p ON p.PrescriptionId = pi.PrescriptionId
WHERE p.PatientId = @PatientId;", connection, transaction))
            {
                deleteItemsCommand.Parameters.AddWithValue("@PatientId", patientId);
                deleteItemsCommand.ExecuteNonQuery();
            }

            using (MySqlCommand deletePrescriptionsCommand = new MySqlCommand("DELETE FROM Prescriptions WHERE PatientId = @PatientId;", connection, transaction))
            {
                deletePrescriptionsCommand.Parameters.AddWithValue("@PatientId", patientId);
                deletePrescriptionsCommand.ExecuteNonQuery();
            }

            foreach (string rawPrescription in prescriptions)
            {
                string prescriptionText = (rawPrescription ?? "").Trim();
                if (string.IsNullOrWhiteSpace(prescriptionText))
                {
                    continue;
                }

                string medicineName = prescriptionText;
                string dosage = "Not specified";
                string frequency = "No schedule";
                string[] parts = prescriptionText.Split(new[] { " - " }, StringSplitOptions.None);
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    medicineName = parts[0].Trim();
                }
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    dosage = parts[1].Trim();
                }
                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
                {
                    frequency = parts[2].Trim();
                }

                int medicineId = EnsureLookupId(connection, transaction, "Medicines", "MedicineId", medicineName);
                long prescriptionId;

                using (MySqlCommand insertPrescriptionCommand = new MySqlCommand(@"
INSERT INTO Prescriptions (PatientId, DoctorId, IssueDate, Status)
VALUES (@PatientId, @DoctorId, @IssueDate, @Status);", connection, transaction))
                {
                    insertPrescriptionCommand.Parameters.AddWithValue("@PatientId", patientId);
                    insertPrescriptionCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                    insertPrescriptionCommand.Parameters.AddWithValue("@IssueDate", issueDate.Date);
                    insertPrescriptionCommand.Parameters.AddWithValue("@Status", "Active");
                    insertPrescriptionCommand.ExecuteNonQuery();
                    prescriptionId = insertPrescriptionCommand.LastInsertedId;
                }

                using (MySqlCommand insertItemCommand = new MySqlCommand(@"
INSERT INTO PrescriptionItems (PrescriptionId, MedicineId, Dosage, Frequency)
VALUES (@PrescriptionId, @MedicineId, @Dosage, @Frequency);", connection, transaction))
                {
                    insertItemCommand.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                    insertItemCommand.Parameters.AddWithValue("@MedicineId", medicineId);
                    insertItemCommand.Parameters.AddWithValue("@Dosage", dosage);
                    insertItemCommand.Parameters.AddWithValue("@Frequency", frequency);
                    insertItemCommand.ExecuteNonQuery();
                }
            }
        }

        private int EnsureLookupId(MySqlConnection connection, MySqlTransaction transaction, string tableName, string idColumnName, string name)
        {
            string selectQuery = "SELECT " + idColumnName + " FROM " + tableName + " WHERE Name = @Name LIMIT 1;";
            using (MySqlCommand selectCommand = new MySqlCommand(selectQuery, connection, transaction))
            {
                selectCommand.Parameters.AddWithValue("@Name", name);
                object existingId = selectCommand.ExecuteScalar();
                if (existingId != null)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            string insertQuery = "INSERT INTO " + tableName + " (Name) VALUES (@Name);";
            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@Name", name);
                insertCommand.ExecuteNonQuery();
                return (int)insertCommand.LastInsertedId;
            }
        }

        private void WritePatientAuditLogs(MySqlConnection connection, MySqlTransaction transaction, PatientAuditSnapshot beforeSnapshot, int doctorId, Doctor doctor, string firstName, string lastName, DateTime visitDate, string reason, string diagnosis, string notes, List<string> allergies, List<string> chronicProblems, List<string> prescriptions)
        {
            string doctorName = doctor == null ? "Doctor #" + doctorId : "Dr. " + (doctor.FirstName + " " + doctor.LastName).Trim();
            string patientName = (firstName + " " + lastName).Trim();
            string oldPatientName = (beforeSnapshot.FirstName + " " + beforeSnapshot.LastName).Trim();

            LogDoctorChange(connection, transaction, doctorId, doctorName, beforeSnapshot.PatientId, patientName, "PATIENT_BASIC_INFO_UPDATED", "PatientProfile", "FirstName", beforeSnapshot.FirstName, firstName, "Doctor updated patient first name");
            LogDoctorChange(connection, transaction, doctorId, doctorName, beforeSnapshot.PatientId, patientName, "PATIENT_BASIC_INFO_UPDATED", "PatientProfile", "LastName", beforeSnapshot.LastName, lastName, "Doctor updated patient last name");

            string oldAllergies = auditLogService.JoinList(beforeSnapshot.Allergies);
            string newAllergies = auditLogService.JoinList(allergies);
            LogDoctorChange(connection, transaction, doctorId, doctorName, beforeSnapshot.PatientId, patientName, "PATIENT_ALLERGIES_UPDATED", "Allergies", "Allergies", oldAllergies, newAllergies, "Doctor updated patient allergies");

            string oldChronicProblems = auditLogService.JoinList(beforeSnapshot.ChronicProblems);
            string newChronicProblems = auditLogService.JoinList(chronicProblems);
            LogDoctorChange(connection, transaction, doctorId, doctorName, beforeSnapshot.PatientId, patientName, "PATIENT_CHRONIC_PROBLEMS_UPDATED", "ChronicProblems", "ChronicProblems", oldChronicProblems, newChronicProblems, "Doctor updated patient chronic problems");

            string oldPrescriptions = auditLogService.JoinList(beforeSnapshot.Prescriptions);
            string newPrescriptions = auditLogService.JoinList(prescriptions);
            LogDoctorChange(connection, transaction, doctorId, doctorName, beforeSnapshot.PatientId, patientName, "PATIENT_PRESCRIPTIONS_UPDATED", "Prescriptions", "Prescriptions", oldPrescriptions, newPrescriptions, "Doctor updated patient prescriptions");

            string visitSummary = "Date: " + visitDate.ToString("yyyy-MM-dd") + "; Reason: " + NormalizeAuditValue(reason) + "; Diagnosis: " + NormalizeAuditValue(diagnosis) + "; Notes: " + NormalizeAuditValue(notes);
            auditLogService.Log(connection, transaction,
                "Doctor", doctorId, doctorName,
                "Patient", beforeSnapshot.PatientId, patientName,
                "PATIENT_VISIT_ADDED", "Visit", "Visit",
                null, visitSummary,
                "Doctor added a new visit to patient profile. Previous patient name: " + oldPatientName);
        }

        private void LogDoctorChange(MySqlConnection connection, MySqlTransaction transaction, int doctorId, string doctorName, int patientId, string patientName, string actionType, string entityType, string fieldName, string oldValue, string newValue, string description)
        {
            oldValue = NormalizeAuditValue(oldValue);
            newValue = NormalizeAuditValue(newValue);

            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                return;
            }

            auditLogService.Log(connection, transaction,
                "Doctor", doctorId, doctorName,
                "Patient", patientId, patientName,
                actionType, entityType, fieldName,
                oldValue, newValue, description);
        }

        private PatientAuditSnapshot GetPatientAuditSnapshot(MySqlConnection connection, MySqlTransaction transaction, int patientId)
        {
            PatientAuditSnapshot snapshot = new PatientAuditSnapshot();
            snapshot.PatientId = patientId;

            using (MySqlCommand command = new MySqlCommand(@"
SELECT FirstName, LastName
FROM Patients
WHERE PatientId = @PatientId AND IsActive = 1;", connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        snapshot.FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                        snapshot.LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                    }
                }
            }

            using (MySqlCommand command = new MySqlCommand(@"
SELECT at.Name
FROM PatientAllergies pa
INNER JOIN AllergyTypes at ON pa.AllergyTypeId = at.AllergyTypeId
WHERE pa.PatientId = @PatientId
ORDER BY at.Name;", connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        snapshot.Allergies.Add(reader.GetString("Name"));
                    }
                }
            }

            using (MySqlCommand command = new MySqlCommand(@"
SELECT cdt.Name
FROM PatientChronicDiseases pcd
INNER JOIN ChronicDiseaseTypes cdt ON pcd.ChronicDiseaseTypeId = cdt.ChronicDiseaseTypeId
WHERE pcd.PatientId = @PatientId
ORDER BY cdt.Name;", connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        snapshot.ChronicProblems.Add(reader.GetString("Name"));
                    }
                }
            }

            using (MySqlCommand command = new MySqlCommand(@"
SELECT CONCAT(m.Name, ' - ', pi.Dosage, ' - ', IFNULL(pi.Frequency, 'No schedule')) AS PrescriptionText
FROM Prescriptions p
INNER JOIN PrescriptionItems pi ON p.PrescriptionId = pi.PrescriptionId
INNER JOIN Medicines m ON pi.MedicineId = m.MedicineId
WHERE p.PatientId = @PatientId
ORDER BY p.IssueDate DESC, pi.PrescriptionItemId DESC;", connection, transaction))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        snapshot.Prescriptions.Add(reader.GetString("PrescriptionText"));
                    }
                }
            }

            return snapshot;
        }

        private string NormalizeAuditValue(string value)
        {
            return (value ?? "").Trim();
        }

        private class PatientAuditSnapshot
        {
            public int PatientId { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public List<string> Allergies { get; set; } = new List<string>();
            public List<string> ChronicProblems { get; set; } = new List<string>();
            public List<string> Prescriptions { get; set; } = new List<string>();
        }

        private int CalculateAge(DateTime birthDate)
        {
            DateTime now = DateTime.Now;
            int age = now.Year - birthDate.Year;
            if (now.Month < birthDate.Month || (now.Month == birthDate.Month && now.Day < birthDate.Day))
            {
                age--;
            }
            return age;
        }
    }
}
