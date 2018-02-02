using System.Windows.Controls;
using System.Windows.Navigation;

namespace JustUltedProj.Windows
{
    /// <summary>
    /// Interaction logic for FakePage.xaml
    /// </summary>
    public partial class FakePage : Page
    {
        public FakePage()
        {
            InitializeComponent();
        }

        public NavigationService getNavigationService()
        {
            return this.NavigationService;
        }
    }
}