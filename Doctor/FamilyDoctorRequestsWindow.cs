using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public class FamilyDoctorRequestsWindow : Window
    {
        private int doctorId;
        private FamilyDoctorService service = new FamilyDoctorService();
        private AvatarService avatarService = new AvatarService();
        private StackPanel requestsPanel;

        public FamilyDoctorRequestsWindow(int doctorIdValue)
        {
            doctorId = doctorIdValue;
            Title = "Family Doctor Requests";
            Width = 960;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));
            BuildUi();
            LoadRequests();
        }

        private void BuildUi()
        {
            Grid root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            TextBlock title = new TextBlock { Text = "Pending Patient Requests", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, Margin = new Thickness(0, 0, 0, 18) };
            root.Children.Add(title);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 54, 0, 60) };
            Grid.SetRow(scroll, 1);
            requestsPanel = new StackPanel();
            scroll.Content = requestsPanel;
            root.Children.Add(scroll);

            Button close = new Button { Content = "Close", Height = 46, FontSize = 15, FontWeight = FontWeights.SemiBold };
            close.Click += delegate { Close(); };
            Grid.SetRow(close, 2);
            root.Children.Add(close);
        }

        private void LoadRequests()
        {
            try
            {
                requestsPanel.Children.Clear();
                List<FamilyDoctorRequestCard> requests = service.GetPendingRequests(doctorId);
                if (requests.Count == 0)
                {
                    requestsPanel.Children.Add(new TextBlock { Text = "No pending requests.", FontSize = 16, Foreground = Brushes.Gray, Margin = new Thickness(8) });
                    return;
                }
                foreach (FamilyDoctorRequestCard request in requests)
                {
                    requestsPanel.Children.Add(CreateRequestCard(request));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateRequestCard(FamilyDoctorRequestCard request)
        {
            Border card = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFDEB")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAB308")), BorderThickness = new Thickness(1.5), CornerRadius = new CornerRadius(8), Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 14) };
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            Border avatar = new Border { Width = 58, Height = 58, CornerRadius = new CornerRadius(29), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF94A3B8")), BorderThickness = new Thickness(1.5) };
            ImageBrush brush = avatarService.BuildAvatarBrush(request.AvatarUrl);
            if (brush == null)
            {
                avatar.Child = new TextBlock { Text = BuildInitials(request.PatientName), FontSize = 17, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")) };
            }
            else
            {
                avatar.Background = brush;
            }
            grid.Children.Add(avatar);

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(info, 1);
            info.Children.Add(new TextBlock { Text = request.PatientName, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.Black });
            info.Children.Add(new TextBlock { Text = "Age: " + request.PatientAge + " • Requested: " + request.RequestDateText, FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF374151")), Margin = new Thickness(0, 6, 0, 0) });
            info.Children.Add(new TextBlock { Text = "Reason: Looking for a family doctor", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")), Margin = new Thickness(0, 6, 0, 0) });
            grid.Children.Add(info);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(buttons, 2);
            Button accept = new Button { Content = "✓ Accept", Width = 110, Height = 44, FontSize = 14, FontWeight = FontWeights.Bold, Tag = request.RequestId, Margin = new Thickness(0, 0, 10, 0) };
            accept.Click += Accept_Click;
            Button decline = new Button { Content = "× Decline", Width = 110, Height = 44, FontSize = 14, FontWeight = FontWeights.Bold, Tag = request.RequestId };
            decline.Click += Decline_Click;
            buttons.Children.Add(accept);
            buttons.Children.Add(decline);
            grid.Children.Add(buttons);
            return card;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                service.AcceptRequest(Convert.ToInt32(((Button)sender).Tag), doctorId);
                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                service.DeclineRequest(Convert.ToInt32(((Button)sender).Tag), doctorId);
                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildInitials(string name)
        {
            string[] parts = (name ?? "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "P";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
            return parts[0].Substring(0, 1).ToUpper() + parts[1].Substring(0, 1).ToUpper();
        }
    }
}
