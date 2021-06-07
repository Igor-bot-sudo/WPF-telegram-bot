using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfTelegramBot
{
    public partial class MainWindow : Window
    {
        private readonly TelegramMessageClient telegramMessageClient;
        private readonly Bitcoin bitCoin;
        private FilesListWindow filesListWindow;

        public MainWindow()
        {
            InitializeComponent();

            telegramMessageClient = new TelegramMessageClient(this);

            LogList.ItemsSource = telegramMessageClient.BotMessageLog;
            filesListWindow = new FilesListWindow();
            filesListWindow.FilesListBox.ItemsSource = telegramMessageClient.filesList;

            bitCoin = new Bitcoin(this);
            bitCoin.StartBitcoinChart();
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageTextBox.Text != "")
            {
                telegramMessageClient.SendMessage(MessageTextBox.Text, IdTextBox.Text);
                MessageTextBox.Clear();
            }
        }

        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            JsonOps.JsonSerializeMessageLog(telegramMessageClient.BotMessageLog);
        }

        private void FilesListButton_Click(object sender, RoutedEventArgs e)
        {
            filesListWindow.Show();
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.Width < 650)
            {
                this.Width = 650;
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
                this.ResizeMode = ResizeMode.CanResize;

            if (this.Height < 770)
            {
                this.Height = 770;
                this.ResizeMode = ResizeMode.NoResize;
            }
            else
                this.ResizeMode = ResizeMode.CanResize;
        }
    }
}