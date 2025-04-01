using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WPFGrowerApp.DataAccess; // Added for BaseDatabaseService
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Services; 
using WPFGrowerApp.Views; // Added for GrowerSearchView registration

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DataAccess Services (Interface -> Implementation)
            // Removed incorrect registration for abstract BaseDatabaseService:
            // services.AddSingleton<IDatabaseService, BaseDatabaseService>(); 
            // Concrete services inheriting from BaseDatabaseService are registered below.
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
            // Assuming ValidationService is used by others, register it if needed
            services.AddTransient<ValidationService>(); 

            // Register Other Services
            services.AddTransient<ReportExportService>();
            services.AddSingleton<IDialogService, DialogService>(); // Register DialogService

            // Register ViewModels
            // Use Transient for ViewModels that have state specific to their view instance
            // Use Singleton if a ViewModel should persist across navigation (less common for page VMs)
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<GrowerViewModel>();
            services.AddTransient<GrowerSearchViewModel>(); 
            services.AddTransient<ImportViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<InventoryViewModel>(); // Added
            services.AddTransient<SettingsViewModel>(); // Added
            // Add other ViewModels (e.g., for specific reports)

            // Register Views that need DI resolution (like dialogs)
            services.AddTransient<GrowerSearchView>(); // Added registration for the view

            // Register MainWindow
            services.AddSingleton<MainWindow>();
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            // Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cXmVCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXxceXVcRWReUUZ1V0dWYUo=");
            
            base.OnStartup(e);

            // Resolve and show the main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
