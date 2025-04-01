using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Services;
using WPFGrowerApp.Views;
using WPFGrowerApp.Infrastructure.Security;
using WPFGrowerApp.Infrastructure.Logging;
using System.Threading.Tasks;

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            SetupExceptionHandling();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DataAccess Services
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IAuditService, AuditService>();
            services.AddTransient<IBankRecService, BankRecService>();
            services.AddTransient<IChequeService, ChequeService>();
            services.AddTransient<IFileImportService, FileImportService>();
            services.AddTransient<IGrowerService, GrowerService>();
            services.AddTransient<IImportBatchProcessor, ImportBatchProcessor>();
            services.AddTransient<IImportBatchService, ImportBatchService>();
            services.AddTransient<IPayGroupService, PayGroupService>();
            services.AddTransient<IReceiptService, ReceiptService>();
            services.AddTransient<ValidationService>();
            services.AddTransient<IUserService, UserService>();

            // Register Other Services
            services.AddTransient<ReportExportService>();
            services.AddSingleton<IDialogService, DialogService>();

            // Register ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<GrowerViewModel>();
            services.AddTransient<GrowerSearchViewModel>();
            services.AddTransient<ImportViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LoginViewModel>();

            // Register Views
            services.AddTransient<GrowerSearchView>();
            services.AddTransient<LoginView>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cXmVCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXxceXVcRWReUUZ1V0dWYUo=");
            Logger.Info("Application starting up.");
            base.OnStartup(e);

            // Check for password setting arguments
            if (e.Args.Length == 3 && e.Args[0].Equals("--set-password", StringComparison.OrdinalIgnoreCase))
            {
                string username = e.Args[1];
                string password = e.Args[2];
                SetUserPasswordAndExit(username, password);
                return;
            }

            try
            {
                // Create MainWindow first but don't show it
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow = mainWindow;

                // Show LoginView
                Logger.Info("Resolving LoginView.");
                var loginView = ServiceProvider.GetRequiredService<LoginView>();
                Logger.Info("Showing LoginView dialog.");
                
                // Center LoginView relative to screen
                loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                
                bool? loginResult = loginView.ShowDialog();
                Logger.Info($"LoginView dialog closed with result: {loginResult?.ToString() ?? "null"}");

                if (loginResult == true)
                {
                    Logger.Info("Login successful. Showing MainWindow.");
                    mainWindow.Show();
                    Logger.Info("MainWindow shown successfully.");
                }
                else
                {
                    Logger.Info("Login cancelled. Shutting down application.");
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal("Critical error during application startup.", ex);
                MessageBox.Show($"A critical error occurred during startup: {ex.Message}", 
                               "Startup Error", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
                Application.Current.Shutdown(-1);
            }
        }

        private async void SetUserPasswordAndExit(string username, string password)
        {
            Console.WriteLine($"Attempting to set password for user: {username}");
            bool success = false;
            try
            {
                var userService = ServiceProvider.GetRequiredService<IUserService>();
                success = await userService.SetPasswordAsync(username, password);
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Password for user '{username}' set successfully.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failed to set password for user '{username}'. User might not exist.");
                }
            }
            catch (Exception ex)
            {
                 Console.ForegroundColor = ConsoleColor.Red;
                 Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                 Console.ResetColor();
                 Application.Current.Shutdown(); // Explicitly shutdown after setting password
            }
        }

        private void SetupExceptionHandling()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                Logger.Fatal("Unhandled UI exception occurred.", e.Exception);
                MessageBox.Show($"An unexpected UI error occurred: {e.Exception.Message}\n\nThe application may need to close.", "Unhandled UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logger.Fatal("Unobserved background task exception occurred.", e.Exception);
                e.SetObserved();
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                 Logger.Fatal("Unhandled non-UI thread exception occurred.", e.ExceptionObject as Exception);
            };
             Logger.Info("Global exception handlers configured.");
        }
    }
}
