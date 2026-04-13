using System;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class Data
    {
        private string connectionString =
            "server=127.0.0.1;port=3306;user=root;password=;database=medical_reports_app;SslMode=None;";

        private bool schemaChecked = false;

        public MySqlConnection GetConnection()
        {
            EnsureSchema();
            return new MySqlConnection(connectionString);
        }

        public int ExecuteInsert(string query, Action<MySqlCommand> addParameters)
        {
            using (MySqlConnection connection = GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    addParameters(command);
                    connection.Open();
                    command.ExecuteNonQuery();
                    return (int)command.LastInsertedId;
                }
                catch (Exception ex)
                {
                    throw new Exception("Database insert error: " + ex.Message);
                }
            }
        }

        public int ExecuteNonQuery(string query, Action<MySqlCommand> addParameters)
        {
            using (MySqlConnection connection = GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    addParameters(command);
                    connection.Open();
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("Database query error: " + ex.Message);
                }
            }
        }

        public object ExecuteScalar(string query, Action<MySqlCommand> addParameters)
        {
            using (MySqlConnection connection = GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    addParameters(command);
                    connection.Open();
                    return command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new Exception("Database scalar error: " + ex.Message);
                }
            }
        }

        private void EnsureSchema()
        {
            if (schemaChecked)
            {
                return;
            }

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            using (MySqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"
CREATE TABLE IF NOT EXISTS Doctors (
    DoctorId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    FirstName VARCHAR(100) NULL,
    LastName VARCHAR(100) NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Gender VARCHAR(30) NULL,
    BirthDate DATE NULL,
    Specialization VARCHAR(150) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive TINYINT(1) NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS PendingPatients (
    PendingPatientId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    VerificationCode VARCHAR(20) NOT NULL,
    RoleName VARCHAR(30) NOT NULL DEFAULT 'Patient',
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsVerified TINYINT(1) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS PendingPasswordResets (
    PendingPasswordResetId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL,
    RoleName VARCHAR(30) NOT NULL,
    VerificationCode VARCHAR(20) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UK_PendingPasswordResets_EmailRole (Email, RoleName)
);

ALTER TABLE Patients ADD COLUMN IF NOT EXISTS City VARCHAR(100) NULL AFTER Address;
ALTER TABLE Patients ADD COLUMN IF NOT EXISTS BloodType VARCHAR(10) NULL AFTER City;
ALTER TABLE PendingPatients ADD COLUMN IF NOT EXISTS RoleName VARCHAR(30) NOT NULL DEFAULT 'Patient' AFTER VerificationCode;
ALTER TABLE Doctors ADD COLUMN IF NOT EXISTS Specialization VARCHAR(150) NULL AFTER BirthDate;
ALTER TABLE Doctors ADD COLUMN IF NOT EXISTS Gender VARCHAR(30) NULL AFTER PasswordHash;
ALTER TABLE Doctors ADD COLUMN IF NOT EXISTS BirthDate DATE NULL AFTER Gender;
ALTER TABLE Doctors ADD COLUMN IF NOT EXISTS IsActive TINYINT(1) NOT NULL DEFAULT 1 AFTER CreatedAt;
";
                command.ExecuteNonQuery();
            }

            schemaChecked = true;
        }
    }
}
