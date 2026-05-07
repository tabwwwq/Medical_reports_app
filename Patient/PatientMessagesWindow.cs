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
    public class PatientMessagesWindow : Window
    {
        private int patientId;
        private MessageService messageService = new MessageService();
        private StackPanel doctorsPanel;
        private TextBox txtSearch;
        private List<DoctorChatContactCard> loadedDoctors = new List<DoctorChatContactCard>();

        public PatientMessagesWindow(int patientId)
        {
            this.patientId = patientId;
            Title = "Messages";
            Width = 760;
            Height = 640;
            MinWidth = 640;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brush("#FFF8FAFC");
            BuildUi();
            LoadDoctors();
        }

        private void BuildUi()
        {
            Grid root = new Grid();
            root.Margin = new Thickness(18);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Content = root;

            TextBlock title = new TextBlock();
            title.Text = "Messages";
            title.FontSize = 28;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = Brush("#FF020617");
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            TextBlock subtitle = new TextBlock();
            subtitle.Text = "You can message your family doctor and doctors who added visit information for you.";
            subtitle.FontSize = 14;
            subtitle.Foreground = Brush("#FF64748B");
            subtitle.Margin = new Thickness(0, 38, 0, 16);
            Grid.SetRow(subtitle, 0);
            root.Children.Add(subtitle);

            Border searchBorder = new Border();
            searchBorder.Height = 46;
            searchBorder.Background = Brushes.White;
            searchBorder.BorderBrush = Brush("#FFCBD5E1");
            searchBorder.BorderThickness = new Thickness(1.4);
            searchBorder.CornerRadius = new CornerRadius(12);
            searchBorder.Padding = new Thickness(14, 0, 14, 0);
            searchBorder.Margin = new Thickness(0, 0, 0, 16);
            Grid.SetRow(searchBorder, 1);
            root.Children.Add(searchBorder);

            txtSearch = new TextBox();
            txtSearch.BorderThickness = new Thickness(0);
            txtSearch.Background = Brushes.Transparent;
            txtSearch.FontSize = 15;
            txtSearch.VerticalContentAlignment = VerticalAlignment.Center;
            txtSearch.TextChanged += txtSearch_TextChanged;
            searchBorder.Child = txtSearch;

            Border listCard = CreateCard(14);
            listCard.Padding = new Thickness(18);
            Grid.SetRow(listCard, 2);
            root.Children.Add(listCard);

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            listCard.Child = scrollViewer;
            doctorsPanel = new StackPanel();
            scrollViewer.Content = doctorsPanel;
        }

        private void LoadDoctors()
        {
            try
            {
                string search = txtSearch == null ? "" : txtSearch.Text.Trim();
                loadedDoctors = messageService.GetPatientAllowedDoctors(patientId, search);
                RenderDoctors();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderDoctors()
        {
            doctorsPanel.Children.Clear();
            if (loadedDoctors.Count == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "No doctors available for messages yet.";
                empty.FontSize = 15;
                empty.Foreground = Brush("#FF64748B");
                empty.HorizontalAlignment = HorizontalAlignment.Center;
                empty.Margin = new Thickness(0, 120, 0, 0);
                doctorsPanel.Children.Add(empty);
                return;
            }

            foreach (DoctorChatContactCard doctor in loadedDoctors)
            {
                doctorsPanel.Children.Add(CreateDoctorCard(doctor));
            }
        }

        private Border CreateDoctorCard(DoctorChatContactCard doctor)
        {
            Border card = new Border();
            card.Background = Brush("#FFF8FAFC");
            card.BorderBrush = Brush("#FFCBD5E1");
            card.BorderThickness = new Thickness(1.4);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(18);
            card.Margin = new Thickness(0, 0, 0, 12);
            card.Cursor = Cursors.Hand;
            card.Tag = doctor;
            card.MouseLeftButtonUp += doctorCard_MouseLeftButtonUp;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            StackPanel info = new StackPanel();
            grid.Children.Add(info);

            TextBlock name = new TextBlock();
            name.Text = doctor.FullName;
            name.FontSize = 17;
            name.FontWeight = FontWeights.Bold;
            name.Foreground = Brush("#FF020617");
            info.Children.Add(name);

            TextBlock specialization = new TextBlock();
            specialization.Text = doctor.Specialization;
            specialization.FontSize = 14;
            specialization.Foreground = Brush("#FF334155");
            specialization.Margin = new Thickness(0, 6, 0, 0);
            info.Children.Add(specialization);

            TextBlock access = new TextBlock();
            access.Text = doctor.AccessText;
            access.FontSize = 13;
            access.Foreground = Brush("#FF64748B");
            access.Margin = new Thickness(0, 6, 0, 0);
            info.Children.Add(access);

            Button openButton = new Button();
            openButton.Content = "Open chat";
            openButton.Width = 104;
            openButton.Height = 40;
            openButton.Background = Brush("#FF2F6FED");
            openButton.Foreground = Brushes.White;
            openButton.FontWeight = FontWeights.Bold;
            openButton.BorderThickness = new Thickness(0);
            openButton.Template = RoundedButtonTemplate(20);
            openButton.Tag = doctor;
            openButton.Click += openButton_Click;
            Grid.SetColumn(openButton, 1);
            grid.Children.Add(openButton);

            return card;
        }

        private void doctorCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border card = sender as Border;
            DoctorChatContactCard doctor = card == null ? null : card.Tag as DoctorChatContactCard;
            if (doctor != null)
            {
                OpenChat(doctor);
            }
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DoctorChatContactCard doctor = button == null ? null : button.Tag as DoctorChatContactCard;
            if (doctor != null)
            {
                OpenChat(doctor);
            }
        }

        private void OpenChat(DoctorChatContactCard doctor)
        {
            ChatWindow window = new ChatWindow(patientId, doctor.DoctorId, "Patient");
            window.Owner = this;
            window.ShowDialog();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadDoctors();
            }
        }

        private Border CreateCard(double radius)
        {
            Border border = new Border();
            border.Background = Brushes.White;
            border.BorderBrush = Brush("#FFCBD5E1");
            border.BorderThickness = new Thickness(1.4);
            border.CornerRadius = new CornerRadius(radius);
            return border;
        }

        private ControlTemplate RoundedButtonTemplate(double radius)
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            return new ControlTemplate(typeof(Button)) { VisualTree = border };
        }

        private SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }
}
