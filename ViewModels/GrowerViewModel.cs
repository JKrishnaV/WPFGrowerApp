using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerViewModel : INotifyPropertyChanged
    {
        private readonly IGrowerService _growerService;
        private Grower _grower;
        private bool _isLoading;
        private bool _isSaving;
        private string _statusMessage;

        public GrowerViewModel(IGrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            Grower = new Grower();
        }

        public async Task LoadGrowerAsync(decimal growerNumber)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading grower data...";

                var grower = await _growerService.GetGrowerByNumberAsync(growerNumber);
                if (grower != null)
                {
                    Grower = grower;
                    StatusMessage = "Grower data loaded successfully.";
                }
                else
                {
                    StatusMessage = "Grower not found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading grower: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading grower: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<bool> SaveGrowerAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = "Saving grower data...";

                bool result = await _growerService.SaveGrowerAsync(Grower);
                
                if (result)
                {
                    StatusMessage = "Grower saved successfully.";
                }
                else
                {
                    StatusMessage = "Failed to save grower.";
                }

                return result;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving grower: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error saving grower: {ex.Message}");
                return false;
            }
            finally
            {
                IsSaving = false;
            }
        }

        public Grower Grower
        {
            get => _grower;
            set
            {
                if (_grower != value)
                {
                    _grower = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
