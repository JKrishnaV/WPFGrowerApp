using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Repositories;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Views;

namespace WPFGrowerApp
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer("Server=DESKTOP-LQ92Q06;Database=PackagingPaymentSystem;User Id=localDB;Password=528database@JK;"));

            // Register repositories
            services.AddScoped<IGrowerRepository, GrowerRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IChequeRepository, ChequeRepository>();

            // Register services
            services.AddScoped<IGrowerService, GrowerService>();

            // Register view models
            services.AddTransient<GrowerViewModel>();
            services.AddTransient<GrowerSearchViewModel>();

            // Register main window
            services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
