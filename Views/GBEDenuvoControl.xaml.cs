using SolusManifestApp.ViewModels;
using System.Windows.Controls;

namespace SolusManifestApp.Views
{
    public partial class GBEDenuvoControl : UserControl
    {
        public GBEDenuvoControl()
        {
            InitializeComponent();
            DataContext = new GBEDenuvoViewModel();
        }
    }
}
