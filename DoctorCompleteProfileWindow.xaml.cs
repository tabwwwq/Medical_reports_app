using System;
using System.Windows;
using System.Windows.Controls;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class DoctorCompleteProfileWindow : Window
    {
        private AuthService authService = new AuthService();
        private string doctorEmail = "";

        public DoctorCompleteProfileWindow(string email)
        {
            InitializeComponent();
            doctorEmail = email;
            dpBirthDate.SelectedDate = new DateTime(1990, 1, 1);
            cmbGender.SelectedIndex = 2;
            txtSpecialization.Text = "General doctor";
        }

        private void btnCompleteProfile_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string specialization = txtSpecialization.Text.Trim();
            string gender = GetComboValue(cmbGender);
            DateTime? birthDate = dpBirthDate.SelectedDate;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(specialization) || string.IsNullOrWhiteSpace(gender) || birthDate == null)
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
                authService.UpdateDoctorProfileAfterRegistration(doctorEmail, firstName, lastName, gender, birthDate.Value.Date, specialization);
                DoctorDashboardWindow dashboardWindow = new DoctorDashboardWindow(doctorEmail);
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
            DoctorDashboardWindow dashboardWindow = new DoctorDashboardWindow(doctorEmail);
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
