using System;
using MedicalReportsApp.Classes;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class AdminDashboardService
    {
        private Data data = new Data();
        private AuditLogService auditLogService = new AuditLogService();

        public AdminDashboardData GetDashboardByEmail(string email, string searchText, string searchMode, string logCategory)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureAdminTable(connection);
                auditLogService.EnsureTable(connection, null);

                Admin admin = GetAdminByEmail(connection, email);
                if (admin == null)
                {
                    throw new Exception("Admin was not found.");
                }

                AdminDashboardData result = new AdminDashboardData();
                result.Admin = admin;

                using (MySqlCommand command = new MySqlCommand(BuildLogsQuery(searchMode, logCategory), connection))
                {
                    command.Parameters.AddWithValue("@SearchText", "%" + (searchText ?? "").Trim() + "%");
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AdminAuditLogCard card = new AdminAuditLogCard();
                            card.AuditLogId = reader.GetInt32("AuditLogId");
                            card.ActionType = SafeRead(reader, "ActionType");
                            card.EntityType = SafeRead(reader, "EntityType");
                            card.FieldName = SafeRead(reader, "FieldName");
                            card.Description = SafeRead(reader, "Description");
                            card.ActorName = SafeRead(reader, "ActorName");
                            card.TargetName = SafeRead(reader, "TargetName");
                            card.ActorUserType = SafeRead(reader, "ActorUserType");
                            card.TargetUserType = SafeRead(reader, "TargetUserType");
                            card.OldValue = SafeRead(reader, "OldValue");
                            card.NewValue = SafeRead(reader, "NewValue");
                            card.CreatedAt = reader.GetDateTime("CreatedAt");
                            card.Title = BuildTitle(card.ActionType);
                            card.LogTypeText = BuildLogTypeText(card.ActionType);

                            if (card.Description == "")
                            {
                                card.Description = BuildFallbackDescription(card);
                            }

                            result.Logs.Add(card);
                        }
                    }
                }

                return result;
            }
        }

        public void EnsureAdminTable()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureAdminTable(connection);
            }
        }

        private void EnsureAdminTable(MySqlConnection connection)
        {
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

        private Admin GetAdminByEmail(MySqlConnection connection, string email)
        {
            using (MySqlCommand command = new MySqlCommand(@"
SELECT AdminId, Email, PasswordHash, IsActive, CreatedAt
FROM Admins
WHERE Email = @Email AND IsActive = 1
LIMIT 1;", connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Admin
                        {
                            Id = reader.GetInt32("AdminId"),
                            Email = reader.GetString("Email"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            IsActive = reader.GetBoolean("IsActive"),
                            CreatedAt = reader.GetDateTime("CreatedAt")
                        };
                    }
                }
            }

            return null;
        }

        private string BuildLogsQuery(string searchMode, string logCategory)
        {
            string personFilter = "";

            if (searchMode == "Doctor")
            {
                personFilter = "\n  AND ActorUserType = 'Doctor'";
            }
            else if (searchMode == "Patient")
            {
                personFilter = "\n  AND TargetUserType = 'Patient'";
            }

            string categoryFilter = BuildCategoryFilter(logCategory);

            return @"
SELECT AuditLogId, ActorUserType, ActorName, TargetUserType, TargetName, ActionType, EntityType, FieldName, OldValue, NewValue, Description, CreatedAt
FROM AuditLogs
WHERE (
       @SearchText = '%%'
       OR COALESCE(ActionType, '') LIKE @SearchText
       OR COALESCE(Description, '') LIKE @SearchText
       OR COALESCE(ActorName, '') LIKE @SearchText
       OR COALESCE(TargetName, '') LIKE @SearchText
       OR COALESCE(EntityType, '') LIKE @SearchText
       OR COALESCE(FieldName, '') LIKE @SearchText
)
" + personFilter + categoryFilter + @"
ORDER BY CreatedAt DESC
LIMIT 300;";
        }

        private string BuildCategoryFilter(string logCategory)
        {
            switch ((logCategory ?? "All").Trim())
            {
                case "Allergies":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%ALLERG%' OR COALESCE(EntityType, '') LIKE '%Allerg%')";
                case "Prescriptions":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%PRESCRIPTION%' OR COALESCE(EntityType, '') LIKE '%Prescription%')";
                case "Visit":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%VISIT%' OR COALESCE(EntityType, '') LIKE '%Visit%')";
                case "Password":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%PASSWORD%' OR COALESCE(FieldName, '') LIKE '%Password%')";
                case "2FA":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%2FA%' OR COALESCE(FieldName, '') LIKE '%TwoFactor%' OR COALESCE(Description, '') LIKE '%two-factor%')";
                case "Email":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%EMAIL%' OR COALESCE(FieldName, '') LIKE '%Email%')";
                case "Delete Account":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%DELETE%' OR COALESCE(ActionType, '') LIKE '%DELETED%' OR COALESCE(Description, '') LIKE '%delete%' OR COALESCE(Description, '') LIKE '%deleted%')";
                case "Profile":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%PROFILE%' OR COALESCE(ActionType, '') LIKE '%BASIC_INFO%' OR COALESCE(EntityType, '') LIKE '%Profile%')";
                case "Chronic Problems":
                    return "\n  AND (COALESCE(ActionType, '') LIKE '%CHRONIC%' OR COALESCE(EntityType, '') LIKE '%Chronic%')";
                case "Security":
                    return "\n  AND (COALESCE(EntityType, '') LIKE '%Security%' OR COALESCE(ActionType, '') LIKE '%PASSWORD%' OR COALESCE(ActionType, '') LIKE '%2FA%')";
                case "Other":
                    return "\n  AND NOT (COALESCE(ActionType, '') LIKE '%ALLERG%' OR COALESCE(ActionType, '') LIKE '%PRESCRIPTION%' OR COALESCE(ActionType, '') LIKE '%VISIT%' OR COALESCE(ActionType, '') LIKE '%PASSWORD%' OR COALESCE(ActionType, '') LIKE '%2FA%' OR COALESCE(ActionType, '') LIKE '%EMAIL%' OR COALESCE(ActionType, '') LIKE '%DELETE%' OR COALESCE(ActionType, '') LIKE '%DELETED%' OR COALESCE(ActionType, '') LIKE '%PROFILE%' OR COALESCE(ActionType, '') LIKE '%BASIC_INFO%' OR COALESCE(ActionType, '') LIKE '%CHRONIC%')";
                default:
                    return "";
            }
        }

        private string SafeRead(MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return "";
            }

            return reader.GetValue(ordinal).ToString();
        }

        private string BuildTitle(string actionType)
        {
            actionType = (actionType ?? "").Trim();
            if (actionType == "")
            {
                return "System Log";
            }

            string[] parts = actionType.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                string value = parts[i].ToLower();
                if (value.Length > 0)
                {
                    parts[i] = char.ToUpper(value[0]) + value.Substring(1);
                }
            }

            return string.Join(" ", parts);
        }

        private string BuildLogTypeText(string actionType)
        {
            actionType = (actionType ?? "").ToUpper();

            if (actionType.Contains("DELETE") || actionType.Contains("REMOVED") || actionType.Contains("DISABLED"))
            {
                return "WARNING";
            }

            if (actionType.Contains("ERROR"))
            {
                return "ERROR";
            }

            return "INFO";
        }

        private string BuildFallbackDescription(AdminAuditLogCard card)
        {
            string actor = card.ActorName == "" ? card.ActorUserType : card.ActorName;
            string target = card.TargetName == "" ? card.TargetUserType : card.TargetName;
            string field = card.FieldName == "" ? card.EntityType : card.FieldName;

            return actor + " changed " + field + " for " + target;
        }
    }
}
