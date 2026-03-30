using System;
using System.Windows;
using System.Windows.Controls;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class CompleteProfileWindow : Window
    {
        private readonly AuthService authService = new AuthService();
        private readonly string patientEmail;

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

            string phone = txtPhone.Text.Trim();
            string address = txtAddress.Text.Trim();
            string city = txtCity.Text.Trim();
            string bloodType = GetComboValue(cmbBloodType);
            string gender = GetComboValue(cmbGender);
            DateTime? birthDate = dpBirthDate.SelectedDate;

            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city))
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
                authService.UpdatePatientProfileAfterRegistration(patientEmail, phone, address, city, bloodType, gender, birthDate.Value.Date);
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
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
