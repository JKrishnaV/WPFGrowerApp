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

            // Register services
            services.AddSingleton<IGrowerService, GrowerService>();
            services.AddSingleton<IPayGroupService, PayGroupService>();

            // Register ViewModels
            services.AddTransient<GrowerViewModel>();
            services.AddTransient<GrowerSearchViewModel>();
            services.AddSingleton<MainViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }
    }
} 