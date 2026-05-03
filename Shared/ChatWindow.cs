using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MedicalReportsApp.Classes;
using MedicalReportsApp.Services;

namespace MedicalReportsApp
{
    public class ChatWindow : Window
    {
        private int patientId;
        private int doctorId;
        private string viewerType;
        private MessageService messageService = new MessageService();
        private StackPanel messagesPanel;
        private TextBox txtMessage;
        private ScrollViewer scrollViewer;
        private TextBlock txtHeader;
        private TextBlock txtSubHeader;

        public ChatWindow(int patientId, int doctorId, string viewerType)
        {
            this.patientId = patientId;
            this.doctorId = doctorId;
            this.viewerType = viewerType;
            Width = 780;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));
            Title = "Messages";
            BuildUi();
            LoadChat();
        }

        private void BuildUi()
        {
            Grid root = new Grid();
            root.Margin = new Thickness(18);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            Border header = CreateCard();
            header.Padding = new Thickness(22, 18, 22, 18);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel headerPanel = new StackPanel();
            header.Child = headerPanel;
            txtHeader = new TextBlock { Text = "Messages", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brush("#FF111827") };
            txtSubHeader = new TextBlock { Text = "", FontSize = 14, Foreground = Brush("#FF6B7280"), Margin = new Thickness(0, 6, 0, 0) };
            headerPanel.Children.Add(txtHeader);
            headerPanel.Children.Add(txtSubHeader);

            Border body = CreateCard();
            body.Padding = new Thickness(16);
            body.Margin = new Thickness(0, 14, 0, 14);
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            body.Child = scrollViewer;
            messagesPanel = new StackPanel();
            scrollViewer.Content = messagesPanel;

            Border inputCard = CreateCard();
            inputCard.Padding = new Thickness(16);
            Grid.SetRow(inputCard, 2);
            root.Children.Add(inputCard);

            Grid inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inputCard.Child = inputGrid;

            txtMessage = new TextBox();
            txtMessage.Height = 44;
            txtMessage.FontSize = 15;
            txtMessage.Padding = new Thickness(14, 10, 14, 10);
            txtMessage.Background = Brushes.Transparent;
            txtMessage.BorderBrush = Brush("#FFD1D5DB");
            txtMessage.BorderThickness = new Thickness(1.2);
            txtMessage.VerticalContentAlignment = VerticalAlignment.Center;
            txtMessage.Template = RoundedTextBoxTemplate(12);
            inputGrid.Children.Add(txtMessage);

            Button sendButton = new Button();
            sendButton.Content = "Send";
            sendButton.Width = 78;
            sendButton.Height = 44;
            sendButton.Margin = new Thickness(12, 0, 0, 0);
            sendButton.Background = Brush("#FF2F6FED");
            sendButton.Foreground = Brushes.White;
            sendButton.FontWeight = FontWeights.Bold;
            sendButton.BorderThickness = new Thickness(0);
            sendButton.Template = RoundedButtonTemplate(22);
            sendButton.Click += btnSend_Click;
            Grid.SetColumn(sendButton, 1);
            inputGrid.Children.Add(sendButton);
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
            host.SetValue(ScrollViewer.MarginProperty, new Thickness(12, 0, 12, 0));
            host.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(host);
            return new ControlTemplate(typeof(TextBox)) { VisualTree = border };
        }

        private Border CreateCard()
        {
            Border border = new Border();
            border.Background = Brushes.White;
            border.BorderBrush = Brush("#FFD1D5DB");
            border.BorderThickness = new Thickness(1.2);
            border.CornerRadius = new CornerRadius(16);
            return border;
        }

        private SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void LoadChat()
        {
            try
            {
                bool allowed = viewerType == "Doctor" ? messageService.CanDoctorOpenChat(doctorId, patientId) : messageService.CanPatientOpenChat(patientId, doctorId);
                if (!allowed)
                {
                    MessageBox.Show("Chat is available only between patient and family doctor.", "Messages", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                    return;
                }

                ChatPersonInfo info = messageService.GetChatPersonInfo(patientId, doctorId);
                txtHeader.Text = viewerType == "Doctor" ? info.PatientName : info.DoctorName;
                txtSubHeader.Text = viewerType == "Doctor" ? "Family patient" : info.DoctorSpecialization;
                RenderMessages(messageService.GetMessages(patientId, doctorId, viewerType));
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
                TextBlock empty = new TextBlock();
                empty.Text = "No messages yet. Write the first message.";
                empty.FontSize = 15;
                empty.Foreground = Brush("#FF6B7280");
                empty.Margin = new Thickness(6);
                messagesPanel.Children.Add(empty);
                return;
            }

            foreach (ChatMessageCard message in messages)
            {
                Border bubble = new Border();
                bubble.MaxWidth = 480;
                bubble.Padding = new Thickness(14, 10, 14, 10);
                bubble.Margin = message.IsMine ? new Thickness(120, 0, 8, 12) : new Thickness(8, 0, 120, 12);
                bubble.HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                bubble.Background = message.IsMine ? Brush("#FF2F6FED") : Brush("#FFF8FAFC");
                bubble.BorderBrush = message.IsMine ? Brush("#FF1D4ED8") : Brush("#FFD1D5DB");
                bubble.BorderThickness = new Thickness(1.2);
                bubble.CornerRadius = new CornerRadius(12);

                StackPanel bubblePanel = new StackPanel();
                bubble.Child = bubblePanel;
                TextBlock text = new TextBlock();
                text.Text = message.MessageText;
                text.TextWrapping = TextWrapping.Wrap;
                text.FontSize = 15;
                text.Foreground = message.IsMine ? Brushes.White : Brush("#FF111827");
                bubblePanel.Children.Add(text);

                TextBlock time = new TextBlock();
                time.Text = message.TimeText;
                time.FontSize = 12;
                time.Margin = new Thickness(0, 6, 0, 0);
                time.Foreground = message.IsMine ? Brush("#FFE0ECFF") : Brush("#FF6B7280");
                bubblePanel.Children.Add(time);
                messagesPanel.Children.Add(bubble);
            }
            scrollViewer.ScrollToEnd();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                messageService.SendMessage(patientId, doctorId, viewerType, txtMessage.Text);
                txtMessage.Text = "";
                LoadChat();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
