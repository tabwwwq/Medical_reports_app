using System;
using System.Collections.Generic;
using System.Linq;
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
                new AvatarService().EnsureAvatarColumns();
                EnsureAdminTable(connection);
                auditLogService.EnsureTable(connection, null);
                EnsureSuspiciousActivityActionsTable(connection);

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
                            result.Logs.Add(MapAuditLogCard(reader));
                        }
                    }
                }

                result.SuspiciousActivities = GetSuspiciousActivities(connection);
                result.Doctors = GetDoctors(connection);
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
SELECT AuditLogId, ActorUserId, ActorUserType, ActorName, TargetUserType, TargetName, ActionType, EntityType, FieldName, OldValue, NewValue, Description, CreatedAt
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
LIMIT 10;";
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

        private List<SuspiciousActivityAlert> GetSuspiciousActivities(MySqlConnection connection)
        {
            List<AdminAuditLogCard> doctorChanges = new List<AdminAuditLogCard>();

            using (MySqlCommand command = new MySqlCommand(@"
SELECT AuditLogId, ActorUserId, ActorUserType, ActorName, TargetUserType, TargetName, ActionType, EntityType, FieldName, OldValue, NewValue, Description, CreatedAt
FROM AuditLogs
WHERE COALESCE(ActorUserType, '') = 'Doctor'
  AND COALESCE(TargetUserType, '') = 'Patient'
  AND CreatedAt >= DATE_SUB(NOW(), INTERVAL 2 DAY)
ORDER BY ActorName ASC, CreatedAt ASC;", connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        doctorChanges.Add(MapAuditLogCard(reader));
                    }
                }
            }

            List<SuspiciousActivityAlert> alerts = new List<SuspiciousActivityAlert>();
            foreach (var doctorGroup in doctorChanges.GroupBy(x => (x.ActorName ?? "").Trim()))
            {
                string doctorName = string.IsNullOrWhiteSpace(doctorGroup.Key) ? "Unknown doctor" : doctorGroup.Key;
                List<AdminAuditLogCard> ordered = doctorGroup.OrderBy(x => x.CreatedAt).ToList();

                SuspiciousActivityAlert bestAlert = null;
                for (int startIndex = 0; startIndex < ordered.Count; startIndex++)
                {
                    DateTime startTime = ordered[startIndex].CreatedAt;
                    List<AdminAuditLogCard> windowItems = ordered
                        .Where(x => x.CreatedAt >= startTime && x.CreatedAt <= startTime.AddMinutes(30))
                        .ToList();

                    if (windowItems.Count < 10)
                    {
                        continue;
                    }

                    SuspiciousActivityAlert candidate = BuildSuspiciousAlert(doctorName, windowItems);
                    if (bestAlert == null || candidate.ChangeCount > bestAlert.ChangeCount || (candidate.ChangeCount == bestAlert.ChangeCount && candidate.WindowEnd > bestAlert.WindowEnd))
                    {
                        bestAlert = candidate;
                    }
                }

                if (bestAlert != null && !IsSuspiciousAlertAlreadyHandled(connection, bestAlert))
                {
                    alerts.Add(bestAlert);
                }
            }

            return alerts.OrderByDescending(x => GetPriorityRank(x.Priority)).ThenByDescending(x => x.WindowEnd).Take(10).ToList();
        }

        private SuspiciousActivityAlert BuildSuspiciousAlert(string doctorName, List<AdminAuditLogCard> windowItems)
        {
            SuspiciousActivityAlert alert = new SuspiciousActivityAlert();
            alert.DoctorName = doctorName;
            alert.DoctorId = windowItems.Where(x => x.ActorUserId.HasValue).Select(x => x.ActorUserId).FirstOrDefault();
            alert.ChangeCount = windowItems.Count;
            alert.WindowStart = windowItems.Min(x => x.CreatedAt);
            alert.WindowEnd = windowItems.Max(x => x.CreatedAt);
            alert.Changes = windowItems.OrderByDescending(x => x.CreatedAt).ToList();

            if (alert.ChangeCount >= 30)
            {
                alert.Priority = "HIGH PRIORITY";
                alert.Title = "Made " + alert.ChangeCount + " patient record changes within 30 minutes - urgent suspicious activity";
                alert.Description = "Rapid high-volume editing pattern detected. Immediate review is recommended.";
            }
            else if (alert.ChangeCount >= 20)
            {
                alert.Priority = "MEDIUM PRIORITY";
                alert.Title = "Made " + alert.ChangeCount + " patient record changes within 30 minutes - unusual editing pattern detected";
                alert.Description = "Large amount of patient data changes in a short time window.";
            }
            else
            {
                alert.Priority = "LOW PRIORITY";
                alert.Title = "Made " + alert.ChangeCount + " patient record changes within 30 minutes - possible bulk editing activity";
                alert.Description = "Monitor the recent activity and confirm whether the changes were expected.";
            }

            return alert;
        }


        private void EnsureSuspiciousActivityActionsTable(MySqlConnection connection)
        {
            using (MySqlCommand command = new MySqlCommand(@"
CREATE TABLE IF NOT EXISTS SuspiciousActivityActions
(
    SuspiciousActivityActionId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    DoctorId INT NULL,
    DoctorName VARCHAR(255) NOT NULL,
    WindowStart DATETIME NOT NULL,
    WindowEnd DATETIME NOT NULL,
    ActionStatus VARCHAR(50) NOT NULL,
    ActionByAdminEmail VARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_SuspiciousActivityActions_DoctorWindow (DoctorName, WindowStart, WindowEnd)
);", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private bool IsSuspiciousAlertAlreadyHandled(MySqlConnection connection, SuspiciousActivityAlert alert)
        {
            using (MySqlCommand command = new MySqlCommand(@"
SELECT COUNT(*)
FROM SuspiciousActivityActions
WHERE DoctorName = @DoctorName
  AND WindowStart = @WindowStart
  AND WindowEnd = @WindowEnd;", connection))
            {
                command.Parameters.AddWithValue("@DoctorName", (alert.DoctorName ?? "").Trim());
                command.Parameters.AddWithValue("@WindowStart", alert.WindowStart);
                command.Parameters.AddWithValue("@WindowEnd", alert.WindowEnd);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public void MarkSuspiciousActivityAction(SuspiciousActivityAlert alert, string adminEmail, string actionStatus)
        {
            if (alert == null)
            {
                return;
            }

            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureSuspiciousActivityActionsTable(connection);

                using (MySqlCommand command = new MySqlCommand(@"
INSERT INTO SuspiciousActivityActions
(
    DoctorId,
    DoctorName,
    WindowStart,
    WindowEnd,
    ActionStatus,
    ActionByAdminEmail,
    CreatedAt
)
VALUES
(
    @DoctorId,
    @DoctorName,
    @WindowStart,
    @WindowEnd,
    @ActionStatus,
    @ActionByAdminEmail,
    NOW()
);", connection))
                {
                    command.Parameters.AddWithValue("@DoctorId", alert.DoctorId.HasValue ? (object)alert.DoctorId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@DoctorName", (alert.DoctorName ?? "").Trim());
                    command.Parameters.AddWithValue("@WindowStart", alert.WindowStart);
                    command.Parameters.AddWithValue("@WindowEnd", alert.WindowEnd);
                    command.Parameters.AddWithValue("@ActionStatus", (actionStatus ?? "").Trim());
                    command.Parameters.AddWithValue("@ActionByAdminEmail", string.IsNullOrWhiteSpace(adminEmail) ? (object)DBNull.Value : adminEmail.Trim());
                    command.ExecuteNonQuery();
                }
            }
        }

        public void FreezeDoctorAccount(SuspiciousActivityAlert alert, string adminEmail)
        {
            if (alert == null)
            {
                throw new Exception("Suspicious activity alert was not found.");
            }

            if (!alert.DoctorId.HasValue || alert.DoctorId.Value <= 0)
            {
                throw new Exception("Doctor account could not be identified.");
            }

            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureSuspiciousActivityActionsTable(connection);
                auditLogService.EnsureTable(connection, null);

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string doctorEmail = "";
                        string doctorName = alert.DoctorName ?? "Doctor";
                        bool isActive = false;

                        using (MySqlCommand getCommand = new MySqlCommand(@"
SELECT Email, FirstName, LastName, IsActive
FROM Doctors
WHERE DoctorId = @DoctorId
LIMIT 1;", connection, transaction))
                        {
                            getCommand.Parameters.AddWithValue("@DoctorId", alert.DoctorId.Value);
                            using (MySqlDataReader reader = getCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Doctor account was not found.");
                                }

                                doctorEmail = reader["Email"].ToString();
                                doctorName = auditLogService.BuildUserName(reader["FirstName"].ToString(), reader["LastName"].ToString(), doctorEmail);
                                isActive = reader.GetBoolean("IsActive");
                            }
                        }

                        if (!isActive)
                        {
                            throw new Exception("This doctor account is already blocked.");
                        }

                        using (MySqlCommand updateCommand = new MySqlCommand(@"
UPDATE Doctors
SET IsActive = 0
WHERE DoctorId = @DoctorId;", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@DoctorId", alert.DoctorId.Value);
                            updateCommand.ExecuteNonQuery();
                        }

                        using (MySqlCommand actionCommand = new MySqlCommand(@"
INSERT INTO SuspiciousActivityActions
(
    DoctorId,
    DoctorName,
    WindowStart,
    WindowEnd,
    ActionStatus,
    ActionByAdminEmail,
    CreatedAt
)
VALUES
(
    @DoctorId,
    @DoctorName,
    @WindowStart,
    @WindowEnd,
    'FROZEN',
    @ActionByAdminEmail,
    NOW()
);", connection, transaction))
                        {
                            actionCommand.Parameters.AddWithValue("@DoctorId", alert.DoctorId.Value);
                            actionCommand.Parameters.AddWithValue("@DoctorName", doctorName);
                            actionCommand.Parameters.AddWithValue("@WindowStart", alert.WindowStart);
                            actionCommand.Parameters.AddWithValue("@WindowEnd", alert.WindowEnd);
                            actionCommand.Parameters.AddWithValue("@ActionByAdminEmail", string.IsNullOrWhiteSpace(adminEmail) ? (object)DBNull.Value : adminEmail.Trim());
                            actionCommand.ExecuteNonQuery();
                        }

                        auditLogService.Log(
                            connection,
                            transaction,
                            "Admin",
                            null,
                            adminEmail,
                            "Doctor",
                            alert.DoctorId.Value,
                            doctorName,
                            "ACCOUNT_FREEZE",
                            "Security",
                            "IsActive",
                            "1",
                            "0",
                            "Doctor account was frozen from suspicious activity alert."
                        );

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

        private List<AdminDoctorCard> GetDoctors(MySqlConnection connection)
        {
            List<AdminDoctorCard> doctors = new List<AdminDoctorCard>();

            using (MySqlCommand command = new MySqlCommand(@"
SELECT DoctorId, FirstName, LastName, Email, Gender, BirthDate, Specialization, CreatedAt, IsActive, AvatarUrl
FROM Doctors
ORDER BY CreatedAt DESC, DoctorId DESC;", connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        doctors.Add(new AdminDoctorCard
                        {
                            DoctorId = reader.GetInt32("DoctorId"),
                            FirstName = SafeRead(reader, "FirstName"),
                            LastName = SafeRead(reader, "LastName"),
                            Email = SafeRead(reader, "Email"),
                            Gender = SafeRead(reader, "Gender"),
                            BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(1990, 1, 1) : reader.GetDateTime("BirthDate"),
                            Specialization = SafeRead(reader, "Specialization"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            IsActive = reader.GetBoolean("IsActive"),
                            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? "" : reader.GetString("AvatarUrl")
                        });
                    }
                }
            }

            return doctors;
        }

        public void AddDoctor(string adminEmail, string firstName, string lastName, string email, string password, string gender, DateTime birthDate, string specialization, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(specialization))
            {
                throw new Exception("Please fill in all doctor fields.");
            }

            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                new AvatarService().EnsureAvatarColumns();
                auditLogService.EnsureTable(connection, null);

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand existsCommand = new MySqlCommand("SELECT COUNT(*) FROM Doctors WHERE Email = @Email;", connection, transaction))
                        {
                            existsCommand.Parameters.AddWithValue("@Email", email.Trim());
                            if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
                            {
                                throw new Exception("Doctor with this email already exists.");
                            }
                        }

                        int doctorId;
                        string passwordHash = Tools.PasswordHelper.HashPassword(password.Trim());
                        using (MySqlCommand insertCommand = new MySqlCommand(@"
INSERT INTO Doctors
(FirstName, LastName, Email, PasswordHash, Gender, BirthDate, Specialization, CreatedAt, IsActive, AvatarUrl)
VALUES
(@FirstName, @LastName, @Email, @PasswordHash, @Gender, @BirthDate, @Specialization, NOW(), @IsActive, NULL);", connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@FirstName", firstName.Trim());
                            insertCommand.Parameters.AddWithValue("@LastName", lastName.Trim());
                            insertCommand.Parameters.AddWithValue("@Email", email.Trim());
                            insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            insertCommand.Parameters.AddWithValue("@Gender", gender.Trim());
                            insertCommand.Parameters.AddWithValue("@BirthDate", birthDate);
                            insertCommand.Parameters.AddWithValue("@Specialization", specialization.Trim());
                            insertCommand.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                            insertCommand.ExecuteNonQuery();
                            doctorId = Convert.ToInt32(insertCommand.LastInsertedId);
                        }

                        auditLogService.Log(
                            connection,
                            transaction,
                            "Admin",
                            null,
                            adminEmail,
                            "Doctor",
                            doctorId,
                            (firstName + " " + lastName).Trim(),
                            "DOCTOR_CREATED",
                            "Doctor",
                            "Account",
                            "",
                            email.Trim(),
                            "Admin created a new doctor account."
                        );

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


        public void UpdateDoctor(int doctorId, string adminEmail, string firstName, string lastName, string email, string gender, DateTime birthDate, string specialization, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(specialization))
            {
                throw new Exception("Please fill in all doctor fields.");
            }

            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                new AvatarService().EnsureAvatarColumns();
                auditLogService.EnsureTable(connection, null);

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        AdminDoctorCard oldDoctor = null;
                        using (MySqlCommand getCommand = new MySqlCommand(@"
SELECT DoctorId, FirstName, LastName, Email, Gender, BirthDate, Specialization, CreatedAt, IsActive, AvatarUrl
FROM Doctors
WHERE DoctorId = @DoctorId
LIMIT 1;", connection, transaction))
                        {
                            getCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                            using (MySqlDataReader reader = getCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Doctor was not found.");
                                }

                                oldDoctor = new AdminDoctorCard
                                {
                                    DoctorId = reader.GetInt32("DoctorId"),
                                    FirstName = SafeRead(reader, "FirstName"),
                                    LastName = SafeRead(reader, "LastName"),
                                    Email = SafeRead(reader, "Email"),
                                    Gender = SafeRead(reader, "Gender"),
                                    BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? new DateTime(1990, 1, 1) : reader.GetDateTime("BirthDate"),
                                    Specialization = SafeRead(reader, "Specialization"),
                                    CreatedAt = reader.GetDateTime("CreatedAt"),
                                    IsActive = reader.GetBoolean("IsActive"),
                                    AvatarUrl = reader.IsDBNull(reader.GetOrdinal("AvatarUrl")) ? "" : reader.GetString("AvatarUrl")
                                };
                            }
                        }

                        using (MySqlCommand existsCommand = new MySqlCommand("SELECT COUNT(*) FROM Doctors WHERE Email = @Email AND DoctorId <> @DoctorId;", connection, transaction))
                        {
                            existsCommand.Parameters.AddWithValue("@Email", email.Trim());
                            existsCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                            if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
                            {
                                throw new Exception("Another doctor already uses this email.");
                            }
                        }

                        using (MySqlCommand updateCommand = new MySqlCommand(@"
UPDATE Doctors
SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Gender = @Gender, BirthDate = @BirthDate, Specialization = @Specialization, IsActive = @IsActive
WHERE DoctorId = @DoctorId;", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@FirstName", firstName.Trim());
                            updateCommand.Parameters.AddWithValue("@LastName", lastName.Trim());
                            updateCommand.Parameters.AddWithValue("@Email", email.Trim());
                            updateCommand.Parameters.AddWithValue("@Gender", gender.Trim());
                            updateCommand.Parameters.AddWithValue("@BirthDate", birthDate);
                            updateCommand.Parameters.AddWithValue("@Specialization", specialization.Trim());
                            updateCommand.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                            updateCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                            updateCommand.ExecuteNonQuery();
                        }

                        string newName = (firstName + " " + lastName).Trim();
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "FirstName", oldDoctor.FirstName, firstName.Trim());
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "LastName", oldDoctor.LastName, lastName.Trim());
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "Email", oldDoctor.Email, email.Trim());
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "Gender", oldDoctor.Gender, gender.Trim());
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "BirthDate", oldDoctor.BirthDate.ToString("yyyy-MM-dd"), birthDate.ToString("yyyy-MM-dd"));
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "Specialization", oldDoctor.Specialization, specialization.Trim());
                        LogDoctorChange(connection, transaction, adminEmail, doctorId, newName, "IsActive", oldDoctor.IsActive ? "1" : "0", isActive ? "1" : "0");

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

        private void LogDoctorChange(MySqlConnection connection, MySqlTransaction transaction, string adminEmail, int doctorId, string doctorName, string fieldName, string oldValue, string newValue)
        {
            if ((oldValue ?? "").Trim() == (newValue ?? "").Trim())
            {
                return;
            }

            auditLogService.Log(
                connection,
                transaction,
                "Admin",
                null,
                adminEmail,
                "Doctor",
                doctorId,
                doctorName,
                "DOCTOR_UPDATED",
                "Doctor",
                fieldName,
                oldValue,
                newValue,
                "Admin updated doctor information."
            );
        }

        public void FreezeDoctorAccountById(int doctorId, string adminEmail)
        {
            UpdateDoctorActiveStatus(doctorId, adminEmail, false);
        }

        public void UnfreezeDoctorAccountById(int doctorId, string adminEmail)
        {
            UpdateDoctorActiveStatus(doctorId, adminEmail, true);
        }

        private void UpdateDoctorActiveStatus(int doctorId, string adminEmail, bool isActive)
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                new AvatarService().EnsureAvatarColumns();
                auditLogService.EnsureTable(connection, null);
                EnsureSuspiciousActivityActionsTable(connection);

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string doctorName = "Doctor";
                        string doctorEmail = "";
                        bool currentIsActive;

                        using (MySqlCommand getCommand = new MySqlCommand(@"
SELECT FirstName, LastName, Email, IsActive
FROM Doctors
WHERE DoctorId = @DoctorId
LIMIT 1;", connection, transaction))
                        {
                            getCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                            using (MySqlDataReader reader = getCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Doctor was not found.");
                                }

                                doctorEmail = SafeRead(reader, "Email");
                                doctorName = auditLogService.BuildUserName(SafeRead(reader, "FirstName"), SafeRead(reader, "LastName"), doctorEmail);
                                currentIsActive = reader.GetBoolean("IsActive");
                            }
                        }

                        if (currentIsActive == isActive)
                        {
                            throw new Exception(isActive ? "Doctor account is already active." : "Doctor account is already frozen.");
                        }

                        using (MySqlCommand updateCommand = new MySqlCommand(@"
UPDATE Doctors
SET IsActive = @IsActive
WHERE DoctorId = @DoctorId;", connection, transaction))
                        {
                            updateCommand.Parameters.Add("@IsActive", MySqlDbType.Int16).Value = isActive ? 1 : 0;
                            updateCommand.Parameters.Add("@DoctorId", MySqlDbType.Int32).Value = doctorId;

                            int affectedRows = updateCommand.ExecuteNonQuery();
                            if (affectedRows <= 0)
                            {
                                throw new Exception("Doctor account status could not be updated.");
                            }
                        }

                        if (isActive)
                        {
                            using (MySqlCommand clearFrozenActionsCommand = new MySqlCommand(@"
INSERT INTO SuspiciousActivityActions
(
    DoctorId,
    DoctorName,
    WindowStart,
    WindowEnd,
    ActionStatus,
    ActionByAdminEmail,
    CreatedAt
)
SELECT
    @DoctorId,
    @DoctorName,
    sa.WindowStart,
    sa.WindowEnd,
    'UNFROZEN',
    @ActionByAdminEmail,
    NOW()
FROM
(
    SELECT WindowStart, WindowEnd
    FROM SuspiciousActivityActions
    WHERE DoctorId = @DoctorId AND ActionStatus = 'FROZEN'
    ORDER BY CreatedAt DESC
    LIMIT 1
) sa;", connection, transaction))
                            {
                                clearFrozenActionsCommand.Parameters.AddWithValue("@DoctorId", doctorId);
                                clearFrozenActionsCommand.Parameters.AddWithValue("@DoctorName", doctorName);
                                clearFrozenActionsCommand.Parameters.AddWithValue("@ActionByAdminEmail", string.IsNullOrWhiteSpace(adminEmail) ? (object)DBNull.Value : adminEmail.Trim());
                                clearFrozenActionsCommand.ExecuteNonQuery();
                            }
                        }

                        auditLogService.Log(
                            connection,
                            transaction,
                            "Admin",
                            null,
                            adminEmail,
                            "Doctor",
                            doctorId,
                            doctorName,
                            isActive ? "ACCOUNT_UNFREEZE" : "ACCOUNT_FREEZE",
                            "Security",
                            "IsActive",
                            currentIsActive ? "1" : "0",
                            isActive ? "1" : "0",
                            isActive ? "Doctor account was restored by admin." : "Doctor account was frozen by admin."
                        );

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

        private int GetPriorityRank(string priority)
        {
            switch ((priority ?? "").ToUpper())
            {
                case "HIGH PRIORITY":
                    return 3;
                case "MEDIUM PRIORITY":
                    return 2;
                case "LOW PRIORITY":
                    return 1;
                default:
                    return 0;
            }
        }

        private AdminAuditLogCard MapAuditLogCard(MySqlDataReader reader)
        {
            AdminAuditLogCard card = new AdminAuditLogCard();
            card.AuditLogId = reader.GetInt32("AuditLogId");
            card.ActorUserId = reader.IsDBNull(reader.GetOrdinal("ActorUserId")) ? (int?)null : reader.GetInt32("ActorUserId");
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

            return card;
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
