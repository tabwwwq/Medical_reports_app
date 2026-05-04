using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public class FindDoctorWindow : Window
    {
        private int patientId;
        private FamilyDoctorService service = new FamilyDoctorService();
        private AvatarService avatarService = new AvatarService();
        private TextBox txtSearch;
        private StackPanel doctorsPanel;

        public FindDoctorWindow(int patientIdValue)
        {
            patientId = patientIdValue;
            Title = "Find a Doctor";
            Width = 900;
            Height = 700;
            MinWidth = 760;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));
            BuildUi();
            LoadDoctors();
        }

        private void BuildUi()
        {
            Grid root = new Grid { Margin = new Thickness(22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            StackPanel header = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            TextBlock title = new TextBlock
            {
                Text = "Find a Doctor",
                FontSize = 27,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            header.Children.Add(title);

            TextBlock subtitle = new TextBlock
            {
                Text = "Choose a doctor and send a request. Doctors with 10 family patients are hidden.",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4B5563"))
            };
            header.Children.Add(subtitle);

            Border searchBorder = new Border
            {
                Height = 48,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB")),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(searchBorder, 1);
            root.Children.Add(searchBorder);

            txtSearch = new TextBox
            {
                FontSize = 15,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            };
            txtSearch.TextChanged += delegate { LoadDoctors(); };
            searchBorder.Child = txtSearch;

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 8, 0)
            };
            Grid.SetRow(scroll, 2);
            doctorsPanel = new StackPanel();
            scroll.Content = doctorsPanel;
            root.Children.Add(scroll);

            Button close = CreateRoundedButton("Close", 46, 0);
            close.Background = Brushes.White;
            close.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"));
            close.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            close.Margin = new Thickness(0, 14, 0, 0);
            close.Click += delegate { Close(); };
            Grid.SetRow(close, 3);
            root.Children.Add(close);
        }

        private void LoadDoctors()
        {
            try
            {
                doctorsPanel.Children.Clear();
                List<DoctorSearchCard> doctors = service.SearchAvailableDoctors(patientId, txtSearch == null ? "" : txtSearch.Text);

                if (doctors.Count == 0)
                {
                    Border emptyCard = new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB")),
                        BorderThickness = new Thickness(1.2),
                        CornerRadius = new CornerRadius(18),
                        Padding = new Thickness(22),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    emptyCard.Child = new TextBlock
                    {
                        Text = "No available doctors found.",
                        FontSize = 16,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280"))
                    };
                    doctorsPanel.Children.Add(emptyCard);
                    return;
                }

                foreach (DoctorSearchCard doctor in doctors)
                {
                    doctorsPanel.Children.Add(CreateDoctorCard(doctor));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateDoctorCard(DoctorSearchCard doctor)
        {
            Border card = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB")),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 10)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            card.Child = grid;

            Border avatar = new Border
            {
                Width = 58,
                Height = 58,
                CornerRadius = new CornerRadius(29),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF94A3B8")),
                BorderThickness = new Thickness(1.5),
                VerticalAlignment = VerticalAlignment.Center
            };

            ImageBrush brush = avatarService.BuildAvatarBrush(doctor.AvatarUrl);
            if (brush == null)
            {
                avatar.Child = new TextBlock
                {
                    Text = BuildInitials(doctor.FullName),
                    FontSize = 17,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B"))
                };
            }
            else
            {
                avatar.Background = brush;
            }
            grid.Children.Add(avatar);

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(info, 1);

            info.Children.Add(new TextBlock
            {
                Text = doctor.FullName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827"))
            });

            info.Children.Add(new TextBlock
            {
                Text = "Specialization: " + doctor.Specialization,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4B5563")),
                Margin = new Thickness(0, 5, 0, 0)
            });

            info.Children.Add(new TextBlock
            {
                Text = "Family patients: " + doctor.CountText,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4B5563")),
                Margin = new Thickness(0, 5, 0, 0)
            });

            if (!string.IsNullOrWhiteSpace(doctor.RequestStatus))
            {
                info.Children.Add(new TextBlock
                {
                    Text = "Last request: " + doctor.RequestStatus,
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280")),
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            grid.Children.Add(info);

            Button send = CreateRoundedButton(doctor.RequestStatus == "Pending" ? "Pending" : "Send Request", 42, 140);
            send.Tag = doctor.DoctorId;
            send.IsEnabled = doctor.RequestStatus != "Pending";

            if (doctor.RequestStatus == "Pending")
            {
                send.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB"));
                send.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF64748B"));
                send.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1D5DB"));
            }
            else
            {
                send.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2563EB"));
                send.Foreground = Brushes.White;
                send.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1D4ED8"));
            }

            send.Click += Send_Click;
            Grid.SetColumn(send, 2);
            grid.Children.Add(send);

            return card;
        }

        private Button CreateRoundedButton(string text, double height, double width)
        {
            Button button = new Button
            {
                Content = text,
                Height = height,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(16, 0, 16, 0)
            };

            if (width > 0)
            {
                button.Width = width;
            }

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            button.Template = template;

            return button;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int doctorId = Convert.ToInt32(((Button)sender).Tag);
                service.SendRequest(patientId, doctorId);
                MessageBox.Show("Request sent.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDoctors();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildInitials(string name)
        {
            string clean = name.Replace("Dr.", "").Trim();
            string[] parts = clean.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string result = "DR";
            if (parts.Length == 1)
            {
                result = parts[0].Substring(0, 1).ToUpper();
            }
            if (parts.Length > 1)
            {
                result = parts[0].Substring(0, 1).ToUpper() + parts[1].Substring(0, 1).ToUpper();
            }
            return result;
        }
    }
}
