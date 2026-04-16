using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class TwoFactorLoginWindow : Window
    {
        private readonly string currentEmail;
        private readonly TwoFactorAuthService service = new TwoFactorAuthService();

        public TwoFactorLoginWindow(string email)
        {
            InitializeComponent();
            currentEmail = email;
            txtCode.PreviewTextInput += txtCode_PreviewTextInput;
            txtCode.Focus();
        }

        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;
            string code = txtCode.Text.Trim();

            if (!Regex.IsMatch(code, "^[0-9]{6}$"))
            {
                ShowError("Enter a valid 6-digit code.");
                return;
            }

            bool ok = service.VerifyLoginCode(currentEmail, code);
            if (!ok)
            {
                ShowError("Incorrect code.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
