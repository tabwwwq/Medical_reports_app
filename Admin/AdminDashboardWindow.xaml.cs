using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public partial class AdminDashboardWindow : Window
    {
        private string adminEmail = "";
        private AdminDashboardService dashboardService = new AdminDashboardService();
        private AvatarService avatarService = new AvatarService();

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
            RenderSuspiciousActivities(dashboard);
            RenderDoctors(dashboard);
            RenderLogs(dashboard);
        }

        private void RenderSuspiciousActivities(AdminDashboardData dashboard)
        {
            suspiciousPanel.Children.Clear();

            if (dashboard.SuspiciousActivities.Count == 0)
            {
                Border emptyCard = new Border();
                emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                emptyCard.BorderThickness = new Thickness(1.2);
                emptyCard.CornerRadius = new CornerRadius(12);
                emptyCard.Padding = new Thickness(18);
                emptyCard.Child = new TextBlock
                {
                    Text = "No suspicious activity detected in the latest doctor changes.",
                    FontSize = 15,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                };
                suspiciousPanel.Children.Add(emptyCard);
                return;
            }

            foreach (SuspiciousActivityAlert alert in dashboard.SuspiciousActivities)
            {
                suspiciousPanel.Children.Add(CreateSuspiciousCard(alert));
            }
        }

        private Border CreateSuspiciousCard(SuspiciousActivityAlert alert)
        {
            Border card = new Border();
            card.Background = GetAlertBackground(alert.Priority);
            card.BorderBrush = GetAlertBorder(alert.Priority);
            card.BorderThickness = new Thickness(1.5);
            card.CornerRadius = new CornerRadius(14);
            card.Padding = new Thickness(18);
            card.Margin = new Thickness(0, 0, 0, 14);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            StackPanel leftPanel = new StackPanel();
            grid.Children.Add(leftPanel);

            leftPanel.Children.Add(new TextBlock
            {
                Text = alert.Priority,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = GetAlertText(alert.Priority)
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = alert.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = alert.Description,
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF334155"))
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = "User: " + alert.DoctorName,
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            leftPanel.Children.Add(new TextBlock
            {
                Text = "Time: " + alert.WindowStart.ToString("yyyy-MM-dd HH:mm:ss") + " - " + alert.WindowEnd.ToString("HH:mm:ss"),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            StackPanel buttonPanel = new StackPanel();
            buttonPanel.Orientation = Orientation.Horizontal;
            buttonPanel.HorizontalAlignment = HorizontalAlignment.Right;
            buttonPanel.VerticalAlignment = VerticalAlignment.Top;
            buttonPanel.Margin = new Thickness(20, 0, 0, 0);
            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Button investigateButton = CreateAlertButton("Investigate", "#FF2563EB");
            investigateButton.Margin = new Thickness(0, 0, 10, 0);
            investigateButton.Tag = alert;
            investigateButton.Click += btnInvestigate_Click;
            buttonPanel.Children.Add(investigateButton);

            Button takeActionButton = CreateAlertButton("Take Actions", "#FFF97316");
            takeActionButton.Tag = alert;
            takeActionButton.Click += btnTakeActions_Click;
            buttonPanel.Children.Add(takeActionButton);

            return card;
        }

        private Button CreateAlertButton(string text, string backgroundColor)
        {
            Button button = new Button();
            button.Content = text;
            button.Height = 44;
            button.MinWidth = 150;
            button.Padding = new Thickness(16, 0, 16, 0);
            button.FontSize = 14;
            button.FontWeight = FontWeights.Bold;
            button.Foreground = Brushes.White;
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor));
            button.BorderThickness = new Thickness(0);
            button.Cursor = Cursors.Hand;

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            button.Template = template;

            return button;
        }

        private void RenderDoctors(AdminDashboardData dashboard)
        {
            doctorsPanel.Children.Clear();

            if (dashboard.Doctors.Count == 0)
            {
                Border emptyCard = new Border();
                emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                emptyCard.BorderThickness = new Thickness(1.2);
                emptyCard.CornerRadius = new CornerRadius(12);
                emptyCard.Padding = new Thickness(18);
                emptyCard.Child = new TextBlock
                {
                    Text = "No doctors found.",
                    FontSize = 15,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                };
                doctorsPanel.Children.Add(emptyCard);
                return;
            }

            foreach (AdminDoctorCard doctor in dashboard.Doctors)
            {
                doctorsPanel.Children.Add(CreateDoctorCard(doctor));
            }
        }

        private Border CreateDoctorCard(AdminDoctorCard doctor)
        {
            Border card = new Border();
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));
            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            card.BorderThickness = new Thickness(1.1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(16);
            card.Margin = new Thickness(0, 0, 0, 12);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            Border avatar = new Border();
            avatar.Width = 62;
            avatar.Height = 62;
            avatar.CornerRadius = new CornerRadius(31);
            avatar.BorderThickness = new Thickness(2);
            avatar.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF94A3B8"));
            avatar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1F5F9"));
            avatar.VerticalAlignment = VerticalAlignment.Top;
            ImageBrush doctorBrush = avatarService.BuildAvatarBrush(doctor.AvatarUrl);
            if (doctorBrush == null)
            {
                avatar.Child = new TextBlock
                {
                    Text = doctor.Initials,
                    FontSize = 21,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
            }
            else
            {
                avatar.Background = doctorBrush;
            }
            grid.Children.Add(avatar);

            StackPanel info = new StackPanel();
            info.Margin = new Thickness(16, 0, 0, 0);
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            info.Children.Add(new TextBlock
            {
                Text = doctor.FullName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });
            info.Children.Add(new TextBlock
            {
                Text = doctor.Email,
                FontSize = 14,
                Margin = new Thickness(0, 3, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });
            info.Children.Add(new TextBlock
            {
                Text = "Specialization: " + (string.IsNullOrWhiteSpace(doctor.Specialization) ? "-" : doctor.Specialization),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1F2937"))
            });
            info.Children.Add(new TextBlock
            {
                Text = "Added: " + doctor.CreatedAt.ToString("yyyy-MM-dd") + "  •  Status: " + doctor.StatusText,
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = doctor.IsActive
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"))
            });

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);

            Button avatarButton = CreateAlertButton("Edit Doctor", "#FF3B82F6");
            avatarButton.MinWidth = 120;
            avatarButton.Height = 42;
            avatarButton.Margin = new Thickness(0, 0, 10, 0);
            avatarButton.Tag = doctor;
            avatarButton.Click += btnEditDoctor_Click;
            actions.Children.Add(avatarButton);

            if (doctor.IsActive)
            {
                Button freezeButton = CreateAlertButton("Freeze Account", "#FFEF4444");
                freezeButton.MinWidth = 130;
                freezeButton.Height = 42;
                freezeButton.Tag = doctor;
                freezeButton.Click += btnFreezeDoctor_Click;
                actions.Children.Add(freezeButton);
            }
            else
            {
                Button unfreezeButton = CreateAlertButton("Unfreeze Account", "#FF22C55E");
                unfreezeButton.MinWidth = 120;
                unfreezeButton.Height = 42;
                unfreezeButton.Tag = doctor;
                unfreezeButton.Click += btnUnfreezeDoctor_Click;
                actions.Children.Add(unfreezeButton);
            }

            return card;
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

        private Brush GetAlertBackground(string priority)
        {
            if (priority == "HIGH PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFEE2E2"));
            }

            if (priority == "MEDIUM PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFEF3C7"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFECFCCB"));
        }

        private Brush GetAlertBorder(string priority)
        {
            if (priority == "HIGH PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444"));
            }

            if (priority == "MEDIUM PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF84CC16"));
        }

        private Brush GetAlertText(string priority)
        {
            if (priority == "HIGH PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB91C1C"));
            }

            if (priority == "MEDIUM PRIORITY")
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF92400E"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F6212"));
        }

        private string GetRealText(TextBox textBox)
        {
            if (textBox == null)
            {
                return "";
            }

            SolidColorBrush brush = textBox.Foreground as SolidColorBrush;
            if (brush != null && brush.Color == ((Color)ColorConverter.ConvertFromString("#FF9CA3AF")))
            {
                return "";
            }

            return (textBox.Text ?? "").Trim();
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

        private void btnInvestigate_Click(object sender, RoutedEventArgs e)
        {
            SuspiciousActivityAlert alert = (sender as Button).Tag as SuspiciousActivityAlert;
            if (alert == null)
            {
                return;
            }

            Window detailsWindow = new Window();
            detailsWindow.Title = "Investigate Suspicious Activity";
            detailsWindow.Width = 980;
            detailsWindow.Height = 760;
            detailsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            detailsWindow.Owner = this;
            detailsWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));

            Border rootBorder = new Border();
            rootBorder.Padding = new Thickness(22);
            rootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));
            detailsWindow.Content = rootBorder;

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            rootBorder.Child = scrollViewer;

            StackPanel rootPanel = new StackPanel();
            scrollViewer.Content = rootPanel;

            rootPanel.Children.Add(new TextBlock
            {
                Text = "Detailed Changes",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                Margin = new Thickness(0, 0, 0, 18)
            });

            Border summaryCard = new Border();
            summaryCard.Background = Brushes.White;
            summaryCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            summaryCard.BorderThickness = new Thickness(1.2);
            summaryCard.CornerRadius = new CornerRadius(12);
            summaryCard.Padding = new Thickness(18);
            summaryCard.Margin = new Thickness(0, 0, 0, 18);
            rootPanel.Children.Add(summaryCard);

            StackPanel summaryPanel = new StackPanel();
            summaryCard.Child = summaryPanel;
            summaryPanel.Children.Add(new TextBlock
            {
                Text = alert.Title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });
            summaryPanel.Children.Add(new TextBlock
            {
                Text = "User: " + alert.DoctorName,
                FontSize = 14,
                Margin = new Thickness(0, 10, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });
            summaryPanel.Children.Add(new TextBlock
            {
                Text = "Time: " + alert.WindowStart.ToString("yyyy-MM-dd HH:mm:ss") + " - " + alert.WindowEnd.ToString("HH:mm:ss"),
                FontSize = 14,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            rootPanel.Children.Add(new TextBlock
            {
                Text = "All Changes (" + alert.Changes.Count + ")",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                Margin = new Thickness(0, 0, 0, 12)
            });

            foreach (AdminAuditLogCard change in alert.Changes)
            {
                Border itemCard = new Border();
                itemCard.Background = Brushes.White;
                itemCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                itemCard.BorderThickness = new Thickness(1.2);
                itemCard.CornerRadius = new CornerRadius(12);
                itemCard.Padding = new Thickness(18);
                itemCard.Margin = new Thickness(0, 0, 0, 12);
                rootPanel.Children.Add(itemCard);

                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
                itemCard.Child = itemGrid;

                StackPanel infoPanel = new StackPanel();
                itemGrid.Children.Add(infoPanel);
                infoPanel.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(change.TargetName) ? "Patient" : change.TargetName,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
                });
                infoPanel.Children.Add(new TextBlock
                {
                    Text = "Field Changed",
                    FontSize = 13,
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                });
                infoPanel.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(change.FieldName) ? change.Title : change.FieldName,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
                });

                Grid valueGrid = new Grid();
                valueGrid.Margin = new Thickness(0, 14, 0, 0);
                valueGrid.ColumnDefinitions.Add(new ColumnDefinition());
                valueGrid.ColumnDefinitions.Add(new ColumnDefinition());
                itemGrid.Children.Add(valueGrid);
                Grid.SetColumn(valueGrid, 1);

                StackPanel oldPanel = new StackPanel();
                valueGrid.Children.Add(oldPanel);
                oldPanel.Children.Add(new TextBlock
                {
                    Text = "Old Value",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                });
                oldPanel.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(change.OldValue) ? "None" : change.OldValue,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"))
                });

                StackPanel newPanel = new StackPanel();
                newPanel.Margin = new Thickness(18, 0, 0, 0);
                valueGrid.Children.Add(newPanel);
                Grid.SetColumn(newPanel, 1);
                newPanel.Children.Add(new TextBlock
                {
                    Text = "New Value",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                });
                newPanel.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(change.NewValue) ? "None" : change.NewValue,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF16A34A"))
                });

                TextBlock timeBlock = new TextBlock();
                timeBlock.Text = change.CreatedAt.ToString("HH:mm:ss");
                timeBlock.FontSize = 13;
                timeBlock.HorizontalAlignment = HorizontalAlignment.Right;
                timeBlock.VerticalAlignment = VerticalAlignment.Top;
                timeBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"));
                itemGrid.Children.Add(timeBlock);
                Grid.SetColumn(timeBlock, 1);
            }

            detailsWindow.ShowDialog();
        }

        private void btnAddDoctor_Click(object sender, RoutedEventArgs e)
        {
            Window dialog = new Window();
            dialog.Title = "Add New Doctor";
            dialog.Width = 660;
            dialog.Height = 860;
            dialog.MinWidth = 620;
            dialog.MinHeight = 760;
            dialog.ResizeMode = ResizeMode.NoResize;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Owner = this;
            dialog.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));

            ScrollViewer viewer = new ScrollViewer();
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            dialog.Content = viewer;

            Grid outerGrid = new Grid();
            outerGrid.Margin = new Thickness(28);
            viewer.Content = outerGrid;

            Border formShell = new Border();
            formShell.Width = 520;
            formShell.HorizontalAlignment = HorizontalAlignment.Center;
            formShell.VerticalAlignment = VerticalAlignment.Center;
            formShell.Background = Brushes.White;
            formShell.CornerRadius = new CornerRadius(18);
            formShell.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB"));
            formShell.BorderThickness = new Thickness(1.5);
            formShell.Padding = new Thickness(34, 28, 34, 28);
            outerGrid.Children.Add(formShell);

            StackPanel stack = new StackPanel();
            formShell.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Text = "Add Doctor",
                FontSize = 31,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Create a doctor account in the same clean style as registration.",
                FontSize = 15,
                Margin = new Thickness(0, 6, 0, 24),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")),
                TextWrapping = TextWrapping.Wrap
            });

            Border infoCard = new Border();
            infoCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0FDF4"));
            infoCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF86EFAC"));
            infoCard.BorderThickness = new Thickness(1.3);
            infoCard.CornerRadius = new CornerRadius(12);
            infoCard.Padding = new Thickness(16);
            infoCard.Margin = new Thickness(0, 0, 0, 20);
            infoCard.Child = new TextBlock
            {
                Text = "Fill in the doctor's data and choose whether the account should be active immediately.",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF166534")),
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(infoCard);

            TextBox txtFirstName = CreateDialogTextBox("John");
            TextBox txtLastName = CreateDialogTextBox("Smith");
            TextBox txtEmail = CreateDialogTextBox("doctor@hospital.com");
            PasswordBox txtPassword = CreateDialogPasswordBox();
            ComboBox cmbGender = CreateDialogComboBox();
            cmbGender.Items.Add("Male");
            cmbGender.Items.Add("Female");
            cmbGender.Items.Add("Other");
            cmbGender.SelectedIndex = 0;
            DatePicker dpBirthDate = CreateDialogDatePicker();
            dpBirthDate.SelectedDate = new DateTime(1990, 1, 1);
            TextBox txtSpecialization = CreateDialogTextBox("Cardiology, Pediatrics, etc.");
            CheckBox chkActive = new CheckBox
            {
                Content = "Activate account right after creation",
                IsChecked = true,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(CreateDialogField("First Name *", txtFirstName));
            stack.Children.Add(CreateDialogField("Last Name *", txtLastName));
            stack.Children.Add(CreateDialogField("Email Address *", txtEmail));
            stack.Children.Add(CreateDialogField("Password *", txtPassword, "Minimum 6 characters"));
            stack.Children.Add(CreateDialogField("Gender *", cmbGender));
            stack.Children.Add(CreateDialogField("Birth Date *", dpBirthDate));
            stack.Children.Add(CreateDialogField("Specialization *", txtSpecialization));

            Border activeBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB")),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 18),
                Child = chkActive
            };
            stack.Children.Add(activeBorder);

            Grid buttonGrid = new Grid();
            buttonGrid.Margin = new Thickness(0, 8, 0, 0);
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stack.Children.Add(buttonGrid);

            Button btnCancel = CreateSecondaryDialogButton("Cancel");
            btnCancel.Click += (s, args) => dialog.Close();
            buttonGrid.Children.Add(btnCancel);
            Grid.SetColumn(btnCancel, 0);

            Button btnSave = CreatePrimaryDialogButton("Add Doctor", "Create doctor account and show it in doctors list");
            btnSave.Click += (s, args) =>
            {
                try
                {
                    if (!dpBirthDate.SelectedDate.HasValue)
                    {
                        throw new Exception("Please select birth date.");
                    }

                    dashboardService.AddDoctor(
                        adminEmail,
                        GetRealText(txtFirstName),
                        GetRealText(txtLastName),
                        GetRealText(txtEmail),
                        txtPassword.Password,
                        cmbGender.SelectedItem == null ? "Other" : cmbGender.SelectedItem.ToString(),
                        dpBirthDate.SelectedDate.Value,
                        GetRealText(txtSpecialization),
                        chkActive.IsChecked == true
                    );

                    dialog.DialogResult = true;
                    dialog.Close();
                    LoadDashboard();
                    MessageBox.Show("Doctor account was created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            buttonGrid.Children.Add(btnSave);
            Grid.SetColumn(btnSave, 2);

            dialog.ShowDialog();
        }

        private void btnEditDoctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                AdminDoctorCard doctor = button == null ? null : button.Tag as AdminDoctorCard;
                if (doctor == null)
                {
                    return;
                }

                Window dialog = new Window();
                dialog.Title = "Edit Doctor";
                dialog.Width = 680;
                dialog.Height = 880;
                dialog.MinWidth = 620;
                dialog.MinHeight = 760;
                dialog.ResizeMode = ResizeMode.NoResize;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Owner = this;
                dialog.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));

                ScrollViewer viewer = new ScrollViewer();
                viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                dialog.Content = viewer;

                Border formShell = new Border();
                formShell.Background = Brushes.White;
                formShell.CornerRadius = new CornerRadius(18);
                formShell.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
                formShell.BorderThickness = new Thickness(1.2);
                formShell.Padding = new Thickness(28);
                formShell.Margin = new Thickness(28);
                viewer.Content = formShell;

                StackPanel stack = new StackPanel();
                formShell.Child = stack;

                stack.Children.Add(new TextBlock
                {
                    Text = "Edit Doctor",
                    FontSize = 31,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
                });

                stack.Children.Add(new TextBlock
                {
                    Text = "Change doctor information and avatar.",
                    FontSize = 15,
                    Margin = new Thickness(0, 6, 0, 24),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")),
                    TextWrapping = TextWrapping.Wrap
                });

                Border avatarPreview = new Border();
                avatarPreview.Width = 110;
                avatarPreview.Height = 110;
                avatarPreview.CornerRadius = new CornerRadius(55);
                avatarPreview.BorderThickness = new Thickness(2);
                avatarPreview.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF94A3B8"));
                avatarPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1F5F9"));
                avatarPreview.HorizontalAlignment = HorizontalAlignment.Center;
                avatarPreview.Margin = new Thickness(0, 0, 0, 14);

                ImageBrush existingBrush = avatarService.BuildAvatarBrush(doctor.AvatarUrl);
                if (existingBrush == null)
                {
                    avatarPreview.Child = new TextBlock
                    {
                        Text = doctor.Initials,
                        FontSize = 30,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                }
                else
                {
                    avatarPreview.Background = existingBrush;
                }
                stack.Children.Add(avatarPreview);

                string selectedAvatarFile = "";
                Button btnChooseAvatar = CreateSecondaryDialogButton("Change Avatar");
                btnChooseAvatar.Margin = new Thickness(0, 0, 0, 18);
                btnChooseAvatar.Click += (s, args) =>
                {
                    string filePath = avatarService.SelectImageFile();
                    if (filePath == "")
                    {
                        return;
                    }

                    selectedAvatarFile = filePath;
                    ImageBrush previewBrush = avatarService.BuildAvatarBrush(filePath);
                    if (previewBrush == null)
                    {
                        System.Windows.Media.Imaging.BitmapImage bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        previewBrush = new ImageBrush(bitmap);
                        previewBrush.Stretch = Stretch.UniformToFill;
                    }
                    avatarPreview.Background = previewBrush;
                    avatarPreview.Child = null;
                };
                stack.Children.Add(btnChooseAvatar);

                TextBox txtFirstName = CreateDialogValueTextBox(doctor.FirstName);
                TextBox txtLastName = CreateDialogValueTextBox(doctor.LastName);
                TextBox txtEmail = CreateDialogValueTextBox(doctor.Email);
                ComboBox cmbGender = CreateDialogComboBox();
                cmbGender.Items.Add("Male");
                cmbGender.Items.Add("Female");
                cmbGender.Items.Add("Other");
                cmbGender.SelectedItem = string.IsNullOrWhiteSpace(doctor.Gender) ? "Other" : doctor.Gender;
                if (cmbGender.SelectedItem == null)
                {
                    cmbGender.SelectedIndex = 2;
                }
                DatePicker dpBirthDate = CreateDialogDatePicker();
                dpBirthDate.SelectedDate = doctor.BirthDate;
                TextBox txtSpecialization = CreateDialogValueTextBox(doctor.Specialization);
                CheckBox chkActive = new CheckBox
                {
                    Content = "Doctor account is active",
                    IsChecked = doctor.IsActive,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                    VerticalAlignment = VerticalAlignment.Center
                };

                stack.Children.Add(CreateDialogField("First Name *", txtFirstName));
                stack.Children.Add(CreateDialogField("Last Name *", txtLastName));
                stack.Children.Add(CreateDialogField("Email Address *", txtEmail));
                stack.Children.Add(CreateDialogField("Gender *", cmbGender));
                stack.Children.Add(CreateDialogField("Birth Date *", dpBirthDate));
                stack.Children.Add(CreateDialogField("Specialization *", txtSpecialization));

                Border activeBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB")),
                    BorderThickness = new Thickness(1.2),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 18),
                    Child = chkActive
                };
                stack.Children.Add(activeBorder);

                Grid buttonGrid = new Grid();
                buttonGrid.Margin = new Thickness(0, 8, 0, 0);
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                stack.Children.Add(buttonGrid);

                Button btnCancel = CreateSecondaryDialogButton("Cancel");
                btnCancel.Click += (s, args) => dialog.Close();
                buttonGrid.Children.Add(btnCancel);
                Grid.SetColumn(btnCancel, 0);

                Button btnSave = CreatePrimaryDialogButton("Save Changes", "Update doctor profile");
                btnSave.Click += (s, args) =>
                {
                    try
                    {
                        if (!dpBirthDate.SelectedDate.HasValue)
                        {
                            throw new Exception("Please select birth date.");
                        }

                        dashboardService.UpdateDoctor(
                            doctor.DoctorId,
                            adminEmail,
                            GetRealText(txtFirstName),
                            GetRealText(txtLastName),
                            GetRealText(txtEmail),
                            cmbGender.SelectedItem == null ? "Other" : cmbGender.SelectedItem.ToString(),
                            dpBirthDate.SelectedDate.Value,
                            GetRealText(txtSpecialization),
                            chkActive.IsChecked == true
                        );

                        if (!string.IsNullOrWhiteSpace(selectedAvatarFile))
                        {
                            avatarService.SaveDoctorAvatar(doctor.DoctorId, selectedAvatarFile);
                        }

                        dialog.DialogResult = true;
                        dialog.Close();
                        LoadDashboard();
                        MessageBox.Show("Doctor information was updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                buttonGrid.Children.Add(btnSave);
                Grid.SetColumn(btnSave, 2);

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFreezeDoctor_Click(object sender, RoutedEventArgs e)
        {
            AdminDoctorCard doctor = (sender as Button).Tag as AdminDoctorCard;
            if (doctor == null)
            {
                return;
            }

            MessageBoxResult confirm = MessageBox.Show("Freeze " + doctor.FullName + " account?", "Confirm freeze", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                dashboardService.FreezeDoctorAccountById(doctor.DoctorId, adminEmail);
                LoadDashboard();
                MessageBox.Show("Doctor account was frozen.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnUnfreezeDoctor_Click(object sender, RoutedEventArgs e)
        {
            AdminDoctorCard doctor = (sender as Button).Tag as AdminDoctorCard;
            if (doctor == null)
            {
                return;
            }

            MessageBoxResult confirm = MessageBox.Show("Unfreeze " + doctor.FullName + " account?", "Confirm unfreeze", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                dashboardService.UnfreezeDoctorAccountById(doctor.DoctorId, adminEmail);
                LoadDashboard();
                MessageBox.Show("Doctor account was activated.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TextBox CreateDialogTextBox(string placeholder)
        {
            TextBox textBox = new TextBox();
            textBox.Height = 52;
            textBox.FontSize = 15;
            textBox.Padding = new Thickness(16, 0, 16, 0);
            textBox.Margin = new Thickness(0);
            textBox.Background = Brushes.White;
            textBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            textBox.BorderThickness = new Thickness(1.5);
            textBox.Text = placeholder;
            textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
                }
            };
            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"));
                }
            };
            return textBox;
        }

        private TextBox CreateDialogValueTextBox(string value)
        {
            TextBox textBox = new TextBox();
            textBox.Height = 52;
            textBox.FontSize = 15;
            textBox.Padding = new Thickness(16, 0, 16, 0);
            textBox.Margin = new Thickness(0);
            textBox.Background = Brushes.White;
            textBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            textBox.BorderThickness = new Thickness(1.5);
            textBox.Text = value ?? "";
            textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
            return textBox;
        }

        private PasswordBox CreateDialogPasswordBox()
        {
            PasswordBox passwordBox = new PasswordBox();
            passwordBox.Height = 52;
            passwordBox.FontSize = 15;
            passwordBox.Padding = new Thickness(16, 0, 16, 0);
            passwordBox.Margin = new Thickness(0);
            passwordBox.Background = Brushes.White;
            passwordBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            passwordBox.BorderThickness = new Thickness(1.5);
            return passwordBox;
        }

        private ComboBox CreateDialogComboBox()
        {
            ComboBox comboBox = new ComboBox();
            comboBox.Height = 52;
            comboBox.FontSize = 15;
            comboBox.Margin = new Thickness(0);
            comboBox.Background = Brushes.White;
            comboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            comboBox.BorderThickness = new Thickness(1.5);
            comboBox.Padding = new Thickness(12, 0, 12, 0);
            return comboBox;
        }

        private DatePicker CreateDialogDatePicker()
        {
            DatePicker datePicker = new DatePicker();
            datePicker.Height = 52;
            datePicker.FontSize = 15;
            datePicker.Margin = new Thickness(0);
            datePicker.Background = Brushes.White;
            datePicker.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            datePicker.BorderThickness = new Thickness(1.5);
            return datePicker;
        }

        private Button CreatePrimaryDialogButton(string title, string subtitle)
        {
            Button button = new Button();
            button.Content = title;
            button.Height = 52;
            button.FontSize = 16;
            button.FontWeight = FontWeights.Bold;
            button.Foreground = Brushes.White;
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2563EB"));
            button.BorderThickness = new Thickness(0);
            button.Cursor = Cursors.Hand;
            button.Template = CreateRoundedButtonTemplate(12);
            button.ToolTip = subtitle;
            return button;
        }

        private Button CreateSecondaryDialogButton(string text)
        {
            Button button = new Button();
            button.Content = text;
            button.Height = 52;
            button.FontSize = 15;
            button.FontWeight = FontWeights.SemiBold;
            button.Background = Brushes.White;
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            button.BorderThickness = new Thickness(1.2);
            button.Cursor = Cursors.Hand;
            button.Template = CreateRoundedButtonTemplate(12);
            return button;
        }

        private void AddFormField(Grid grid, int row, int column, string title, FrameworkElement input, int columnSpan = 1)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(column == 0 ? 0 : 16, 0, 0, 12);
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });
            panel.Children.Add(input);
            grid.Children.Add(panel);
            Grid.SetRow(panel, row);
            Grid.SetColumn(panel, column);
            Grid.SetColumnSpan(panel, columnSpan);
        }

        private StackPanel CreateDialogField(string title, FrameworkElement input, string hint = null)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(0, 0, 0, 18);

            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF334155")),
                Margin = new Thickness(0, 0, 0, 8)
            });

            panel.Children.Add(input);

            if (!string.IsNullOrWhiteSpace(hint))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = hint,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF")),
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            return panel;
        }

        private void btnTakeActions_Click(object sender, RoutedEventArgs e)
        {
            SuspiciousActivityAlert alert = (sender as Button).Tag as SuspiciousActivityAlert;
            if (alert == null)
            {
                return;
            }

            Window dialog = new Window();
            dialog.Title = "Take Action";
            dialog.Width = 720;
            dialog.Height = 500;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.ResizeMode = ResizeMode.NoResize;
            dialog.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FAFC"));
            dialog.Owner = this;

            Border root = new Border();
            root.Padding = new Thickness(24);
            dialog.Content = root;

            StackPanel stack = new StackPanel();
            root.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Text = alert.Title,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            Border infoCard = new Border();
            infoCard.Margin = new Thickness(0, 18, 0, 18);
            infoCard.Padding = new Thickness(18);
            infoCard.CornerRadius = new CornerRadius(14);
            infoCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            infoCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            infoCard.BorderThickness = new Thickness(1.3);
            stack.Children.Add(infoCard);

            StackPanel infoStack = new StackPanel();
            infoCard.Child = infoStack;
            infoStack.Children.Add(new TextBlock
            {
                Text = alert.Description,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = "User: " + alert.DoctorName,
                FontSize = 14,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = "Time: " + alert.WindowStart.ToString("yyyy-MM-dd HH:mm:ss"),
                FontSize = 14,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Choose an action:",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 14),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            Button freezeButton = CreateActionChoiceButton("Freeze Account", "Temporarily suspend this user's account access", "#FFFEF2F2", "#FFEF4444", "#FFDC2626");
            freezeButton.Margin = new Thickness(0, 0, 0, 12);
            freezeButton.Click += (s, args) =>
            {
                MessageBoxResult confirm = MessageBox.Show("Freeze " + alert.DoctorName + " account?", "Confirm freeze", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    dashboardService.FreezeDoctorAccount(alert, adminEmail);
                    dialog.DialogResult = true;
                    dialog.Close();
                    LoadDashboard();
                    MessageBox.Show("Doctor account was blocked successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            stack.Children.Add(freezeButton);

            Button okButton = CreateActionChoiceButton("Mark as OK", "This activity is normal, no action needed", "#FFF0FDF4", "#FF22C55E", "#FF16A34A");
            okButton.Margin = new Thickness(0, 0, 0, 18);
            okButton.Click += (s, args) =>
            {
                try
                {
                    dashboardService.MarkSuspiciousActivityAction(alert, adminEmail, "MARKED_OK");
                    dialog.DialogResult = true;
                    dialog.Close();
                    LoadDashboard();
                    MessageBox.Show("Alert was removed from active suspicious activity.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            stack.Children.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Content = "Cancel";
            cancelButton.Height = 48;
            cancelButton.FontSize = 15;
            cancelButton.FontWeight = FontWeights.SemiBold;
            cancelButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            cancelButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            cancelButton.BorderThickness = new Thickness(1.3);
            cancelButton.Cursor = Cursors.Hand;
            cancelButton.Click += (s, args) => dialog.Close();
            cancelButton.Template = CreateRoundedButtonTemplate(12);
            stack.Children.Add(cancelButton);

            dialog.ShowDialog();
        }

        private Button CreateActionChoiceButton(string title, string subtitle, string backgroundColor, string borderColor, string iconColor)
        {
            Button button = new Button();
            button.Height = 78;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor));
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor));
            button.BorderThickness = new Thickness(1.5);
            button.Cursor = Cursors.Hand;
            button.Template = CreateRoundedButtonTemplate(14);

            Grid grid = new Grid();
            grid.Margin = new Thickness(8, 0, 8, 0);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border icon = new Border();
            icon.Width = 40;
            icon.Height = 40;
            icon.CornerRadius = new CornerRadius(10);
            icon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconColor));
            icon.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(icon);

            StackPanel textPanel = new StackPanel();
            textPanel.Margin = new Thickness(16, 0, 0, 0);
            textPanel.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(textPanel, 1);
            grid.Children.Add(textPanel);

            textPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF475569"))
            });

            button.Content = grid;
            return button;
        }

        private ControlTemplate CreateRoundedButtonTemplate(double cornerRadius)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            return template;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
