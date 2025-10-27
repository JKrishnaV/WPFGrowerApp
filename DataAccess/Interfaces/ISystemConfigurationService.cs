using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface ISystemConfigurationService
    {
        Task<string> GetConfigValueAsync(string configKey, string defaultValue = null);
        Task<bool> GetConfigBoolAsync(string configKey, bool defaultValue = false);
        Task<int> GetConfigIntAsync(string configKey, int defaultValue = 0);
        Task<long> GetConfigLongAsync(string configKey, long defaultValue = 0);
        Task<decimal> GetConfigDecimalAsync(string configKey, decimal defaultValue = 0);
        Task SetConfigValueAsync(string configKey, string value, string description = null);
        Task SetConfigBoolAsync(string configKey, bool value, string description = null);
        Task SetConfigIntAsync(string configKey, int value, string description = null);
        Task SetConfigLongAsync(string configKey, long value, string description = null);
        Task SetConfigDecimalAsync(string configKey, decimal value, string description = null);
        Task<SystemConfiguration> GetConfigAsync(string configKey);
        Task<bool> ConfigExistsAsync(string configKey);
    }
}
