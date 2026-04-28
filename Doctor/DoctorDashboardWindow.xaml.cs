using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class DoctorDashboardWindow : Window
    {
        private string doctorEmail = "";
        private int doctorId;
        private DoctorDashboardService dashboardService = new DoctorDashboardService();
        private AvatarService avatarService = new AvatarService();
        private Doctor currentDoctor;
        private bool showingPlaceholder = false;

        public DoctorDashboardWindow(string email)
        {
            InitializeComponent();
            doctorEmail = email;
            SetPlaceholder();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            string searchText = showingPlaceholder ? "" : txtSearch.Text.Trim();
            DoctorDashboardData dashboard = dashboardService.GetDashboardByEmail(doctorEmail, searchText);

            currentDoctor = dashboard.Doctor;
            doctorId = dashboard.Doctor.Id;
            LoadDoctorAvatar(dashboard.Doctor);
            txtDoctorName.Text = "Dr. " + dashboard.Doctor.FullName;
            txtDoctorInfo.Text = dashboard.Doctor.Specialization + " • Age: " + dashboard.Doctor.Age + " • " + dashboard.Doctor.Email;
            txtSearchInfo.Text = "Patients found: " + dashboard.Patients.Count;
            RenderPatients(dashboard.Patients);
        }

        private void LoadDoctorAvatar(Doctor doctor)
        {
            txtDoctorInitials.Text = BuildInitials(doctor.FirstName, doctor.LastName);
            ImageBrush brush = avatarService.BuildAvatarBrush(doctor.AvatarUrl);
            if (brush == null)
            {
                doctorAvatarBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
                txtDoctorInitials.Visibility = Visibility.Visible;
            }
            else
            {
                doctorAvatarBorder.Background = brush;
                txtDoctorInitials.Visibility = Visibility.Collapsed;
            }
        }

        private string BuildInitials(string firstName, string lastName)
        {
            string first = string.IsNullOrWhiteSpace(firstName) ? "" : firstName.Trim().Substring(0, 1).ToUpper();
            string last = string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim().Substring(0, 1).ToUpper();
            string value = (first + last).Trim();
            return value == "" ? "DR" : value;
        }

        private void doctorAvatarBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentDoctor == null)
                {
                    return;
                }

                string filePath = avatarService.SelectImageFile();
                if (filePath == "")
                {
                    return;
                }

                string avatarUrl = avatarService.SaveDoctorAvatar(currentDoctor.Id, filePath);
                currentDoctor.AvatarUrl = avatarUrl;
                LoadDoctorAvatar(currentDoctor);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderPatients(List<DoctorDashboardPatientCard> patients)
        {
            patientsPanel.Children.Clear();

            if (patients.Count == 0)
            {
                Border emptyCard = new Border();
                emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                emptyCard.BorderThickness = new Thickness(1.2);
                emptyCard.CornerRadius = new CornerRadius(10);
                emptyCard.Padding = new Thickness(20);
                emptyCard.Child = new TextBlock
                {
                    Text = "No patients were found.",
                    FontSize = 15,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                };
                patientsPanel.Children.Add(emptyCard);
                return;
            }

            foreach (DoctorDashboardPatientCard patient in patients)
            {
                patientsPanel.Children.Add(CreatePatientCard(patient));
            }
        }

        private Border CreatePatientCard(DoctorDashboardPatientCard patient)
        {
            Border card = new Border();
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));
            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            card.BorderThickness = new Thickness(1.2);
            card.CornerRadius = new CornerRadius(10);
            card.Padding = new Thickness(18);
            card.Margin = new Thickness(0, 0, 0, 14);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            Border avatar = new Border();
            avatar.Width = 54;
            avatar.Height = 54;
            avatar.CornerRadius = new CornerRadius(27);
            avatar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB"));
            avatar.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC5CAD3"));
            avatar.BorderThickness = new Thickness(1.5);
            avatar.HorizontalAlignment = HorizontalAlignment.Left;
            avatar.VerticalAlignment = VerticalAlignment.Center;
            ImageBrush patientBrush = avatarService.BuildAvatarBrush(patient.AvatarUrl);
            if (patientBrush == null)
            {
                avatar.Child = new TextBlock
                {
                    Text = "👤",
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else
            {
                avatar.Background = patientBrush;
            }
            grid.Children.Add(avatar);

            StackPanel infoPanel = new StackPanel();
            Grid.SetColumn(infoPanel, 1);
            infoPanel.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(infoPanel);

            infoPanel.Children.Add(new TextBlock
            {
                Text = patient.FullName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = "Age: " + patient.Age + " • Last visit: " + patient.LastVisitText,
                FontSize = 14,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4B5563"))
            });

            StackPanel buttonsPanel = new StackPanel();
            buttonsPanel.Orientation = Orientation.Horizontal;
            buttonsPanel.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(buttonsPanel, 2);
            grid.Children.Add(buttonsPanel);

            Button editButton = new Button();
            editButton.Content = "✎ Edit";
            editButton.Style = (Style)FindResource("ActionBlueButtonStyle");
            editButton.Tag = patient.PatientId;
            editButton.Click += btnEditPatient_Click;
            buttonsPanel.Children.Add(editButton);

            return card;
        }

        private void btnViewPatient_Click(object sender, RoutedEventArgs e)
        {
            int patientId = Convert.ToInt32(((Button)sender).Tag);
            DoctorPatientProfileWindow window = new DoctorPatientProfileWindow(patientId, doctorId, false);
            window.Owner = this;
            window.ShowDialog();
            LoadDashboard();
        }

        private void btnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            int patientId = Convert.ToInt32(((Button)sender).Tag);
            DoctorPatientProfileWindow window = new DoctorPatientProfileWindow(patientId, doctorId, true);
            window.Owner = this;
            window.ShowDialog();
            LoadDashboard();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || showingPlaceholder)
            {
                return;
            }
            LoadDashboard();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void SetPlaceholder()
        {
            showingPlaceholder = true;
            txtSearch.Text = "Search patients...";
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!showingPlaceholder)
            {
                return;
            }

            showingPlaceholder = false;
            txtSearch.Text = "";
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                return;
            }
            SetPlaceholder();
            LoadDashboard();
        }
    }
}
