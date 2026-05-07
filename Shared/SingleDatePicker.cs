using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MedicalReportsApp
{
    public class SingleDatePicker : Button
    {
        private DateTime? selectedDate;

        public DateTime? SelectedDate
        {
            get { return selectedDate; }
            set
            {
                selectedDate = value;
                UpdateText();
            }
        }

        public SingleDatePicker()
        {
            Height = 48;
            FontSize = 15;
            Background = Brushes.White;
            Foreground = BrushFrom("#FF1E293B");
            BorderBrush = BrushFrom("#FFD1D5DB");
            BorderThickness = new Thickness(1.5);
            Padding = new Thickness(16, 0, 16, 0);
            HorizontalContentAlignment = HorizontalAlignment.Left;
            VerticalContentAlignment = VerticalAlignment.Center;
            Cursor = System.Windows.Input.Cursors.Hand;
            Template = RoundedTemplate(16);
            Click += OpenPicker;
            UpdateText();
        }

        private void OpenPicker(object sender, RoutedEventArgs e)
        {
            DateTime startDate = SelectedDate ?? DateTime.Today;
            if (startDate.Year < 1900) startDate = new DateTime(1900, 1, 1);
            if (startDate.Year > 2026) startDate = new DateTime(2026, 12, 31);

            SingleDatePickerWindow window = new SingleDatePickerWindow(startDate);
            window.Owner = Window.GetWindow(this);

            bool? result = window.ShowDialog();
            if (result == true)
            {
                SelectedDate = window.SelectedDate;
            }
        }

        private void UpdateText()
        {
            Content = SelectedDate.HasValue ? SelectedDate.Value.ToString("dd-MM-yyyy") : "Select date";
        }

        private static SolidColorBrush BrushFrom(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private static ControlTemplate RoundedTemplate(double radius)
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, new TemplateBindingExtension(Button.HorizontalContentAlignmentProperty));
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, new TemplateBindingExtension(Button.VerticalContentAlignmentProperty));
            content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
            border.AppendChild(content);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            return template;
        }
    }

    public class SingleDatePickerWindow : Window
    {
        private const int MinYear = 1900;
        private const int MaxYear = 2026;

        private ComboBox cmbMonth;
        private TextBox txtYear;
        private Slider yearSlider;
        private TextBlock yearSliderText;
        private UniformGrid daysGrid;
        private DateTime displayMonth;
        private DateTime? selectedDate;
        private bool isLoading;
        private readonly string[] months = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;

        public DateTime? SelectedDate
        {
            get { return selectedDate; }
        }

        public SingleDatePickerWindow(DateTime startDate)
        {
            if (startDate.Year < MinYear) startDate = new DateTime(MinYear, 1, 1);
            if (startDate.Year > MaxYear) startDate = new DateTime(MaxYear, 12, 31);
            selectedDate = startDate.Date;
            displayMonth = new DateTime(startDate.Year, startDate.Month, 1);
            Title = "Select Date";
            Width = 760;
            Height = 790;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.White;
            BuildLayout();
            LoadMonth();
        }

        private void BuildLayout()
        {
            Border card = new Border();
            card.Background = Brushes.White;
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(28, 24, 28, 22);
            card.Margin = new Thickness(10);
            card.BorderBrush = BrushFrom("#FFD1D5DB");
            card.BorderThickness = new Thickness(1.2);

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            card.Child = root;

            Grid top = new Grid { Margin = new Thickness(0, 0, 0, 28) };
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button prev = SmallArrowButton("‹");
            prev.Click += delegate { MoveMonth(-1); };
            Grid.SetColumn(prev, 0);
            top.Children.Add(prev);

            cmbMonth = new ComboBox
            {
                Height = 52,
                FontSize = 20,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(12, 0, 12, 0),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = BrushFrom("#FFD1D5DB"),
                BorderThickness = new Thickness(1.4),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            for (int i = 0; i < 12; i++) cmbMonth.Items.Add(months[i]);
            cmbMonth.SelectionChanged += delegate { if (!isLoading) ApplyHeaderDate(); };
            Grid.SetColumn(cmbMonth, 1);
            top.Children.Add(cmbMonth);

            Border yearBox = new Border
            {
                Height = 52,
                Background = Brushes.White,
                BorderBrush = BrushFrom("#FFD1D5DB"),
                BorderThickness = new Thickness(1.4),
                CornerRadius = new CornerRadius(16)
            };

            Grid yearGrid = new Grid();
            yearGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            yearGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            yearGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            yearGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            txtYear = new TextBox
            {
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(16, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Black,
                MaxLength = 4,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            txtYear.PreviewTextInput += YearPreviewTextInput;
            txtYear.TextChanged += YearTextChanged;
            txtYear.LostFocus += YearLostFocus;
            DataObject.AddPastingHandler(txtYear, YearPaste);
            Grid.SetColumn(txtYear, 0);
            Grid.SetRowSpan(txtYear, 2);
            yearGrid.Children.Add(txtYear);

            Button yearUp = YearStepperButton("⌃");
            yearUp.Click += delegate { ChangeYear(1); };
            Grid.SetColumn(yearUp, 1);
            Grid.SetRow(yearUp, 0);
            yearGrid.Children.Add(yearUp);

            Button yearDown = YearStepperButton("⌄");
            yearDown.Click += delegate { ChangeYear(-1); };
            Grid.SetColumn(yearDown, 1);
            Grid.SetRow(yearDown, 1);
            yearGrid.Children.Add(yearDown);

            yearBox.Child = yearGrid;
            Grid.SetColumn(yearBox, 3);
            top.Children.Add(yearBox);

            Button next = SmallArrowButton("›");
            next.Click += delegate { MoveMonth(1); };
            Grid.SetColumn(next, 4);
            top.Children.Add(next);

            Grid.SetRow(top, 0);
            root.Children.Add(top);

            Grid sliderBox = new Grid { Margin = new Thickness(6, 0, 6, 24) };
            sliderBox.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sliderBox.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sliderBox.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sliderBox.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sliderBox.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock minText = new TextBlock
            {
                Text = MinYear.ToString(),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFrom("#FF64748B"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(minText, 0);
            Grid.SetColumn(minText, 0);
            sliderBox.Children.Add(minText);

            yearSliderText = new TextBlock
            {
                Text = MaxYear.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFrom("#FFFF2D42"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(yearSliderText, 0);
            Grid.SetColumn(yearSliderText, 1);
            sliderBox.Children.Add(yearSliderText);

            TextBlock maxText = new TextBlock
            {
                Text = MaxYear.ToString(),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFrom("#FF64748B"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(maxText, 0);
            Grid.SetColumn(maxText, 2);
            sliderBox.Children.Add(maxText);

            yearSlider = new Slider
            {
                Minimum = MinYear,
                Maximum = MaxYear,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Height = 42,
                Margin = new Thickness(12, 4, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            yearSlider.ValueChanged += delegate { if (!isLoading) ApplySliderYear(); };
            Grid.SetRow(yearSlider, 1);
            Grid.SetColumnSpan(yearSlider, 3);
            sliderBox.Children.Add(yearSlider);

            Grid.SetRow(sliderBox, 1);
            root.Children.Add(sliderBox);

            UniformGrid week = new UniformGrid { Columns = 7, Margin = new Thickness(24, 0, 24, 28) };
            string[] names = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };
            foreach (string name in names)
            {
                week.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFrom("#FF64748B"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            Grid.SetRow(week, 2);
            root.Children.Add(week);

            daysGrid = new UniformGrid { Columns = 7, Rows = 6, Height = 408, Margin = new Thickness(26, 2, 26, 8) };
            Grid.SetRow(daysGrid, 3);
            root.Children.Add(daysGrid);

            Grid bottom = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button done = FooterButton("Done", true);
            done.Click += delegate { DialogResult = true; Close(); };
            Grid.SetColumn(done, 1);
            bottom.Children.Add(done);

            Grid.SetRow(bottom, 4);
            root.Children.Add(bottom);

            Content = card;
        }

        private Button SmallArrowButton(string text)
        {
            Button button = new Button
            {
                Content = text,
                Width = 38,
                Height = 52,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Background = Brushes.White,
                BorderBrush = BrushFrom("#00FFFFFF"),
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Black,
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Template = RoundedButtonTemplate(19)
            };
            return button;
        }

        private Button FooterButton(string text, bool primary)
        {
            Button button = new Button
            {
                Content = text,
                Width = 112,
                Height = 54,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = RoundedButtonTemplate(15)
            };
            button.Background = primary ? BrushFrom("#FFFF2D42") : Brushes.White;
            button.Foreground = primary ? Brushes.White : BrushFrom("#FF1E293B");
            button.BorderBrush = primary ? BrushFrom("#FFE11D2E") : BrushFrom("#FFD1D5DB");
            button.BorderThickness = new Thickness(1.4);
            return button;
        }

        private Button DayButton(DateTime date, bool isSelected)
        {
            Button btn = new Button();
            btn.Content = date.Day.ToString("00");
            btn.Width = 56;
            btn.Height = 56;
            btn.Margin = new Thickness(6);
            btn.HorizontalAlignment = HorizontalAlignment.Center;
            btn.VerticalAlignment = VerticalAlignment.Center;
            btn.FontSize = 20;
            btn.FontWeight = FontWeights.SemiBold;
            btn.Cursor = System.Windows.Input.Cursors.Hand;
            btn.Tag = date;
            btn.Background = isSelected ? BrushFrom("#FFFF2D42") : Brushes.White;
            btn.Foreground = isSelected ? Brushes.White : Brushes.Black;
            btn.BorderBrush = isSelected ? BrushFrom("#FFE11D2E") : Brushes.Transparent;
            btn.BorderThickness = isSelected ? new Thickness(1.2) : new Thickness(0);
            btn.Template = RoundedButtonTemplate(17);
            btn.Click += SelectDay;
            return btn;
        }

        private static ControlTemplate RoundedButtonTemplate(double radius)
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

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            return template;
        }

        private static SolidColorBrush BrushFrom(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void LoadMonth()
        {
            if (displayMonth.Year < MinYear) displayMonth = new DateTime(MinYear, displayMonth.Month, 1);
            if (displayMonth.Year > MaxYear) displayMonth = new DateTime(MaxYear, displayMonth.Month, 1);

            isLoading = true;
            cmbMonth.SelectedIndex = displayMonth.Month - 1;
            if (txtYear != null) txtYear.Text = displayMonth.Year.ToString();
            if (yearSlider != null) yearSlider.Value = displayMonth.Year;
            if (yearSliderText != null) yearSliderText.Text = displayMonth.Year.ToString();
            isLoading = false;

            daysGrid.Children.Clear();
            int empty = (int)displayMonth.DayOfWeek;
            int days = DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month);

            for (int i = 0; i < empty; i++) daysGrid.Children.Add(new Border());

            for (int day = 1; day <= days; day++)
            {
                DateTime date = new DateTime(displayMonth.Year, displayMonth.Month, day);
                bool isSelected = selectedDate.HasValue && selectedDate.Value.Date == date.Date;
                daysGrid.Children.Add(DayButton(date, isSelected));
            }

            while (daysGrid.Children.Count < 42) daysGrid.Children.Add(new Border());
        }

        private void SelectDay(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            selectedDate = ((DateTime)btn.Tag).Date;
            LoadMonth();
        }

        private void MoveMonth(int offset)
        {
            DateTime next = displayMonth.AddMonths(offset);
            if (next.Year < MinYear) next = new DateTime(MinYear, 1, 1);
            if (next.Year > MaxYear) next = new DateTime(MaxYear, 12, 1);
            displayMonth = next;
            LoadMonth();
        }

        private void ApplySliderYear()
        {
            int year = (int)Math.Round(yearSlider.Value);
            if (year < MinYear) year = MinYear;
            if (year > MaxYear) year = MaxYear;

            int month = displayMonth.Month;
            displayMonth = new DateTime(year, month, 1);
            LoadMonth();
        }

        private Button YearStepperButton(string text)
        {
            Button button = new Button
            {
                Content = text,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = BrushFrom("#FF64748B"),
                Cursor = Cursors.Hand,
                Padding = new Thickness(0),
                Template = RoundedButtonTemplate(12)
            };
            return button;
        }

        private void ChangeYear(int offset)
        {
            int year = displayMonth.Year + offset;
            if (year < MinYear) year = MinYear;
            if (year > MaxYear) year = MaxYear;
            displayMonth = new DateTime(year, displayMonth.Month, 1);
            LoadMonth();
        }

        private void YearPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void YearPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrWhiteSpace(text) || text.Length > 4)
            {
                e.CancelCommand();
                return;
            }

            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                {
                    e.CancelCommand();
                    return;
                }
            }
        }

        private void YearTextChanged(object sender, TextChangedEventArgs e)
        {
            if (isLoading || txtYear == null) return;

            string text = txtYear.Text.Trim();
            if (text.Length != 4) return;

            int year;
            if (!int.TryParse(text, out year)) return;
            if (year < MinYear || year > MaxYear) return;

            int month = cmbMonth.SelectedIndex < 0 ? displayMonth.Month : cmbMonth.SelectedIndex + 1;
            displayMonth = new DateTime(year, month, 1);
            LoadMonth();
        }

        private void YearLostFocus(object sender, RoutedEventArgs e)
        {
            if (txtYear == null) return;

            int year;
            if (!int.TryParse(txtYear.Text, out year)) year = displayMonth.Year;
            if (year < MinYear) year = MinYear;
            if (year > MaxYear) year = MaxYear;

            displayMonth = new DateTime(year, displayMonth.Month, 1);
            LoadMonth();
        }

        private void ApplyHeaderDate()
        {
            int year = displayMonth.Year;

            if (txtYear != null)
            {
                int typedYear;
                if (int.TryParse(txtYear.Text, out typedYear) && txtYear.Text.Trim().Length == 4) year = typedYear;
            }

            if (year < MinYear) year = MinYear;
            if (year > MaxYear) year = MaxYear;

            int month = cmbMonth.SelectedIndex < 0 ? displayMonth.Month : cmbMonth.SelectedIndex + 1;
            displayMonth = new DateTime(year, month, 1);
            LoadMonth();
        }
    }
}
