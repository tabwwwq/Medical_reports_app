using System;
using System.Collections.Generic;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class FamilyDoctorService
    {
        private Data data = new Data();
        private const int MaxFamilyPatients = 10;

        public void EnsureFamilyDoctorTable()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS FamilyDoctorRequests (
    RequestId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    PatientId INT NOT NULL,
    RequestedDoctorId INT NOT NULL,
    RequestDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    AdminComment VARCHAR(500) NULL,
    INDEX IX_FamilyDoctorRequests_PatientId (PatientId),
    INDEX IX_FamilyDoctorRequests_RequestedDoctorId (RequestedDoctorId)
);", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<DoctorSearchCard> SearchAvailableDoctors(int patientId, string searchText)
        {
            EnsureFamilyDoctorTable();
            List<DoctorSearchCard> doctors = new List<DoctorSearchCard>();
            string query = @"
SELECT d.DoctorId,
       d.FirstName,
       d.LastName,
       d.Email,
       d.Specialization,
       d.AvatarUrl,
       COUNT(fp.PatientId) AS FamilyPatientCount,
       (SELECT r.Status FROM FamilyDoctorRequests r WHERE r.PatientId = @PatientId AND r.RequestedDoctorId = d.DoctorId ORDER BY r.RequestId DESC LIMIT 1) AS RequestStatus
FROM Doctors d
LEFT JOIN Patients fp ON fp.FamilyDoctorId = d.DoctorId AND fp.IsActive = 1
WHERE d.IsActive = 1
  AND (@SearchText = '' OR CONCAT(IFNULL(d.FirstName, ''), ' ', IFNULL(d.LastName, ''), ' ', IFNULL(d.Specialization, '')) LIKE CONCAT('%', @SearchText, '%'))
GROUP BY d.DoctorId, d.FirstName, d.LastName, d.Email, d.Specialization, d.AvatarUrl
HAVING FamilyPatientCount < 10
ORDER BY d.FirstName, d.LastName;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@SearchText", searchText == null ? "" : searchText.Trim());
                connection.Open();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DoctorSearchCard doctor = new DoctorSearchCard();
                        doctor.DoctorId = reader.GetInt32("DoctorId");
                        string firstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                        string lastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                        doctor.FullName = ("Dr. " + firstName + " " + lastName).Trim();
                        doctor.Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email");
                        doctor.Specialization = reader.IsDBNull(reader.GetOrdinal("Specialization")) ? "General doctor" : reader.GetString("Specialization");
                        doctor.AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? "" : reader.GetString("AvatarUrl");
                        doctor.FamilyPatientCount = Convert.ToInt32(reader["FamilyPatientCount"]);
                        doctor.RequestStatus = reader.IsDBNull(reader.GetOrdinal("RequestStatus")) ? "" : reader.GetString("RequestStatus");
                        doctors.Add(doctor);
                    }
                }
            }
            return doctors;
        }

        public void SendRequest(int patientId, int doctorId)
        {
            EnsureFamilyDoctorTable();
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlCommand countCommand = new MySqlCommand("SELECT COUNT(*) FROM Patients WHERE FamilyDoctorId = @DoctorId AND IsActive = 1;", connection))
                {
                    countCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                    if (Convert.ToInt32(countCommand.ExecuteScalar()) >= MaxFamilyPatients)
                    {
                        throw new Exception("This doctor already has 10 family patients.");
                    }
                }
                using (MySqlCommand check = new MySqlCommand("SELECT COUNT(*) FROM FamilyDoctorRequests WHERE PatientId = @PatientId AND RequestedDoctorId = @DoctorId AND Status = 'Pending';", connection))
                {
                    check.Parameters.AddWithValue("@PatientId", patientId);
                    check.Parameters.AddWithValue("@DoctorId", doctorId);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                    {
                        throw new Exception("You already sent a request to this doctor.");
                    }
                }
                using (MySqlCommand command = new MySqlCommand("INSERT INTO FamilyDoctorRequests (PatientId, RequestedDoctorId, Status) VALUES (@PatientId, @DoctorId, 'Pending');", connection))
                {
                    command.Parameters.AddWithValue("@PatientId", patientId);
                    command.Parameters.AddWithValue("@DoctorId", doctorId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<FamilyDoctorRequestCard> GetPendingRequests(int doctorId)
        {
            EnsureFamilyDoctorTable();
            List<FamilyDoctorRequestCard> requests = new List<FamilyDoctorRequestCard>();
            string query = @"
SELECT r.RequestId, p.PatientId, p.FirstName, p.LastName, p.BirthDate, p.AvatarUrl, r.RequestDate
FROM FamilyDoctorRequests r
INNER JOIN Patients p ON p.PatientId = r.PatientId
WHERE r.RequestedDoctorId = @DoctorId AND r.Status = 'Pending' AND p.IsActive = 1
ORDER BY r.RequestDate DESC, r.RequestId DESC;";
            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                connection.Open();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime birthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(2000, 1, 1) : reader.GetDateTime("BirthDate");
                        FamilyDoctorRequestCard request = new FamilyDoctorRequestCard();
                        request.RequestId = reader.GetInt32("RequestId");
                        request.PatientId = reader.GetInt32("PatientId");
                        request.PatientName = ((reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName")) + " " + (reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName"))).Trim();
                        request.PatientAge = CalculateAge(birthDate);
                        request.AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? "" : reader.GetString("AvatarUrl");
                        request.RequestDate = reader.GetDateTime("RequestDate");
                        requests.Add(request);
                    }
                }
            }
            return requests;
        }

        public void AcceptRequest(int requestId, int doctorId)
        {
            EnsureFamilyDoctorTable();
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    int patientId;
                    using (MySqlCommand get = new MySqlCommand("SELECT PatientId FROM FamilyDoctorRequests WHERE RequestId = @RequestId AND RequestedDoctorId = @DoctorId AND Status = 'Pending';", connection, transaction))
                    {
                        get.Parameters.AddWithValue("@RequestId", requestId);
                        get.Parameters.AddWithValue("@DoctorId", doctorId);
                        object result = get.ExecuteScalar();
                        if (result == null) throw new Exception("Request was not found.");
                        patientId = Convert.ToInt32(result);
                    }
                    using (MySqlCommand countCommand = new MySqlCommand("SELECT COUNT(*) FROM Patients WHERE FamilyDoctorId = @DoctorId AND IsActive = 1;", connection, transaction))
                    {
                        countCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                        if (Convert.ToInt32(countCommand.ExecuteScalar()) >= MaxFamilyPatients) throw new Exception("You already have 10 family patients.");
                    }
                    using (MySqlCommand updatePatient = new MySqlCommand("UPDATE Patients SET FamilyDoctorId = @DoctorId WHERE PatientId = @PatientId;", connection, transaction))
                    {
                        updatePatient.Parameters.AddWithValue("@DoctorId", doctorId);
                        updatePatient.Parameters.AddWithValue("@PatientId", patientId);
                        updatePatient.ExecuteNonQuery();
                    }
                    using (MySqlCommand updateRequest = new MySqlCommand("UPDATE FamilyDoctorRequests SET Status = 'Approved' WHERE RequestId = @RequestId; UPDATE FamilyDoctorRequests SET Status = 'Rejected' WHERE PatientId = @PatientId AND Status = 'Pending' AND RequestId <> @RequestId;", connection, transaction))
                    {
                        updateRequest.Parameters.AddWithValue("@RequestId", requestId);
                        updateRequest.Parameters.AddWithValue("@PatientId", patientId);
                        updateRequest.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public void RemoveFamilyDoctor(int patientId)
        {
            EnsureFamilyDoctorTable();
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    using (MySqlCommand updatePatient = new MySqlCommand("UPDATE Patients SET FamilyDoctorId = NULL WHERE PatientId = @PatientId;", connection, transaction))
                    {
                        updatePatient.Parameters.AddWithValue("@PatientId", patientId);
                        updatePatient.ExecuteNonQuery();
                    }

                    using (MySqlCommand updateRequests = new MySqlCommand("UPDATE FamilyDoctorRequests SET Status = 'Cancelled' WHERE PatientId = @PatientId AND Status = 'Pending';", connection, transaction))
                    {
                        updateRequests.Parameters.AddWithValue("@PatientId", patientId);
                        updateRequests.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        public void DeclineRequest(int requestId, int doctorId)
        {
            EnsureFamilyDoctorTable();
            data.ExecuteNonQuery("UPDATE FamilyDoctorRequests SET Status = 'Rejected' WHERE RequestId = @RequestId AND RequestedDoctorId = @DoctorId AND Status = 'Pending';", command =>
            {
                command.Parameters.AddWithValue("@RequestId", requestId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
            });
        }

        private int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now.Month < birthDate.Month || (DateTime.Now.Month == birthDate.Month && DateTime.Now.Day < birthDate.Day)) age--;
            return age;
        }
    }
}
