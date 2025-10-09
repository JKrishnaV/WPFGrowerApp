using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Services
{
    public static class ServiceConfiguration
    {
        private static ServiceProvider _serviceProvider;

        public static void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register Data Access Services
            services.AddSingleton<IGrowerService, GrowerService>();
            services.AddSingleton<IPayGroupService, PayGroupService>();
            services.AddSingleton<IFileImportService, FileImportService>();
            services.AddSingleton<IImportBatchProcessor, ImportBatchProcessor>();
            services.AddSingleton<ValidationService>();
            services.AddSingleton<IImportBatchService, ImportBatchService>();
            services.AddSingleton<IReceiptService, ReceiptService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<IPriceService, PriceService>();
            services.AddSingleton<IDepotService, DepotService>();
            services.AddSingleton<IAccountService, AccountService>();
            services.AddSingleton<IChequeService, ChequeService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IAuditLogService, AuditLogService>();
            
            // Register Payment System Services (Phase 1)
            services.AddSingleton<IPaymentTypeService, PaymentTypeService>();
            services.AddSingleton<IPaymentBatchManagementService, PaymentBatchManagementService>();
            services.AddSingleton<IChequeGenerationService, ChequeGenerationService>();
            services.AddSingleton<IPaymentService, PaymentService>();
            
            // Register Application Services (Printing)
            services.AddSingleton<IChequePrintingService, ChequePrintingService>();
            services.AddSingleton<IStatementPrintingService, StatementPrintingService>();


            // Register ViewModels
            services.AddTransient<GrowerViewModel>();
            services.AddTransient<GrowerSearchViewModel>();
            services.AddTransient<ImportViewModel>();
            services.AddSingleton<MainViewModel>();
            
            // Register Payment ViewModels (Phase 1)
            services.AddTransient<PaymentRunViewModel>();
            services.AddTransient<PaymentBatchViewModel>();
            services.AddTransient<ChequeManagementViewModel>();
            services.AddTransient<FinalPaymentViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }
    }
}
