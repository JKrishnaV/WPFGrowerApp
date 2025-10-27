using System.Windows;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for AdvanceDeductionDialog.xaml
    /// </summary>
    public partial class AdvanceDeductionDialog : Window
    {
        public AdvanceDeductionItem SelectedDeduction { get; set; }

        public AdvanceDeductionDialog()
        {
            InitializeComponent();
        }

        private void SetToRemaining_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && 
                button.DataContext is AdvanceDeductionItem deduction)
            {
                deduction.SetToRemaining();
            }
        }

        private void ClearDeduction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && 
                button.DataContext is AdvanceDeductionItem deduction)
            {
                deduction.ClearDeduction();
            }
        }
    }
}
