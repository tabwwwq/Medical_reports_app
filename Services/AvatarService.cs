using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MySqlConnector;

namespace MedicalReportsApp.Services
{
    public class AvatarService
    {
        private Data data = new Data();

        public string SelectImageFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
            dialog.Multiselect = false;
            return dialog.ShowDialog() == true ? dialog.FileName : "";
        }

        public string SavePatientAvatar(int patientId, string sourcePath)
        {
            string relativePath = SaveAvatarFile("pid", patientId, sourcePath);
            EnsureAvatarColumns();
            data.ExecuteNonQuery("UPDATE Patients SET AvatarUrl = @AvatarUrl WHERE PatientId = @PatientId;", command =>
            {
                command.Parameters.AddWithValue("@AvatarUrl", relativePath);
                command.Parameters.AddWithValue("@PatientId", patientId);
            });
            return relativePath;
        }

        public string SaveDoctorAvatar(int doctorId, string sourcePath)
        {
            string relativePath = SaveAvatarFile("did", doctorId, sourcePath);
            EnsureAvatarColumns();
            data.ExecuteNonQuery("UPDATE Doctors SET AvatarUrl = @AvatarUrl WHERE DoctorId = @DoctorId;", command =>
            {
                command.Parameters.AddWithValue("@AvatarUrl", relativePath);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
            });
            return relativePath;
        }

        public ImageBrush BuildAvatarBrush(string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return null;
            }

            string fullPath = GetFullPath(avatarUrl);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(fullPath);

                BitmapImage bitmap = new BitmapImage();
                using (MemoryStream stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                ImageBrush brush = new ImageBrush(bitmap);
                brush.Stretch = Stretch.UniformToFill;
                return brush;
            }
            catch
            {
                return null;
            }
        }

        public void EnsureAvatarColumns()
        {
            using (MySqlConnection connection = data.GetConnection())
            {
                connection.Open();
                EnsureColumn(connection, "Patients", "AvatarUrl");
                EnsureColumn(connection, "Doctors", "AvatarUrl");
            }
        }

        private void EnsureColumn(MySqlConnection connection, string tableName, string columnName)
        {
            using (MySqlCommand check = new MySqlCommand(@"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @TableName
  AND COLUMN_NAME = @ColumnName;", connection))
            {
                check.Parameters.AddWithValue("@TableName", tableName);
                check.Parameters.AddWithValue("@ColumnName", columnName);
                int count = Convert.ToInt32(check.ExecuteScalar());
                if (count > 0)
                {
                    return;
                }
            }

            using (MySqlCommand command = new MySqlCommand("ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " VARCHAR(500) NULL;", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private string SaveAvatarFile(string prefix, int id, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new Exception("Image file was not found.");
            }

            string extension = Path.GetExtension(sourcePath).ToLower();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".bmp" && extension != ".gif")
            {
                throw new Exception("Select a valid image file.");
            }

            string folder = Path.Combine(GetProjectFolder(), "Avatars");
            Directory.CreateDirectory(folder);
            string fileName = prefix + id + extension;
            string destination = Path.Combine(folder, fileName);
            File.Copy(sourcePath, destination, true);
            return "Avatars/" + fileName;
        }

        private string GetProjectFolder()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "MedicalReportsApp.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            string localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MedicalReportsApp");
            Directory.CreateDirectory(localFolder);
            return localFolder;
        }

        private string GetFullPath(string avatarUrl)
        {
            if (Path.IsPathRooted(avatarUrl))
            {
                return avatarUrl;
            }

            string cleanPath = avatarUrl.Replace('/', Path.DirectorySeparatorChar);
            string projectPath = Path.Combine(GetProjectFolder(), cleanPath);
            if (File.Exists(projectPath))
            {
                return projectPath;
            }

            string baseDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cleanPath);
            if (File.Exists(baseDirectoryPath))
            {
                return baseDirectoryPath;
            }

            return Path.Combine(Environment.CurrentDirectory, cleanPath);
        }
    }
}
