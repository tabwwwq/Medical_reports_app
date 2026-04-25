using System;
using System.IO;
using System.Windows.Media.Imaging;
using MedicalReportsApp.Classes;
using MySqlConnector;
using QRCoder;

namespace MedicalReportsApp.Services
{
    public class PrescriptionService
    {
        private Data data = new Data();

        public PrescriptionCard GetPrescriptionById(int prescriptionId)
        {
            string query = @"
SELECT p.PrescriptionId,
       p.PatientId,
       p.DoctorId,
       p.Name,
       p.Dosage,
       p.Quantity,
       p.Status,
       p.CreatedAt,
       d.FirstName,
       d.LastName
FROM Prescriptions p
LEFT JOIN Doctors d ON p.DoctorId = d.DoctorId
WHERE p.PrescriptionId = @PrescriptionId;";

            using (MySqlConnection connection = data.GetConnection())
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    PrescriptionCard card = new PrescriptionCard();
                    card.PrescriptionId = reader.GetInt32("PrescriptionId");
                    card.PatientId = reader.GetInt32("PatientId");
                    card.DoctorId = reader.IsDBNull(reader.GetOrdinal("DoctorId")) ? null : (int?)reader.GetInt32("DoctorId");
                    card.Name = reader.GetString("Name");
                    card.MedicineName = card.Name;
                    card.Dosage = reader.GetString("Dosage");
                    card.Quantity = reader.GetString("Quantity");
                    card.Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Active" : reader.GetString("Status");
                    card.CreatedAt = reader.GetDateTime("CreatedAt");

                    string firstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? "" : reader.GetString("FirstName");
                    string lastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? "" : reader.GetString("LastName");
                    string doctorName = (firstName + " " + lastName).Trim();
                    card.DoctorName = string.IsNullOrWhiteSpace(doctorName) ? "Not specified" : "Dr. " + doctorName;
                    return card;
                }
            }
        }

        public BitmapImage GenerateQrCodeImage(string qrText)
        {
            using (QRCodeGenerator generator = new QRCodeGenerator())
            using (QRCodeData qrData = generator.CreateQrCode(qrText ?? "", QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
            {
                byte[] bytes = qrCode.GetGraphic(16);
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
    }
}
