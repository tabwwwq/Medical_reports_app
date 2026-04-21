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
        private TwoFactorAuthService twoFactorAuthService = new TwoFactorAuthService();
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

            LoadTwoFactorStatus();
        }

        private void LoadTwoFactorStatus()
        {
            try
            {
                bool isEnabled = twoFactorAuthService.IsTwoFactorEnabled(currentEmail);
                txtTwoFactorStatus.Text = isEnabled ? "Status: Enabled" : "Status: Disabled";
                btnGoogleAuth.Content = isEnabled ? "Disconnect" : "Connect";
            }
            catch
            {
                txtTwoFactorStatus.Text = "Status: Disabled";
                btnGoogleAuth.Content = "Connect";
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

        private void btnGoogleAuth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (twoFactorAuthService.IsTwoFactorEnabled(currentEmail))
                {
                    if (MessageBox.Show("Disconnect Google Authenticator from this account?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        twoFactorAuthService.Disable(currentEmail);
                        LoadTwoFactorStatus();
                        MessageBox.Show("Google Authenticator was disconnected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    return;
                }

                TwoFactorSetupWindow setupWindow = new TwoFactorSetupWindow(currentEmail);
                setupWindow.Owner = this;
                bool? result = setupWindow.ShowDialog();

                if (result == true)
                {
                    LoadTwoFactorStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            DeleteProfileWindow deleteWindow = new DeleteProfileWindow(currentEmail);
            deleteWindow.Owner = this;

            bool? result = deleteWindow.ShowDialog();

            if (result == true)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
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
