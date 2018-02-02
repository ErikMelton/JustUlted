﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JustUltedProj.GUIElements
{
    /// <summary>
    /// Interaction logic for NotifyPlayerPopup.xaml
    /// </summary>
    public partial class NotifyPlayerPopup : UserControl
    {
        public NotifyPlayerPopup(string title, string Content)
        {
            InitializeComponent();
            NotificationTypeLabel.Content = title;
            NotificationTextBox.Text = Content;
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
