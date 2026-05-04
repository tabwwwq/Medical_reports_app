using System;
using System.Text.RegularExpressions;
using System.Windows;
using MedicalReportsApp.Services;
using MedicalReportsApp.Tools;

namespace MedicalReportsApp
{
    public partial class ChangeEmailWindow : Window
    {
        private string currentEmail;
        private string pendingNewEmail = "";
        private string verificationCode = "";
        private PatientDashboardService dashboardService = new PatientDashboardService();
        private EmailService emailService = new EmailService();

        public string UpdatedEmail { get; private set; }

        public ChangeEmailWindow(string email)
        {
            InitializeComponent();
            currentEmail = email;
            UpdatedEmail = email;
            txtCurrentEmail.Text = email;
        }

        private void btnSendCode_Click(object sender, RoutedEventArgs e)
        {
            SendVerificationCode();
        }

        private void btnResendCode_Click(object sender, RoutedEventArgs e)
        {
            SendVerificationCode();
        }

        private void SendVerificationCode()
        {
            string newEmail = txtNewEmail.Text.Trim();
            string password = txtCurrentPassword.Password;

            if (!Regex.IsMatch(newEmail, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                MessageBox.Show("Enter a valid email address.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Enter a different email address.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter your current password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!dashboardService.VerifyPatientPassword(currentEmail, password))
                {
                    MessageBox.Show("Current password is incorrect.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dashboardService.PatientEmailExists(newEmail))
                {
                    MessageBox.Show("This email is already used by another patient.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                verificationCode = VerificationCodeHelper.GenerateCode();
                pendingNewEmail = newEmail;
                emailService.SendEmailChangeCode(newEmail, verificationCode);

                txtVerificationInfo.Text = "Check your email: " + pendingNewEmail;
                verificationPanel.Visibility = Visibility.Visible;
                successBorder.Visibility = Visibility.Visible;
                txtVerificationCode.Text = "";
                txtVerificationCode.Focus();
                txtNewEmail.IsEnabled = false;
                txtCurrentPassword.IsEnabled = false;
                btnSendCode.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            verificationCode = "";
            pendingNewEmail = "";
            verificationPanel.Visibility = Visibility.Collapsed;
            successBorder.Visibility = Visibility.Collapsed;
            txtVerificationCode.Text = "";
            txtNewEmail.IsEnabled = true;
            txtCurrentPassword.IsEnabled = true;
            btnSendCode.Visibility = Visibility.Visible;
        }

        private void btnConfirmChange_Click(object sender, RoutedEventArgs e)
        {
            string code = txtVerificationCode.Text.Trim();

            if (pendingNewEmail == "" || verificationCode == "")
            {
                MessageBox.Show("Send the verification code first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (code == "")
            {
                MessageBox.Show("Enter the verification code.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (code != verificationCode)
            {
                MessageBox.Show("Verification code is incorrect.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                dashboardService.ChangePatientEmail(currentEmail, pendingNewEmail);
                UpdatedEmail = pendingNewEmail;
                EditProfileWindow editProfileWindow = new EditProfileWindow(UpdatedEmail);
                editProfileWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            EditProfileWindow editProfileWindow = new EditProfileWindow(currentEmail);
            editProfileWindow.Show();
            Close();
        }
    }
}
