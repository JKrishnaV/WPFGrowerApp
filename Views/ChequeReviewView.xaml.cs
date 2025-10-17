using System.Windows.Controls;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChequeReviewView.xaml
    /// </summary>
    public partial class ChequeReviewView : UserControl
    {
        public ChequeReviewView()
        {
            InitializeComponent();
            Loaded += (s, e) => Focus(); // Enable immediate keyboard shortcuts
        }
    }
}
