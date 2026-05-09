using System;
using System.Text;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class AuditLogService
    {
        private Data data = new Data();

        public void EnsureTable()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection, null);
            }
        }

        public void EnsureTable(MySqlConnection connection, MySqlTransaction transaction)
        {
            using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS AuditLogs
(
    AuditLogId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ActorUserType VARCHAR(50) NOT NULL,
    ActorUserId INT NULL,
    ActorName VARCHAR(255) NULL,
    TargetUserType VARCHAR(50) NOT NULL,
    TargetUserId INT NULL,
    TargetName VARCHAR(255) NULL,
    ActionType VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100) NOT NULL,
    FieldName VARCHAR(100) NULL,
    OldValue TEXT NULL,
    NewValue TEXT NULL,
    Description TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_AuditLogs_ActorUserId (ActorUserId),
    INDEX IX_AuditLogs_TargetUserId (TargetUserId),
    INDEX IX_AuditLogs_ActionType (ActionType),
    INDEX IX_AuditLogs_CreatedAt (CreatedAt)
);", connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        public void Log(string actorUserType, int? actorUserId, string actorName,
                        string targetUserType, int? targetUserId, string targetName,
                        string actionType, string entityType, string fieldName,
                        string oldValue, string newValue, string description)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureTable(connection, null);
                InsertLog(connection, null, actorUserType, actorUserId, actorName, targetUserType, targetUserId, targetName,
                    actionType, entityType, fieldName, oldValue, newValue, description);
            }
        }

        public void Log(MySqlConnection connection, MySqlTransaction transaction,
                        string actorUserType, int? actorUserId, string actorName,
                        string targetUserType, int? targetUserId, string targetName,
                        string actionType, string entityType, string fieldName,
                        string oldValue, string newValue, string description)
        {
            EnsureTable(connection, transaction);
            InsertLog(connection, transaction, actorUserType, actorUserId, actorName, targetUserType, targetUserId, targetName,
                actionType, entityType, fieldName, oldValue, newValue, description);
        }

        private void InsertLog(MySqlConnection connection, MySqlTransaction transaction,
                               string actorUserType, int? actorUserId, string actorName,
                               string targetUserType, int? targetUserId, string targetName,
                               string actionType, string entityType, string fieldName,
                               string oldValue, string newValue, string description)
        {
            using (MySqlCommand command = new MySqlCommand(@"
INSERT INTO AuditLogs
(
    ActorUserType,
    ActorUserId,
    ActorName,
    TargetUserType,
    TargetUserId,
    TargetName,
    ActionType,
    EntityType,
    FieldName,
    OldValue,
    NewValue,
    Description,
    CreatedAt
)
VALUES
(
    @ActorUserType,
    @ActorUserId,
    @ActorName,
    @TargetUserType,
    @TargetUserId,
    @TargetName,
    @ActionType,
    @EntityType,
    @FieldName,
    @OldValue,
    @NewValue,
    @Description,
    NOW()
);", connection, transaction))
            {
                command.Parameters.AddWithValue("@ActorUserType", Safe(actorUserType, 50));
                command.Parameters.AddWithValue("@ActorUserId", actorUserId.HasValue ? (object)actorUserId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@ActorName", NullIfEmpty(Safe(actorName, 255)));
                command.Parameters.AddWithValue("@TargetUserType", Safe(targetUserType, 50));
                command.Parameters.AddWithValue("@TargetUserId", targetUserId.HasValue ? (object)targetUserId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@TargetName", NullIfEmpty(Safe(targetName, 255)));
                command.Parameters.AddWithValue("@ActionType", Safe(actionType, 100));
                command.Parameters.AddWithValue("@EntityType", Safe(entityType, 100));
                command.Parameters.AddWithValue("@FieldName", NullIfEmpty(Safe(fieldName, 100)));
                command.Parameters.AddWithValue("@OldValue", NullIfEmpty(oldValue));
                command.Parameters.AddWithValue("@NewValue", NullIfEmpty(newValue));
                command.Parameters.AddWithValue("@Description", NullIfEmpty(description));
                command.ExecuteNonQuery();
            }
        }

        public string BuildUserName(string firstName, string lastName, string email)
        {
            string fullName = ((firstName ?? "").Trim() + " " + (lastName ?? "").Trim()).Trim();
            if (fullName != "")
            {
                return fullName;
            }
            return (email ?? "").Trim();
        }

        public string JoinList(System.Collections.Generic.IEnumerable<string> items)
        {
            if (items == null)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            foreach (string item in items)
            {
                string value = (item ?? "").Trim();
                if (value == "")
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(value);
            }

            return builder.ToString();
        }

        private object NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private string Safe(string value, int maxLength)
        {
            value = value ?? "";
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength);
            }
            return value;
        }
    }
}
