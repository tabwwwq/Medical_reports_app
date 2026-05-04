using System.Windows;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class PrescriptionDetailsWindow : Window
    {
        private PrescriptionCard prescription;
        private PrescriptionService prescriptionService = new PrescriptionService();

        public PrescriptionDetailsWindow(int prescriptionId)
        {
            InitializeComponent();
            LoadPrescription(prescriptionId);
        }

        private void LoadPrescription(int prescriptionId)
        {
            prescription = prescriptionService.GetPrescriptionById(prescriptionId);
            if (prescription == null)
            {
                MessageBox.Show("Prescription was not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            txtMedication.Text = prescription.DisplayName;
            txtDosage.Text = prescription.Dosage;
            txtQuantity.Text = prescription.Quantity;
            txtDoctor.Text = prescription.DoctorName;
            txtCreatedAt.Text = prescription.CreatedAt.ToString("yyyy-MM-dd");
            txtPrescriptionId.Text = "RX" + prescription.PrescriptionId.ToString("D5") + "-" + prescription.CreatedAt.ToString("yyyy-MM-dd");
            txtStatus.Text = string.IsNullOrWhiteSpace(prescription.Status) ? "Active" : prescription.Status;
            imgQrCode.Source = prescriptionService.GenerateQrCodeImage(prescription.QrText);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRequestRefill_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Refill request was sent for " + prescription.DisplayName + ".", "Request sent", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
