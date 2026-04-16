using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MedicalReportsApp.Services;
using QRCoder;

namespace MedicalReportsApp
{
    public partial class TwoFactorSetupWindow : Window
    {
        private readonly string currentEmail;
        private readonly string secretKey;
        private readonly TwoFactorAuthService service = new TwoFactorAuthService();

        public TwoFactorSetupWindow(string email)
        {
            InitializeComponent();
            currentEmail = email;
            secretKey = service.StartSetup(email);

            txtAccount.Text = "Account: " + email;
            txtSecretKey.Text = secretKey;
            imgQrCode.Source = BuildQrCodeImage(service.BuildOtpAuthUri(email, secretKey));

            txtCode.PreviewTextInput += txtCode_PreviewTextInput;
            txtCode.Focus();
        }

        private void btnEnable_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;
            string code = txtCode.Text.Trim();

            if (!Regex.IsMatch(code, "^[0-9]{6}$"))
            {
                ShowError("Enter a valid 6-digit code.");
                return;
            }

            try
            {
                bool ok = service.Enable(currentEmail, code);
                if (!ok)
                {
                    ShowError("Incorrect code.");
                    return;
                }

                MessageBox.Show("Google Authenticator was connected successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private BitmapImage BuildQrCodeImage(string payload)
        {
            using (QRCodeGenerator generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            {
                PngByteQRCode qrCode = new PngByteQRCode(data);
                byte[] imageBytes = qrCode.GetGraphic(20);
                using (MemoryStream stream = new MemoryStream(imageBytes))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
