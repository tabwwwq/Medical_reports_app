using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class AdminDashboardWindow : Window
    {
        private string adminEmail = "";
        private AdminDashboardService dashboardService = new AdminDashboardService();

        public AdminDashboardWindow(string email)
        {
            InitializeComponent();
            adminEmail = email;
            txtSearch.Text = "Search logs by title, action or details...";
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            string searchText = IsShowingPlaceholder() ? "" : txtSearch.Text.Trim();
            string logCategory = ((ComboBoxItem)cmbLogCategory.SelectedItem).Content.ToString();

            AdminDashboardData dashboard = dashboardService.GetDashboardByEmail(adminEmail, searchText, "All", logCategory);
            txtAdminTitle.Text = "Admin Panel";
            txtAdminInfo.Text = dashboard.Admin.Email + " • Audit logs monitoring";
            txtSearchInfo.Text = "Logs found: " + dashboard.Logs.Count;
            RenderLogs(dashboard);
        }

        private void RenderLogs(AdminDashboardData dashboard)
        {
            logsPanel.Children.Clear();

            if (dashboard.Logs.Count == 0)
            {
                Border emptyCard = new Border();
                emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                emptyCard.BorderThickness = new Thickness(1.2);
                emptyCard.CornerRadius = new CornerRadius(12);
                emptyCard.Padding = new Thickness(20);
                emptyCard.Child = new TextBlock
                {
                    Text = "No logs were found.",
                    FontSize = 15,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                };
                logsPanel.Children.Add(emptyCard);
                return;
            }

            foreach (AdminAuditLogCard log in dashboard.Logs)
            {
                logsPanel.Children.Add(CreateLogCard(log));
            }
        }

        private Border CreateLogCard(AdminAuditLogCard log)
        {
            Border card = new Border();
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEEF5FF"));
            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF60A5FA"));
            card.BorderThickness = new Thickness(1.4);
            card.CornerRadius = new CornerRadius(10);
            card.Padding = new Thickness(16);
            card.Margin = new Thickness(0, 0, 0, 10);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            StackPanel leftPanel = new StackPanel();
            grid.Children.Add(leftPanel);

            leftPanel.Children.Add(new TextBlock
            {
                Text = log.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = log.Description,
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF334155"))
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = log.MetaText,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            Border tag = new Border();
            tag.CornerRadius = new CornerRadius(6);
            tag.Padding = new Thickness(12, 6, 12, 6);
            tag.Margin = new Thickness(16, 0, 0, 0);
            tag.VerticalAlignment = VerticalAlignment.Top;
            tag.Background = GetBadgeBackground(log.LogTypeText);
            tag.Child = new TextBlock
            {
                Text = log.LogTypeText,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F172A"))
            };
            Grid.SetColumn(tag, 1);
            grid.Children.Add(tag);

            return card;
        }

        private Brush GetBadgeBackground(string logTypeText)
        {
            if (logTypeText == "WARNING")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDE68A"));
            }

            if (logTypeText == "ERROR")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFCA5A5"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBFDBFE"));
        }

        private bool IsShowingPlaceholder()
        {
            return txtSearch.Foreground.ToString() == new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF")).ToString();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!IsShowingPlaceholder())
            {
                return;
            }

            txtSearch.Text = "";
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                return;
            }

            txtSearch.Text = "Search logs by title, action or details...";
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }


        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadDashboard();
            }
        }

        private void cmbLogCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            LoadDashboard();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
