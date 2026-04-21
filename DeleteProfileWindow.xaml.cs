using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class DeleteProfileWindow : Window
    {
        private readonly string currentEmail;
        private readonly PatientDashboardService dashboardService = new PatientDashboardService();
        private readonly EmailService emailService = new EmailService();
        private bool codeSent;

        public DeleteProfileWindow(string email)
        {
            InitializeComponent();
            currentEmail = email;
            txtEmailValue.Text = email;
        }

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            if (!codeSent)
            {
                SendCode();
                return;
            }

            ConfirmDelete();
        }

        private void SendCode()
        {
            try
            {
                string code = Tools.VerificationCodeHelper.GenerateCode();
                dashboardService.StartDeleteProfile(currentEmail, code);
                emailService.SendDeleteProfileCode(currentEmail, code);
                codeSent = true;
                successBorder.Visibility = Visibility.Visible;
                dangerBorder.Visibility = Visibility.Visible;
                codePanel.Visibility = Visibility.Visible;
                btnAction.Content = "Delete Profile";
                btnBack.Content = "Back";
                btnResend.Visibility = Visibility.Visible;
                txtSuccessInfo.Text = "Check: " + currentEmail;
                txtError.Visibility = Visibility.Collapsed;
                txtCode.Text = "";
                txtCode.Focus();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ConfirmDelete()
        {
            string code = txtCode.Text.Trim();

            if (!Regex.IsMatch(code, "^[0-9]{6}$"))
            {
                ShowError("Enter a valid 6-digit code.");
                return;
            }

            try
            {
                bool ok = dashboardService.DeletePatientProfileWithCode(currentEmail, code);

                if (!ok)
                {
                    ShowError("Incorrect code.");
                    return;
                }
                MessageBox.Show("Profile and related data were deleted from the database.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
            SendCode();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (!codeSent)
            {
                Close();
                return;
            }

            codeSent = false;
            successBorder.Visibility = Visibility.Collapsed;
            dangerBorder.Visibility = Visibility.Collapsed;
            codePanel.Visibility = Visibility.Collapsed;
            btnAction.Content = "Send Code";
            btnBack.Content = "Cancel";
            btnResend.Visibility = Visibility.Collapsed;
            txtCode.Text = "";
            txtError.Visibility = Visibility.Collapsed;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
