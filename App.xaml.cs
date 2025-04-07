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
using WPFGrowerApp.DataAccess.Models; 
using WPFGrowerApp.Properties; 

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static User CurrentUser { get; private set; } 

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
            services.AddTransient<IPriceService, PriceService>(); // Added
            services.AddTransient<IPostBatchService, PostBatchService>(); // Added
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IDepotService, DepotService>();
            services.AddTransient<IProductService, ProductService>(); 
            services.AddTransient<IProcessService, ProcessService>(); // Already added, confirming
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
            services.AddTransient<SettingsHostViewModel>(); 
            services.AddTransient<ProductViewModel>(); 
            services.AddTransient<ProcessViewModel>(); // Added Process VM
            services.AddTransient<PaymentRunViewModel>(); 
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ChangePasswordViewModel>();

            // Register Views (Views are typically not registered unless needed for DI resolution like DialogService)
            services.AddTransient<GrowerSearchView>(); // Needed by DialogService
            services.AddTransient<LoginView>();
            // SettingsHostView and ProductView don't need registration if only resolved via DataTemplates
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
                loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                bool? loginResult = loginView.ShowDialog();
                Logger.Info($"LoginView dialog closed with result: {loginResult?.ToString() ?? "null"}");

                if (loginResult == true)
                {
                    // Get the authenticated user from the LoginViewModel
                    var loginViewModel = loginView.DataContext as LoginViewModel;
                    CurrentUser = loginViewModel?.AuthenticatedUser; 

                    if (CurrentUser == null)
                    {
                        Logger.Fatal("Login reported success, but authenticated user object was null. Shutting down.");
                        MessageBox.Show("A critical error occurred after login (User object not found).", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown(-1);
                        return;
                    }

                    // Save username to settings (Correct placement)
                    try
                    {
                        Settings.Default.LastUsername = CurrentUser.Username; // Use Properties namespace implicitly or explicitly
                        Settings.Default.Save();
                        Logger.Info($"Saved last username: {CurrentUser.Username}");
                    }
                    catch (Exception settingsEx)
                    {
                        Logger.Error("Failed to save LastUsername setting.", settingsEx);
                    }

                    Logger.Info($"Login successful for user '{CurrentUser.Username}'. Showing MainWindow.");
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
                 Application.Current.Shutdown(); 
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
