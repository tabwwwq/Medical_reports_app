using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public class DoctorMessagesWindow : Window
    {
        private int doctorId;
        private int selectedPatientId;
        private DoctorDashboardService dashboardService = new DoctorDashboardService();
        private MessageService messageService = new MessageService();
        private AvatarService avatarService = new AvatarService();
        private StackPanel conversationsPanel;
        private StackPanel messagesPanel;
        private ScrollViewer messagesScrollViewer;
        private TextBox txtSearch;
        private TextBox txtMessage;
        private TextBlock txtChatName;
        private TextBlock txtChatSubTitle;
        private Border chatAvatar;
        private List<DoctorDashboardPatientCard> loadedPatients = new List<DoctorDashboardPatientCard>();

        public DoctorMessagesWindow(int doctorId)
        {
            this.doctorId = doctorId;
            Title = "Messages";
            Width = 1280;
            Height = 720;
            MinWidth = 1000;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brush("#FFF8FAFC");
            BuildUi();
            LoadPatients();
        }

        private void BuildUi()
        {
            Grid root = new Grid();
            root.Margin = new Thickness(8);
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Content = root;

            Border leftCard = CreateCard(12);
            leftCard.Margin = new Thickness(0, 0, 12, 0);
            leftCard.Padding = new Thickness(24, 24, 24, 18);
            Grid.SetColumn(leftCard, 0);
            root.Children.Add(leftCard);

            Grid leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftCard.Child = leftGrid;

            TextBlock title = new TextBlock();
            title.Text = "Messages";
            title.FontSize = 24;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = Brush("#FF020617");
            Grid.SetRow(title, 0);
            leftGrid.Children.Add(title);

            Border searchBorder = new Border();
            searchBorder.Height = 44;
            searchBorder.Margin = new Thickness(0, 18, 0, 16);
            searchBorder.Background = Brushes.White;
            searchBorder.BorderBrush = Brush("#FFCBD5E1");
            searchBorder.BorderThickness = new Thickness(1.4);
            searchBorder.CornerRadius = new CornerRadius(9);
            Grid.SetRow(searchBorder, 1);
            leftGrid.Children.Add(searchBorder);

            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchBorder.Child = searchGrid;

            TextBlock searchIcon = new TextBlock();
            searchIcon.Text = "⌕";
            searchIcon.FontSize = 22;
            searchIcon.Foreground = Brush("#FF94A3B8");
            searchIcon.HorizontalAlignment = HorizontalAlignment.Center;
            searchIcon.VerticalAlignment = VerticalAlignment.Center;
            searchGrid.Children.Add(searchIcon);

            txtSearch = new TextBox();
            txtSearch.BorderThickness = new Thickness(0);
            txtSearch.Background = Brushes.Transparent;
            txtSearch.FontSize = 15;
            txtSearch.VerticalContentAlignment = VerticalAlignment.Center;
            txtSearch.Padding = new Thickness(0, 0, 10, 0);
            txtSearch.TextChanged += txtSearch_TextChanged;
            Grid.SetColumn(txtSearch, 1);
            searchGrid.Children.Add(txtSearch);

            ScrollViewer listScroll = new ScrollViewer();
            listScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Grid.SetRow(listScroll, 2);
            leftGrid.Children.Add(listScroll);
            conversationsPanel = new StackPanel();
            listScroll.Content = conversationsPanel;

            Border chatCard = CreateCard(8);
            Grid.SetColumn(chatCard, 1);
            root.Children.Add(chatCard);

            Grid chatGrid = new Grid();
            chatGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(114) });
            chatGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            chatGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) });
            chatCard.Child = chatGrid;

            Border chatHeader = new Border();
            chatHeader.Background = Brushes.White;
            chatHeader.BorderBrush = Brush("#FFCBD5E1");
            chatHeader.BorderThickness = new Thickness(0, 0, 0, 1.2);
            chatHeader.Padding = new Thickness(24, 18, 24, 18);
            Grid.SetRow(chatHeader, 0);
            chatGrid.Children.Add(chatHeader);

            StackPanel headerRow = new StackPanel();
            headerRow.Orientation = Orientation.Horizontal;
            headerRow.VerticalAlignment = VerticalAlignment.Center;
            chatHeader.Child = headerRow;

            chatAvatar = CreateAvatarBorder(62);
            headerRow.Children.Add(chatAvatar);

            StackPanel headerText = new StackPanel();
            headerText.Margin = new Thickness(16, 7, 0, 0);
            headerRow.Children.Add(headerText);

            txtChatName = new TextBlock();
            txtChatName.Text = "Select a patient";
            txtChatName.FontSize = 18;
            txtChatName.FontWeight = FontWeights.Bold;
            txtChatName.Foreground = Brush("#FF020617");
            headerText.Children.Add(txtChatName);

            txtChatSubTitle = new TextBlock();
            txtChatSubTitle.Text = "Open a conversation from the left side";
            txtChatSubTitle.FontSize = 14;
            txtChatSubTitle.Margin = new Thickness(0, 6, 0, 0);
            txtChatSubTitle.Foreground = Brush("#FF334155");
            headerText.Children.Add(txtChatSubTitle);

            messagesScrollViewer = new ScrollViewer();
            messagesScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            messagesScrollViewer.Background = Brushes.White;
            messagesScrollViewer.Padding = new Thickness(24);
            Grid.SetRow(messagesScrollViewer, 1);
            chatGrid.Children.Add(messagesScrollViewer);
            messagesPanel = new StackPanel();
            messagesScrollViewer.Content = messagesPanel;

            Border inputBorder = new Border();
            inputBorder.Background = Brushes.White;
            inputBorder.BorderBrush = Brush("#FFCBD5E1");
            inputBorder.BorderThickness = new Thickness(0, 1.2, 0, 0);
            inputBorder.Padding = new Thickness(24, 18, 24, 18);
            Grid.SetRow(inputBorder, 2);
            chatGrid.Children.Add(inputBorder);

            Grid inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inputBorder.Child = inputGrid;

            txtMessage = new TextBox();
            txtMessage.Height = 52;
            txtMessage.FontSize = 15;
            txtMessage.Padding = new Thickness(16, 0, 16, 0);
            txtMessage.Background = Brushes.Transparent;
            txtMessage.BorderBrush = Brush("#FFCBD5E1");
            txtMessage.BorderThickness = new Thickness(1.4);
            txtMessage.VerticalContentAlignment = VerticalAlignment.Center;
            txtMessage.Template = RoundedTextBoxTemplate(9);
            txtMessage.KeyDown += txtMessage_KeyDown;
            inputGrid.Children.Add(txtMessage);

            Button sendButton = new Button();
            sendButton.Content = "Send";
            sendButton.Width = 116;
            sendButton.Height = 52;
            sendButton.Margin = new Thickness(14, 0, 0, 0);
            sendButton.Background = Brush("#FF2F6FED");
            sendButton.Foreground = Brushes.White;
            sendButton.FontWeight = FontWeights.Bold;
            sendButton.FontSize = 15;
            sendButton.BorderThickness = new Thickness(0);
            sendButton.Cursor = Cursors.Hand;
            sendButton.Template = RoundedButtonTemplate(9);
            sendButton.Click += btnSend_Click;
            Grid.SetColumn(sendButton, 1);
            inputGrid.Children.Add(sendButton);
        }

        private void LoadPatients()
        {
            try
            {
                string search = txtSearch == null ? "" : txtSearch.Text.Trim();
                loadedPatients = dashboardService.GetDoctorPatients(doctorId, search);
                RenderConversations();
                if (selectedPatientId == 0 && loadedPatients.Count > 0)
                {
                    SelectPatient(loadedPatients[0]);
                }
                if (loadedPatients.Count == 0)
                {
                    RenderEmptyChat("No family patients found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderConversations()
        {
            conversationsPanel.Children.Clear();
            foreach (DoctorDashboardPatientCard patient in loadedPatients)
            {
                conversationsPanel.Children.Add(CreateConversationCard(patient));
            }
        }

        private Border CreateConversationCard(DoctorDashboardPatientCard patient)
        {
            bool selected = patient.PatientId == selectedPatientId;
            Border card = new Border();
            card.Background = selected ? Brush("#FFEFF6FF") : Brush("#FFF8FAFC");
            card.BorderBrush = selected ? Brush("#FF2F6FED") : Brush("#FFCBD5E1");
            card.BorderThickness = new Thickness(selected ? 2 : 1.4);
            card.CornerRadius = new CornerRadius(9);
            card.Padding = new Thickness(16);
            card.Margin = new Thickness(0, 0, 0, 10);
            card.Cursor = Cursors.Hand;
            card.Tag = patient;
            card.MouseLeftButtonUp += conversationCard_MouseLeftButtonUp;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            card.Child = grid;

            Border avatar = CreateAvatarBorder(60);
            FillAvatar(avatar, patient.AvatarUrl, Initials(patient.FullName));
            grid.Children.Add(avatar);

            StackPanel info = new StackPanel();
            info.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            Grid nameRow = new Grid();
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            info.Children.Add(nameRow);

            TextBlock name = new TextBlock();
            name.Text = patient.FullName;
            name.FontSize = 15;
            name.FontWeight = FontWeights.Bold;
            name.Foreground = Brush("#FF020617");
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            nameRow.Children.Add(name);

            TextBlock time = new TextBlock();
            time.Text = "Patient";
            time.FontSize = 12;
            time.Foreground = Brush("#FF475569");
            Grid.SetColumn(time, 1);
            nameRow.Children.Add(time);

            TextBlock subtitle = new TextBlock();
            subtitle.Text = "Family patient";
            subtitle.FontSize = 14;
            subtitle.Margin = new Thickness(0, 6, 0, 0);
            subtitle.Foreground = Brush("#FF334155");
            info.Children.Add(subtitle);

            TextBlock preview = new TextBlock();
            preview.Text = "Click to open conversation";
            preview.FontSize = 14;
            preview.Margin = new Thickness(0, 6, 0, 0);
            preview.Foreground = Brush("#FF334155");
            preview.TextTrimming = TextTrimming.CharacterEllipsis;
            info.Children.Add(preview);

            return card;
        }

        private void conversationCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border card = sender as Border;
            DoctorDashboardPatientCard patient = card == null ? null : card.Tag as DoctorDashboardPatientCard;
            if (patient != null)
            {
                SelectPatient(patient);
            }
        }

        private void SelectPatient(DoctorDashboardPatientCard patient)
        {
            selectedPatientId = patient.PatientId;
            txtChatName.Text = patient.FullName;
            txtChatSubTitle.Text = "Family patient";
            FillAvatar(chatAvatar, patient.AvatarUrl, Initials(patient.FullName));
            RenderConversations();
            LoadChat();
        }

        private void LoadChat()
        {
            try
            {
                if (selectedPatientId == 0)
                {
                    RenderEmptyChat("Select a patient to start messaging.");
                    return;
                }
                if (!messageService.CanDoctorOpenChat(doctorId, selectedPatientId))
                {
                    RenderEmptyChat("Chat is available only between doctor and family patient.");
                    return;
                }
                RenderMessages(messageService.GetMessages(selectedPatientId, doctorId, "Doctor"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderMessages(List<ChatMessageCard> messages)
        {
            messagesPanel.Children.Clear();
            if (messages.Count == 0)
            {
                RenderEmptyChat("No messages yet. Write the first message.");
                return;
            }

            foreach (ChatMessageCard message in messages)
            {
                Border bubble = new Border();
                bubble.MaxWidth = 520;
                bubble.Padding = new Thickness(16, 12, 16, 10);
                bubble.Margin = message.IsMine ? new Thickness(220, 0, 0, 18) : new Thickness(0, 0, 220, 18);
                bubble.HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                bubble.Background = message.IsMine ? Brush("#FF2F6FED") : Brush("#FFF8FAFC");
                bubble.BorderBrush = message.IsMine ? Brush("#FF1D4ED8") : Brush("#FFCBD5E1");
                bubble.BorderThickness = new Thickness(1.4);
                bubble.CornerRadius = new CornerRadius(9);

                StackPanel bubblePanel = new StackPanel();
                bubble.Child = bubblePanel;

                TextBlock text = new TextBlock();
                text.Text = message.MessageText;
                text.TextWrapping = TextWrapping.Wrap;
                text.FontSize = 14;
                text.Foreground = message.IsMine ? Brushes.White : Brush("#FF020617");
                bubblePanel.Children.Add(text);

                TextBlock time = new TextBlock();
                time.Text = message.TimeText;
                time.FontSize = 12;
                time.Margin = new Thickness(0, 8, 0, 0);
                time.Foreground = message.IsMine ? Brush("#FFE0ECFF") : Brush("#FF475569");
                bubblePanel.Children.Add(time);
                messagesPanel.Children.Add(bubble);
            }
            messagesScrollViewer.ScrollToEnd();
        }

        private void RenderEmptyChat(string text)
        {
            messagesPanel.Children.Clear();
            TextBlock empty = new TextBlock();
            empty.Text = text;
            empty.FontSize = 15;
            empty.Foreground = Brush("#FF64748B");
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            empty.VerticalAlignment = VerticalAlignment.Center;
            empty.Margin = new Thickness(0, 180, 0, 0);
            messagesPanel.Children.Add(empty);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendCurrentMessage();
                e.Handled = true;
            }
        }

        private void SendCurrentMessage()
        {
            try
            {
                if (selectedPatientId == 0)
                {
                    MessageBox.Show("Select a patient first.", "Messages", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                messageService.SendMessage(selectedPatientId, doctorId, "Doctor", txtMessage.Text);
                txtMessage.Text = "";
                LoadChat();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadPatients();
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

        private Border CreateAvatarBorder(double size)
        {
            Border avatar = new Border();
            avatar.Width = size;
            avatar.Height = size;
            avatar.CornerRadius = new CornerRadius(size / 2);
            avatar.Background = Brush("#FFE5E7EB");
            avatar.BorderBrush = Brush("#FF94A3B8");
            avatar.BorderThickness = new Thickness(2);
            return avatar;
        }

        private void FillAvatar(Border avatar, string avatarUrl, string initials)
        {
            avatar.Child = null;
            avatar.Background = Brush("#FFE5E7EB");
            ImageBrush brush = avatarService.BuildAvatarBrush(avatarUrl);
            if (brush == null)
            {
                TextBlock text = new TextBlock();
                text.Text = initials;
                text.FontSize = 20;
                text.Foreground = Brush("#FF475569");
                text.HorizontalAlignment = HorizontalAlignment.Center;
                text.VerticalAlignment = VerticalAlignment.Center;
                avatar.Child = text;
            }
            else
            {
                avatar.Background = brush;
            }
        }

        private string Initials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "PT";
            }
            string[] parts = fullName.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string result = "";
            if (parts.Length > 0 && parts[0].Length > 0)
            {
                result += parts[0].Substring(0, 1).ToUpper();
            }
            if (parts.Length > 1 && parts[1].Length > 0)
            {
                result += parts[1].Substring(0, 1).ToUpper();
            }
            return result == "" ? "PT" : result;
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

        private ControlTemplate RoundedTextBoxTemplate(double radius)
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            FrameworkElementFactory host = new FrameworkElementFactory(typeof(ScrollViewer));
            host.Name = "PART_ContentHost";
            host.SetValue(ScrollViewer.MarginProperty, new Thickness(8, 0, 8, 0));
            host.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(host);
            return new ControlTemplate(typeof(TextBox)) { VisualTree = border };
        }

        private SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }
}
