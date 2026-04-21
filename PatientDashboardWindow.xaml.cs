using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class PatientDashboardWindow : Window
    {
        private string patientEmail;
        private PatientDashboardService dashboardService = new PatientDashboardService();
        private List<VisitCard> allVisits = new List<VisitCard>();

        public PatientDashboardWindow(string email)
        {
            InitializeComponent();
            patientEmail = email;
            txtVisitSearch.Text = "Search visits...";
            txtVisitSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
            txtVisitSearch.GotFocus += TxtVisitSearch_GotFocus;
            txtVisitSearch.LostFocus += TxtVisitSearch_LostFocus;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            PatientDashboardData dashboard = dashboardService.GetDashboardByEmail(patientEmail);

            txtFullName.Text = dashboard.Patient.FullName;
            txtAge.Text = dashboard.Patient.Age + " years";
            txtGender.Text = dashboard.Patient.Gender;
            txtPatientId.Text = "#" + dashboard.Patient.Id;
            txtBloodType.Text = string.IsNullOrWhiteSpace(dashboard.Patient.BloodType) ? "Not added" : dashboard.Patient.BloodType;
            txtEmail.Text = dashboard.Patient.Email;
            txtPhone.Text = dashboard.Patient.Phone;

            string addressLine = dashboard.Patient.Address;
            if (string.IsNullOrWhiteSpace(addressLine))
            {
                addressLine = "Not added";
            }

            string cityPart = dashboard.Patient.City;
            if (!string.IsNullOrWhiteSpace(cityPart))
            {
                addressLine += ", " + cityPart;
            }
            txtAddress.Text = addressLine;

            txtDoctorName.Text = dashboard.DoctorName;
            txtDoctorSpecialization.Text = dashboard.DoctorSpecialization;
            txtAllergyCount.Text = dashboard.Allergies.Count + " recorded";
            txtChronicCount.Text = dashboard.ChronicProblems.Count + " conditions";
            txtPrescriptionCount.Text = dashboard.Prescriptions.Count + " active";

            FillSimpleList(allergiesPanel, dashboard.Allergies, txtNoAllergies);
            FillSimpleList(chronicPanel, dashboard.ChronicProblems, txtNoChronic);

            prescriptionsPanel.ItemsSource = dashboard.Prescriptions;
            txtNoPrescriptions.Visibility = dashboard.Prescriptions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            prescriptionsPanel.Visibility = dashboard.Prescriptions.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            allVisits = dashboard.Visits;
            ApplyVisitFilter();
        }

        private void FillSimpleList(ItemsControl panel, List<string> items, TextBlock emptyText)
        {
            List<Border> blocks = new List<Border>();

            for (int i = 0; i < items.Count; i++)
            {
                Border border = new Border();
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                border.BorderThickness = new Thickness(1.2);
                border.CornerRadius = new CornerRadius(10);
                border.Padding = new Thickness(14);
                border.Margin = new Thickness(0, 0, 0, 10);

                TextBlock text = new TextBlock();
                text.Text = (i + 1) + ". " + items[i];
                text.FontSize = 15;
                text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
                text.TextWrapping = TextWrapping.Wrap;

                border.Child = text;
                blocks.Add(border);
            }

            panel.ItemsSource = blocks;
            emptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            panel.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void txtVisitSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyVisitFilter();
        }

        private void ApplyVisitFilter()
        {
            if (allVisits == null)
            {
                return;
            }

            string text = txtVisitSearch.Text.Trim();
            if (text == "Search visits...")
            {
                text = "";
            }

            List<VisitCard> filteredVisits = new List<VisitCard>();

            foreach (VisitCard visit in allVisits)
            {
                if (string.IsNullOrWhiteSpace(text)
                    || visit.DoctorName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || visit.VisitType.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || visit.VisitDate.ToString("yyyy-MM-dd").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filteredVisits.Add(visit);
                }
            }

            visitsPanel.ItemsSource = filteredVisits;
            txtNoVisits.Visibility = filteredVisits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            visitsPanel.Visibility = filteredVisits.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TxtVisitSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtVisitSearch.Text == "Search visits...")
            {
                txtVisitSearch.Text = "";
                txtVisitSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
            }
        }

        private void TxtVisitSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVisitSearch.Text))
            {
                txtVisitSearch.Text = "Search visits...";
                txtVisitSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
            }
        }

        private void btnViewVisitDetails_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            VisitCard visit = button.Tag as VisitCard;
            if (visit == null)
            {
                return;
            }

            VisitDetailsWindow window = new VisitDetailsWindow(visit);
            window.Owner = this;
            window.ShowDialog();
        }

        private void btnViewPrescription_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            PrescriptionCard prescription = button.Tag as PrescriptionCard;
            if (prescription == null)
            {
                return;
            }

            PrescriptionDetailsWindow window = new PrescriptionDetailsWindow(prescription.PrescriptionId);
            window.Owner = this;
            window.ShowDialog();
        }

        private void btnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            EditProfileWindow editWindow = new EditProfileWindow(patientEmail);
            editWindow.Show();
            Close();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
