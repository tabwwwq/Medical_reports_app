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
            object result = data.ExecuteScalar("SELECT COUNT(*) FROM Patients WHERE PatientId = @PatientId AND FamilyDoctorId = @DoctorId AND IsActive = 1;", command =>
            {
                command.Parameters.AddWithValue("@PatientId", patientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
            });
            return Convert.ToInt32(result) > 0;
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
                        info.DoctorSpecialization = reader.IsDBNull(reader.GetOrdinal("DoctorSpecialization")) ? "General doctor" : reader.GetString("DoctorSpecialization");
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
                throw new Exception("Chat is available only between a patient and their family doctor.");
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
