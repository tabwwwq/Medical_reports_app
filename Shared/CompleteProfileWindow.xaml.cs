using System;
using System.Windows;
using System.Windows.Controls;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class CompleteProfileWindow : Window
    {
        private AuthService authService = new AuthService();
        private string patientEmail = "";

        public CompleteProfileWindow(string email)
        {
            InitializeComponent();
            patientEmail = email;
            dpBirthDate.SelectedDate = new DateTime(2000, 1, 1);
            cmbGender.SelectedIndex = 2;
            cmbBloodType.SelectedIndex = 0;
        }

        private void btnCompleteProfile_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string address = txtAddress.Text.Trim();
            string city = txtCity.Text.Trim();
            string bloodType = GetComboValue(cmbBloodType);
            string gender = GetComboValue(cmbGender);
            DateTime? birthDate = dpBirthDate.SelectedDate;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city))
            {
                ShowError("Fill in all required fields.");
                return;
            }

            if (string.IsNullOrWhiteSpace(bloodType) || string.IsNullOrWhiteSpace(gender) || birthDate == null)
            {
                ShowError("Fill in all required fields.");
                return;
            }

            if (birthDate.Value.Date >= DateTime.Today)
            {
                ShowError("Enter a correct date of birth.");
                return;
            }

            try
            {
                authService.UpdatePatientProfileAfterRegistration(patientEmail, firstName, lastName, phone, address, city, bloodType, gender, birthDate.Value.Date);
                PatientDashboardWindow dashboardWindow = new PatientDashboardWindow(patientEmail);
                dashboardWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            PatientDashboardWindow dashboardWindow = new PatientDashboardWindow(patientEmail);
            dashboardWindow.Show();
            Close();
        }

        private string GetComboValue(ComboBox comboBox)
        {
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            if (item == null)
            {
                return "";
            }
            return item.Content.ToString();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;
        }
    }
}
