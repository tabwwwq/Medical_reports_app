using System;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class Data
    {
        private string connectionString =
            "server=127.0.0.1;port=3306;user=root;password=;database=medical_reports_app;SslMode=None;";

        public MySqlConnection GetConnection()
        {
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
    }
}