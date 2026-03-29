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
            MessageBox.Show("Password recovery is not available yet.", "Forgot Password",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
