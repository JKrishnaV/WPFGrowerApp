using System.Windows;

namespace WPFGrowerApp.Reports
{
    public partial class EmailDialog : Window
    {
        public string FromEmail { get; private set; }
        public string ToEmail { get; private set; }
        public string Subject { get; private set; }
        public string Body { get; private set; }
        public string SmtpServer { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }

        public EmailDialog()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            FromEmail = txtFromEmail.Text;
            ToEmail = txtToEmail.Text;
            Subject = txtSubject.Text;
            Body = txtBody.Text;
            SmtpServer = txtSmtpServer.Text;
            Port = int.Parse(txtPort.Text);
            Username = txtUsername.Text;
            Password = txtPassword.Password;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 