using System;
using System.Text.RegularExpressions;
using System.Windows;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class LoginWindow : Window
    {
        private AuthService authService = new AuthService();
        private string selectedRole = "Patient";
        private bool resetCodeWasSent = false;
        private string resetEmail = "";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnPatientTab_Click(object sender, RoutedEventArgs e)
        {
            selectedRole = "Patient";
            btnPatientTab.Style = (Style)FindResource("TabActiveStyle");
            btnDoctorTab.Style = (Style)FindResource("TabInactiveStyle");
            ClearError();
        }

        private void btnDoctorTab_Click(object sender, RoutedEventArgs e)
        {
            selectedRole = "Doctor";
            btnDoctorTab.Style = (Style)FindResource("TabActiveStyle");
            btnPatientTab.Style = (Style)FindResource("TabInactiveStyle");
            ClearError();
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (!IsValidEmail(email))
            {
                ShowError("Enter a correct email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Enter your password.");
                return;
            }

            try
            {
                bool ok = authService.Login(email, password, selectedRole);

                if (!ok)
                {
                    ShowError("Incorrect email or password.");
                    return;
                }

                SuccessWindow successWindow = new SuccessWindow();
                successWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ClearError();
            txtResetEmail.Text = txtEmail.Text.Trim();
            loginPanel.Visibility = Visibility.Collapsed;
            resetPanel.Visibility = Visibility.Visible;
            txtResetCode.Text = "";
            txtNewPassword.Password = "";
            txtRepeatNewPassword.Password = "";
            txtResetInfo.Text = "Enter your email and get a code to change the password.";
            txtResetEmail.IsEnabled = true;
            btnSendResetCode.Content = "Send Code";
            btnSendResetCode.Style = (Style)FindResource("SignInButtonStyle");
            resetCodeWasSent = false;
            resetEmail = "";
        }

        private void btnSendResetCode_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            string email = txtResetEmail.Text.Trim();

            if (!IsValidEmail(email))
            {
                ShowError("Enter a correct email address.");
                return;
            }

            try
            {
                authService.StartPasswordReset(email, selectedRole);
                resetCodeWasSent = true;
                resetEmail = email;
                txtResetEmail.IsEnabled = false;
                btnSendResetCode.Content = "Code Sent";
                btnSendResetCode.Style = (Style)FindResource("GreenButtonStyle");
                txtResetInfo.Text = "Check your email: " + resetEmail;
                txtResetCode.Focus();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            string email = txtResetEmail.Text.Trim();
            string code = txtResetCode.Text.Trim();
            string newPassword = txtNewPassword.Password;
            string repeatPassword = txtRepeatNewPassword.Password;

            if (!resetCodeWasSent)
            {
                ShowError("First send the verification code.");
                return;
            }

            if (email != resetEmail)
            {
                ShowError("Use the same email where the code was sent.");
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Enter the verification code.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(repeatPassword))
            {
                ShowError("Enter the new password in both fields.");
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowError("Password must contain at least 6 characters.");
                return;
            }

            if (newPassword != repeatPassword)
            {
                ShowError("Passwords are not the same.");
                return;
            }

            try
            {
                bool ok = authService.VerifyCodeAndChangePassword(email, selectedRole, code, newPassword);

                if (!ok)
                {
                    ShowError("Wrong verification code.");
                    return;
                }

                MessageBox.Show("Password changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                btnBackToSignIn_Click(sender, e);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnBackToSignIn_Click(object sender, RoutedEventArgs e)
        {
            ClearError();
            loginPanel.Visibility = Visibility.Visible;
            resetPanel.Visibility = Visibility.Collapsed;
            txtResetCode.Text = "";
            txtNewPassword.Password = "";
            txtRepeatNewPassword.Password = "";
            txtResetEmail.IsEnabled = true;
            btnSendResetCode.Content = "Send Code";
            btnSendResetCode.Style = (Style)FindResource("SignInButtonStyle");
            resetCodeWasSent = false;
            resetEmail = "";
        }

        private void btnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            MainWindow registerWindow = new MainWindow();
            registerWindow.Show();
            Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }
    }
}
