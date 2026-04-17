using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class DoctorPatientProfileWindow : Window
    {
        private int patientId;
        private int doctorId;
        private bool editMode;
        private DoctorDashboardService dashboardService = new DoctorDashboardService();
        private DoctorPatientDetailsData currentDetails;
        private EmailService emailService = new EmailService();
        private List<string> editableAllergies = new List<string>();
        private List<string> editableChronicProblems = new List<string>();
        private List<string> editablePrescriptions = new List<string>();

        public DoctorPatientProfileWindow(int patientIdValue, int doctorIdValue, bool isEditMode)
        {
            InitializeComponent();
            patientId = patientIdValue;
            doctorId = doctorIdValue;
            editMode = isEditMode;
            LoadPatient();
            ApplyMode();
        }

        private void LoadPatient()
        {
            currentDetails = dashboardService.GetPatientDetails(patientId);

            txtFirstName.Text = currentDetails.Patient.FirstName;
            txtLastName.Text = currentDetails.Patient.LastName;
            txtAge.Text = CalculateAgeText(currentDetails.Patient.BirthDate);
            txtPatientId.Text = "#" + currentDetails.Patient.Id;

            editableAllergies = new List<string>(currentDetails.Allergies);
            editableChronicProblems = new List<string>(currentDetails.ChronicProblems);
            editablePrescriptions = currentDetails.Prescriptions
                .Select(p => p.MedicineName + " - " + p.Dosage + " - " + p.Frequency)
                .ToList();

            FillVisitInformation();
            LoadRecentVisits();
            RenderStringItems(allergiesPanel, editableAllergies, "Add new allergy...", RemoveAllergy_Click);
            RenderStringItems(chronicPanel, editableChronicProblems, "Add new chronic problem...", RemoveChronic_Click);
            RenderStringItems(prescriptionsPanel, editablePrescriptions, "Add new prescription...", RemovePrescription_Click);

            txtNewAllergy.Text = "";
            txtNewChronic.Text = "";
            txtNewPrescription.Text = "";
        }

        private void LoadRecentVisits()
        {
            doctorVisitsPanel.ItemsSource = currentDetails.Visits;
            txtNoRecentVisits.Visibility = currentDetails.Visits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            doctorVisitsPanel.Visibility = currentDetails.Visits.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void FillVisitInformation()
        {
            if (editMode)
            {
                InitializeNewVisitForm();
                return;
            }

            if (currentDetails.Visits.Count > 0)
            {
                VisitCard latestVisit = currentDetails.Visits[0];
                dpVisitDate.SelectedDate = latestVisit.VisitDate;
                txtReason.Text = latestVisit.VisitType;
                txtDiagnosis.Text = string.IsNullOrWhiteSpace(latestVisit.Diagnosis) ? "" : latestVisit.Diagnosis;
                txtNotes.Text = string.IsNullOrWhiteSpace(latestVisit.Notes) ? "" : latestVisit.Notes;
            }
            else
            {
                InitializeNewVisitForm();
            }
        }

        private void InitializeNewVisitForm()
        {
            dpVisitDate.SelectedDate = DateTime.Today;
            txtReason.Text = "";
            txtDiagnosis.Text = "";
            txtNotes.Text = "";
        }

        private void ApplyMode()
        {
            txtWindowTitle.Text = editMode ? "Edit Patient Record" : "Patient Record";
            btnSave.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Content = editMode ? "✕  Cancel" : "✕  Close";
            txtFirstName.IsReadOnly = !editMode;
            txtLastName.IsReadOnly = !editMode;
            txtReason.IsReadOnly = !editMode;
            txtDiagnosis.IsReadOnly = !editMode;
            txtNotes.IsReadOnly = !editMode;
            dpVisitDate.IsEnabled = editMode;
            txtNewAllergy.IsReadOnly = !editMode;
            txtNewChronic.IsReadOnly = !editMode;
            txtNewPrescription.IsReadOnly = !editMode;
        }

        private void RenderStringItems(Panel panel, List<string> items, string placeholder, RoutedEventHandler removeHandler)
        {
            panel.Children.Clear();

            if (items.Count == 0)
            {
                Border emptyBorder = CreateItemBorder(placeholder, false, removeHandler);
                panel.Children.Add(emptyBorder);
                return;
            }

            foreach (string item in items)
            {
                panel.Children.Add(CreateItemBorder(item, editMode, removeHandler));
            }
        }

        private Border CreateItemBorder(string text, bool showRemoveButton, RoutedEventHandler removeHandler)
        {
            Border border = new Border();
            border.Background = new SolidColorBrush(Colors.White);
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            border.BorderThickness = new Thickness(1.2);
            border.CornerRadius = new CornerRadius(8);
            border.Padding = new Thickness(14, 10, 14, 10);
            border.Margin = new Thickness(0, 0, 0, 10);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            border.Child = grid;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.FontSize = 14;
            textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(showRemoveButton ? "#FF111827" : "#FF9CA3AF"));
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(textBlock);

            if (showRemoveButton)
            {
                Button button = new Button();
                button.Content = "×";
                button.Tag = text;
                button.Style = (Style)FindResource("RemoveItemButtonStyle");
                button.Click += removeHandler;
                Grid.SetColumn(button, 1);
                grid.Children.Add(button);
            }

            return border;
        }

        private string CalculateAgeText(DateTime birthDate)
        {
            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }
            return age + " years";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            HideError();
            CommitPendingListInputs();

            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            DateTime visitDate = dpVisitDate.SelectedDate ?? DateTime.Today;
            string reason = txtReason.Text.Trim();
            string diagnosis = txtDiagnosis.Text.Trim();
            string notes = txtNotes.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ShowError("Fill in first name and last name.");
                return;
            }

            PatientRecordChangeSummary summary = BuildChangeSummary(firstName, lastName, visitDate, reason, diagnosis, notes);

            try
            {
                dashboardService.SavePatientRecord(
                    patientId,
                    doctorId,
                    firstName,
                    lastName,
                    visitDate,
                    reason,
                    diagnosis,
                    notes,
                    editableAllergies,
                    editableChronicProblems,
                    editablePrescriptions
                );

                try
                {
                    emailService.SendPatientRecordUpdateEmail(currentDetails.Patient.Email, summary);
                }
                catch (Exception emailEx)
                {
                    MessageBox.Show("Patient record was saved, but the email could not be sent.\n\n" + emailEx.Message, "Email warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                MessageBox.Show("Patient record saved to database.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPatient();
                InitializeNewVisitForm();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private PatientRecordChangeSummary BuildChangeSummary(string firstName, string lastName, DateTime visitDate, string reason, string diagnosis, string notes)
        {
            PatientRecordChangeSummary summary = new PatientRecordChangeSummary();
            Doctor doctor = dashboardService.GetDoctorById(doctorId);

            summary.PatientEmail = currentDetails.Patient.Email;
            summary.PatientFullName = ((firstName ?? "") + " " + (lastName ?? "")).Trim();
            summary.DoctorName = doctor == null ? "Doctor" : doctor.FullName;
            summary.DoctorSpecialization = doctor == null ? "" : doctor.Specialization;
            summary.BasicInfoChanged = !string.Equals((currentDetails.Patient.FirstName ?? "").Trim(), firstName, StringComparison.Ordinal)
                || !string.Equals((currentDetails.Patient.LastName ?? "").Trim(), lastName, StringComparison.Ordinal);
            summary.UpdatedFirstName = firstName;
            summary.UpdatedLastName = lastName;

            summary.AddedAllergies = GetAddedItems(currentDetails.Allergies, editableAllergies);
            summary.RemovedAllergies = GetRemovedItems(currentDetails.Allergies, editableAllergies);
            summary.AddedChronicProblems = GetAddedItems(currentDetails.ChronicProblems, editableChronicProblems);
            summary.RemovedChronicProblems = GetRemovedItems(currentDetails.ChronicProblems, editableChronicProblems);

            List<string> existingPrescriptions = currentDetails.Prescriptions
                .Select(p => p.MedicineName + " - " + p.Dosage + " - " + p.Frequency)
                .ToList();

            summary.AddedPrescriptions = GetAddedItems(existingPrescriptions, editablePrescriptions);
            summary.RemovedPrescriptions = GetRemovedItems(existingPrescriptions, editablePrescriptions);

            summary.VisitAdded = !string.IsNullOrWhiteSpace(reason)
                || !string.IsNullOrWhiteSpace(diagnosis)
                || !string.IsNullOrWhiteSpace(notes);
            summary.VisitDate = summary.VisitAdded ? visitDate.Date : (DateTime?)null;
            summary.VisitReason = reason;
            summary.VisitDiagnosis = diagnosis;
            summary.VisitNotes = notes;
            return summary;
        }

        private List<string> GetAddedItems(List<string> oldItems, List<string> newItems)
        {
            return NormalizeItems(newItems).Except(NormalizeItems(oldItems), StringComparer.OrdinalIgnoreCase).ToList();
        }

        private List<string> GetRemovedItems(List<string> oldItems, List<string> newItems)
        {
            return NormalizeItems(oldItems).Except(NormalizeItems(newItems), StringComparer.OrdinalIgnoreCase).ToList();
        }

        private List<string> NormalizeItems(List<string> items)
        {
            if (items == null)
            {
                return new List<string>();
            }

            return items
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private void btnAddAllergy_Click(object sender, RoutedEventArgs e)
        {
            AddItemToList(txtNewAllergy, editableAllergies, allergiesPanel, "Add new allergy...", RemoveAllergy_Click);
        }

        private void btnAddChronic_Click(object sender, RoutedEventArgs e)
        {
            AddItemToList(txtNewChronic, editableChronicProblems, chronicPanel, "Add new chronic problem...", RemoveChronic_Click);
        }

        private void btnAddPrescription_Click(object sender, RoutedEventArgs e)
        {
            AddItemToList(txtNewPrescription, editablePrescriptions, prescriptionsPanel, "Add new prescription...", RemovePrescription_Click);
        }

        private void AddItemToList(TextBox textBox, List<string> list, Panel panel, string placeholder, RoutedEventHandler removeHandler)
        {
            if (!editMode)
            {
                return;
            }

            string value = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (list.Any(x => string.Equals((x ?? "").Trim(), value, StringComparison.OrdinalIgnoreCase)))
            {
                textBox.Text = "";
                RenderStringItems(panel, list, placeholder, removeHandler);
                return;
            }

            list.Add(value);
            textBox.Text = "";
            RenderStringItems(panel, list, placeholder, removeHandler);
        }

        private void CommitPendingListInputs()
        {
            AddItemToList(txtNewAllergy, editableAllergies, allergiesPanel, "Add new allergy...", RemoveAllergy_Click);
            AddItemToList(txtNewChronic, editableChronicProblems, chronicPanel, "Add new chronic problem...", RemoveChronic_Click);
            AddItemToList(txtNewPrescription, editablePrescriptions, prescriptionsPanel, "Add new prescription...", RemovePrescription_Click);
        }

        private void RemoveAllergy_Click(object sender, RoutedEventArgs e)
        {
            RemoveItemFromList(sender, editableAllergies, allergiesPanel, "Add new allergy...", RemoveAllergy_Click);
        }

        private void RemoveChronic_Click(object sender, RoutedEventArgs e)
        {
            RemoveItemFromList(sender, editableChronicProblems, chronicPanel, "Add new chronic problem...", RemoveChronic_Click);
        }

        private void RemovePrescription_Click(object sender, RoutedEventArgs e)
        {
            RemoveItemFromList(sender, editablePrescriptions, prescriptionsPanel, "Add new prescription...", RemovePrescription_Click);
        }

        private void RemoveItemFromList(object sender, List<string> list, Panel panel, string placeholder, RoutedEventHandler removeHandler)
        {
            if (!editMode)
            {
                return;
            }

            Button button = sender as Button;
            if (button == null || button.Tag == null)
            {
                return;
            }

            list.Remove(button.Tag.ToString());
            RenderStringItems(panel, list, placeholder, removeHandler);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;
        }
    }
}
