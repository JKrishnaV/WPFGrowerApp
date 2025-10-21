using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a price class for growers
    /// </summary>
    [Table("PriceClasses")]
    public class PriceClass : INotifyPropertyChanged
    {
        private int _priceClassId;
        private string _classCode = string.Empty;
        private string _className = string.Empty;
        private string? _description;
        private bool _isActive;

        [Key]
        public int PriceClassId
        {
            get => _priceClassId;
            set
            {
                if (_priceClassId != value)
                {
                    _priceClassId = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        [MaxLength(10)]
        public string ClassCode
        {
            get => _classCode;
            set
            {
                if (_classCode != value)
                {
                    _classCode = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        [MaxLength(50)]
        public string ClassName
        {
            get => _className;
            set
            {
                if (_className != value)
                {
                    _className = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(255)]
        public string? Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        // ====================================================================
        // AUDIT COLUMNS
        // ====================================================================
        
        private DateTime _createdAt;
        private string _createdBy = App.CurrentUser?.Username ?? "SYSTEM";
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (_createdBy != value)
                {
                    _createdBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set
            {
                if (_modifiedAt != value)
                {
                    _modifiedAt = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? ModifiedBy
        {
            get => _modifiedBy;
            set
            {
                if (_modifiedBy != value)
                {
                    _modifiedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set
            {
                if (_deletedAt != value)
                {
                    _deletedAt = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? DeletedBy
        {
            get => _deletedBy;
            set
            {
                if (_deletedBy != value)
                {
                    _deletedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Returns true if the record is soft-deleted
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
