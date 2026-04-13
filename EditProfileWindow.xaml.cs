using System;
using System.Windows;
using System.Windows.Controls;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class EditProfileWindow : Window
    {
        private string currentEmail;
        private PatientDashboardService dashboardService = new PatientDashboardService();
        public string UpdatedEmail { get; private set; }

        public EditProfileWindow(string email)
        {
            InitializeComponent();
            currentEmail = email;
            UpdatedEmail = email;
            LoadPatient();
        }

        private void LoadPatient()
        {
            Patient patient = dashboardService.GetPatientByEmail(currentEmail);
            if (patient == null)
            {
                MessageBox.Show("Patient was not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            txtFirstName.Text = patient.FirstName;
            txtLastName.Text = patient.LastName;
            txtEmail.Text = patient.Email;
            txtPhone.Text = patient.Phone == "Not added" ? "" : patient.Phone;
            dpBirthDate.SelectedDate = patient.BirthDate;
            txtAddress.Text = patient.Address == "Not added" ? "" : patient.Address;
            txtCity.Text = patient.City;

            foreach (ComboBoxItem item in cmbGender.Items)
            {
                if (item.Content.ToString() == patient.Gender)
                {
                    cmbGender.SelectedItem = item;
                    break;
                }
            }

            if (cmbGender.SelectedIndex < 0)
            {
                cmbGender.SelectedIndex = 2;
            }
        }

        private void btnChangeEmail_Click(object sender, RoutedEventArgs e)
        {
            ChangeEmailWindow changeEmailWindow = new ChangeEmailWindow(currentEmail);
            changeEmailWindow.Show();
            Close();
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string address = txtAddress.Text.Trim();
            string city = txtCity.Text.Trim();
            ComboBoxItem selectedGender = cmbGender.SelectedItem as ComboBoxItem;
            string gender = selectedGender == null ? "Other" : selectedGender.Content.ToString();

            if (firstName == "" || lastName == "")
            {
                MessageBox.Show("Fill in first name and last name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpBirthDate.SelectedDate == null)
            {
                MessageBox.Show("Select date of birth.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                dashboardService.UpdatePatientProfile(currentEmail, firstName, lastName, phone, dpBirthDate.SelectedDate.Value, gender, address, city);
                PatientDashboardWindow dashboardWindow = new PatientDashboardWindow(currentEmail);
                dashboardWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            PatientDashboardWindow dashboardWindow = new PatientDashboardWindow(currentEmail);
            dashboardWindow.Show();
            Close();
        }
    }
}
