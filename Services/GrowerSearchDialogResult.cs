namespace WPFGrowerApp.Services
{
    public class GrowerSearchDialogResult
    {
        public bool? DialogResult { get; set; }
        public string? SelectedGrowerNumber { get; set; }

        public GrowerSearchDialogResult(bool? dialogResult, string? selectedGrowerNumber)
        {
            DialogResult = dialogResult;
            SelectedGrowerNumber = selectedGrowerNumber;
        }
    }
}
