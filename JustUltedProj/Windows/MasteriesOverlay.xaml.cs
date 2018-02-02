using JustUltedProj.Logic;
using JustUltedProj.Windows.Profile;
using System.Windows;
using System.Windows.Controls;

namespace JustUltedProj.Windows
{
    /// <summary>
    /// Interaction logic for MasteriesOverlay.xaml
    /// </summary>
    public partial class MasteriesOverlay : Page
    {
        public MasteriesOverlay()
        {
            InitializeComponent();
            Container.Content = new Masteries().Content;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Client.OverlayContainer.Visibility = Visibility.Hidden;
        }
    }
}