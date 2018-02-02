using JustUltedProj.Logic;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using System.Windows.Media;
using System.Windows.Threading;
using JustUltedProj.Windows;

namespace JustUltedProj.GUIElements
{
    /// <summary>
    /// Interaction logic for JoinQueue.xaml
    /// </summary>
    public partial class JoinQueue : UserControl
    {
        public double queueID { get; set; }

        public JoinQueue()
        {
            InitializeComponent();
        }
        
    }
}