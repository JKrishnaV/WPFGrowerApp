using System;
using System.Windows;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Repositories;
using WPFGrowerApp.DataAccess.Services;

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        private DapperConnectionManager _connectionManager;
        private GrowerRepository _growerRepository;
        private AccountRepository _accountRepository;
        private ChequeRepository _chequeRepository;
        private GrowerService _growerService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services
            InitializeServices();

            // Create and show the main window
            var mainWindow = new MainWindow(_growerService);
            mainWindow.Show();
        }

        private void InitializeServices()
        {
            // Create connection manager
            _connectionManager = new DapperConnectionManager();

            // Create repositories
            _growerRepository = new GrowerRepository(_connectionManager);
            _accountRepository = new AccountRepository(_connectionManager);
            _chequeRepository = new ChequeRepository(_connectionManager);

            // Create services
            _growerService = new GrowerService(_growerRepository, _accountRepository, _chequeRepository);
        }
    }
}
