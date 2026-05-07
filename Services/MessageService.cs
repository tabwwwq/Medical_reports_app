using System;
using System.Collections.Generic;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class MessageService
    {
        private Data data = new Data();

        public void EnsureMessageTable()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS DoctorPatientMessages (
    MessageId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    SenderType VARCHAR(20) NOT NULL,
    MessageText TEXT NOT NULL,
    IsRead TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_DoctorPatientMessages_PatientDoctor (PatientId, DoctorId),
    INDEX IX_DoctorPatientMessages_CreatedAt (CreatedAt)
);", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool CanPatientOpenChat(int patientId, int doctorId)
        {
            object result = data.ExecuteScalar(@"
SELECT COUNT(*)
FROM Doctors d
WHERE d.DoctorId = @DoctorId
  AND d.IsActive = 1
  AND (
      EXISTS (SELECT 1 FROM Patients p WHERE p.PatientId = @PatientId AND p.FamilyDoctorId = d.DoctorId AND p.IsActive = 1)
      OR EXISTS (SELECT 1 FROM Visits v WHERE v.PatientId = @PatientId AND v.DoctorId = d.DoctorId)
  );", command =>
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
            });
            return Convert.ToInt32(result) > 0;
        }

        public List<DoctorChatContactCard> GetPatientAllowedDoctors(int patientId, string searchText)
        {
            List<DoctorChatContactCard> doctors = new List<DoctorChatContactCard>();
            string query = @"
SELECT d.DoctorId,
       d.FirstName,
       d.LastName,
       d.Specialization,
       d.AvatarUrl,
       MAX(v.VisitDate) AS LastVisitDate,
       CASE WHEN p.FamilyDoctorId = d.DoctorId THEN 1 ELSE 0 END AS IsFamilyDoctor
FROM Doctors d
INNER JOIN Patients p ON p.PatientId = @PatientId AND p.IsActive = 1
LEFT JOIN Visits v ON v.PatientId = p.PatientId AND v.DoctorId = d.DoctorId
WHERE d.IsActive = 1
  AND (p.FamilyDoctorId = d.DoctorId OR v.VisitId IS NOT NULL)
  AND (
      @SearchText = ''
      OR CONCAT(IFNULL(d.FirstName, ''), ' ', IFNULL(d.LastName, '')) LIKE CONCAT('%', @SearchText, '%')
      OR CONCAT(IFNULL(d.LastName, ''), ' ', IFNULL(d.FirstName, '')) LIKE CONCAT('%', @SearchText, '%')
      OR IFNULL(d.Specialization, '') LIKE CONCAT('%', @SearchText, '%')
  )
GROUP BY d.DoctorId, d.FirstName, d.LastName, d.Specialization, d.AvatarUrl, p.FamilyDoctorId
ORDER BY IsFamilyDoctor DESC, CASE WHEN MAX(v.VisitDate) IS NULL THEN 1 ELSE 0 END, MAX(v.VisitDate) DESC, d.FirstName, d.LastName;";

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
                        DoctorChatContactCard doctor = new DoctorChatContactCard();
                        doctor.DoctorId = reader.GetInt32("DoctorId");
                        string firstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                        string lastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                        doctor.FullName = ("Dr. " + firstName + " " + lastName).Trim();
                        if (doctor.FullName == "Dr.")
                        {
                            doctor.FullName = "Doctor";
                        }
                        doctor.Specialization = reader.IsDBNull(reader.GetOrdinal("Specialization")) ? "General Practitioner (GP)" : reader.GetString("Specialization");
                        doctor.AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? "" : reader.GetString("AvatarUrl");
                        doctor.LastVisitDate = reader.IsDBNull(reader.GetOrdinal("LastVisitDate")) ? (DateTime?)null : reader.GetDateTime("LastVisitDate");
                        doctor.IsFamilyDoctor = Convert.ToInt32(reader["IsFamilyDoctor"]) == 1;
                        doctors.Add(doctor);
                    }
                }
            }
            return doctors;
        }

        public bool CanDoctorOpenChat(int doctorId, int patientId)
        {
            return CanPatientOpenChat(patientId, doctorId);
        }

        public ChatPersonInfo GetChatPersonInfo(int patientId, int doctorId)
        {
            ChatPersonInfo info = new ChatPersonInfo();
            string query = @"
SELECT p.FirstName AS PatientFirstName,
       p.LastName AS PatientLastName,
       p.AvatarUrl AS PatientAvatarUrl,
       d.FirstName AS DoctorFirstName,
       d.LastName AS DoctorLastName,
       d.Specialization AS DoctorSpecialization,
       d.AvatarUrl AS DoctorAvatarUrl
FROM Patients p
INNER JOIN Doctors d ON d.DoctorId = @DoctorId
WHERE p.PatientId = @PatientId;";
            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                connection.Open();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string patientFirst = reader.IsDBNull(reader.GetOrdinal("PatientFirstName")) ? "" : reader.GetString("PatientFirstName");
                        string patientLast = reader.IsDBNull(reader.GetOrdinal("PatientLastName")) ? "" : reader.GetString("PatientLastName");
                        string doctorFirst = reader.IsDBNull(reader.GetOrdinal("DoctorFirstName")) ? "" : reader.GetString("DoctorFirstName");
                        string doctorLast = reader.IsDBNull(reader.GetOrdinal("DoctorLastName")) ? "" : reader.GetString("DoctorLastName");
                        info.PatientName = (patientFirst + " " + patientLast).Trim();
                        info.DoctorName = ("Dr. " + doctorFirst + " " + doctorLast).Trim();
                        info.DoctorSpecialization = reader.IsDBNull(reader.GetOrdinal("DoctorSpecialization")) ? "General Practitioner (GP)" : reader.GetString("DoctorSpecialization");
                        info.PatientAvatarUrl = reader.IsDBNull(reader.GetOrdinal("PatientAvatarUrl")) ? "" : reader.GetString("PatientAvatarUrl");
                        info.DoctorAvatarUrl = reader.IsDBNull(reader.GetOrdinal("DoctorAvatarUrl")) ? "" : reader.GetString("DoctorAvatarUrl");
                    }
                }
            }
            return info;
        }

        public List<ChatMessageCard> GetMessages(int patientId, int doctorId, string viewerType)
        {
            EnsureMessageTable();
            string otherType = viewerType == "Doctor" ? "Patient" : "Doctor";
            data.ExecuteNonQuery("UPDATE DoctorPatientMessages SET IsRead = 1 WHERE PatientId = @PatientId AND DoctorId = @DoctorId AND SenderType = @OtherType;", command =>
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                command.Parameters.AddWithValue("@OtherType", otherType);
            });

            List<ChatMessageCard> messages = new List<ChatMessageCard>();
            string query = @"
SELECT MessageId, SenderType, MessageText, IsRead, CreatedAt
FROM DoctorPatientMessages
WHERE PatientId = @PatientId AND DoctorId = @DoctorId
ORDER BY CreatedAt ASC, MessageId ASC;";
            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                connection.Open();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ChatMessageCard message = new ChatMessageCard();
                        message.MessageId = reader.GetInt32("MessageId");
                        message.SenderType = reader.GetString("SenderType");
                        message.MessageText = reader.GetString("MessageText");
                        message.IsRead = reader.GetBoolean("IsRead");
                        message.CreatedAt = reader.GetDateTime("CreatedAt");
                        message.IsMine = message.SenderType == viewerType;
                        messages.Add(message);
                    }
                }
            }
            return messages;
        }

        public void SendMessage(int patientId, int doctorId, string senderType, string text)
        {
            EnsureMessageTable();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Message is empty.");
            }
            if (!CanPatientOpenChat(patientId, doctorId))
            {
                throw new Exception("Chat is available only with your family doctor or a doctor who added your visit information.");
            }
            data.ExecuteNonQuery(@"
INSERT INTO DoctorPatientMessages (PatientId, DoctorId, SenderType, MessageText)
VALUES (@PatientId, @DoctorId, @SenderType, @MessageText);", command =>
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                command.Parameters.AddWithValue("@SenderType", senderType);
                command.Parameters.AddWithValue("@MessageText", text.Trim());
            });
        }
    }
}
