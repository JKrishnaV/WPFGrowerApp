using System.Windows.Controls;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChequeDeliveryView.xaml
    /// </summary>
    public partial class ChequeDeliveryView : UserControl
    {
        public ChequeDeliveryView()
        {
            InitializeComponent();
            Loaded += (s, e) => Focus(); // Enable immediate keyboard shortcuts
        }
    }
}
