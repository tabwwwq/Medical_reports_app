using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class MainWindow : Window
    {
        private AuthService authService = new AuthService();
        private string savedEmail = "";
        private string savedPassword = "";
        private bool codeWasSent = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSendCode_Click(object sender, RoutedEventArgs e)
        {
            ClearMessages();

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string repeatPassword = txtRepeatPassword.Password;

            if (!IsValidEmail(email))
            {
                ShowError("Enter a correct email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(repeatPassword))
            {
                ShowError("Enter password in both fields.");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Password must contain at least 6 characters.");
                return;
            }

            if (password != repeatPassword)
            {
                txtPasswordMessage.Text = "Passwords are not the same.";
                ShowError("Passwords are not the same.");
                return;
            }

            try
            {
                authService.StartRegistration(email, password);

                savedEmail = email;
                savedPassword = password;
                codeWasSent = true;

                FreezeRegisterFields(true);
                verificationPanel.Visibility = Visibility.Visible;
                txtVerificationInfo.Text = "Check your email: " + savedEmail;
                txtCode.Focus();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            ClearMessages();

            string code = txtCode.Text.Trim();

            if (!codeWasSent)
            {
                ShowError("First send the verification code.");
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Enter the verification code.");
                return;
            }

            try
            {
                bool ok = authService.VerifyCodeAndCreatePatient(savedEmail, code);

                if (!ok)
                {
                    ShowError("Wrong verification code.");
                    return;
                }

                SuccessWindow window = new SuccessWindow();
                window.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            verificationPanel.Visibility = Visibility.Collapsed;
            txtCode.Text = "";
            FreezeRegisterFields(false);
            codeWasSent = false;
            savedEmail = "";
            savedPassword = "";
            ClearMessages();
        }

        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(savedEmail) || string.IsNullOrWhiteSpace(savedPassword))
            {
                ShowError("Fill registration fields first.");
                return;
            }

            try
            {
                authService.StartRegistration(savedEmail, savedPassword);
                txtVerificationInfo.Text = "New code was sent to: " + savedEmail;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void FreezeRegisterFields(bool freeze)
        {
            txtEmail.IsEnabled = !freeze;
            txtPassword.IsEnabled = !freeze;
            txtRepeatPassword.IsEnabled = !freeze;
            btnSendCode.Visibility = freeze ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void ClearMessages()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;
            txtPasswordMessage.Text = "";
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }
    }
}