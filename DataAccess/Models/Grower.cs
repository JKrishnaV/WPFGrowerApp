using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a grower in the modern database schema.
    /// Maps directly to the Growers table with all fields from the latest schema.
    /// </summary>
    public class Grower : INotifyPropertyChanged
    {
        // ======================================================================
        // PRIMARY IDENTIFICATION
        // ======================================================================
        
        private int _growerId;
        private string _growerNumber = string.Empty;
        private string _fullName = string.Empty;
        private string _checkPayeeName = string.Empty;

        public int GrowerId
        {
            get => _growerId;
            set
            {
                if (_growerId != value)
                {
                    _growerId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set
            {
                if (_growerNumber != value)
                {
                    _growerNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CheckPayeeName
        {
            get => _checkPayeeName;
            set
            {
                if (_checkPayeeName != value)
                {
                    _checkPayeeName = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // CONTACT INFORMATION
        // ======================================================================
        
        private string _address = string.Empty;
        private string _city = string.Empty;
        private string _province = string.Empty;
        private string _postalCode = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _mobileNumber = string.Empty;
        private string _email = string.Empty;

        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Province
        {
            get => _province;
            set
            {
                if (_province != value)
                {
                    _province = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PostalCode
        {
            get => _postalCode;
            set
            {
                if (_postalCode != value)
                {
                    _postalCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MobileNumber
        {
            get => _mobileNumber;
            set
            {
                if (_mobileNumber != value)
                {
                    _mobileNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // BUSINESS INFORMATION
        // ======================================================================
        
        private string _gstNumber = string.Empty;
        private string _businessNumber = string.Empty;
        private string _currencyCode = "CAD";

        public string GSTNumber
        {
            get => _gstNumber;
            set
            {
                if (_gstNumber != value)
                {
                    _gstNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BusinessNumber
        {
            get => _businessNumber;
            set
            {
                if (_businessNumber != value)
                {
                    _businessNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (_currencyCode != value)
                {
                    _currencyCode = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PAYMENT & PRICING CONFIGURATION
        // ======================================================================
        
        private int _paymentGroupId;
        private int _defaultDepotId;
        private int _defaultPriceClassId;
        private int? _paymentMethodId;

        public int PaymentGroupId
        {
            get => _paymentGroupId;
            set
            {
                if (_paymentGroupId != value)
                {
                    _paymentGroupId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DefaultDepotId
        {
            get => _defaultDepotId;
            set
            {
                if (_defaultDepotId != value)
                {
                    _defaultDepotId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DefaultPriceClassId
        {
            get => _defaultPriceClassId;
            set
            {
                if (_defaultPriceClassId != value)
                {
                    _defaultPriceClassId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? PaymentMethodId
        {
            get => _paymentMethodId;
            set
            {
                if (_paymentMethodId != value)
                {
                    _paymentMethodId = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // STATUS & FLAGS
        // ======================================================================
        
        private bool _isActive = true;
        private bool _isOnHold;
        private bool _chargeGST = true;

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

        public bool IsOnHold
        {
            get => _isOnHold;
            set
            {
                if (_isOnHold != value)
                {
                    _isOnHold = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ChargeGST
        {
            get => _chargeGST;
            set
            {
                if (_chargeGST != value)
                {
                    _chargeGST = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // ADMINISTRATIVE
        // ======================================================================
        
        private string _notes = string.Empty;
        private DateTime _createdAt = DateTime.Now;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string _modifiedBy = string.Empty;
        private DateTime? _deletedAt;
        private string _deletedBy = string.Empty;

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string ModifiedBy
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

        public string DeletedBy
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

        // ======================================================================
        // NAVIGATION PROPERTIES (for FK lookups)
        // ======================================================================
        
        public PaymentGroup? PaymentGroup { get; set; }
        public Depot? DefaultDepot { get; set; }
        public PriceClass? DefaultPriceClass { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        // ======================================================================
        // LEGACY COMPATIBILITY (for existing code that might reference these)
        // ======================================================================
        
        /// <summary>
        /// Legacy property for backward compatibility. Maps to FullName.
        /// </summary>
        public string GrowerName
        {
            get => FullName;
            set => FullName = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to CheckPayeeName.
        /// </summary>
        public string ChequeName
        {
            get => CheckPayeeName;
            set => CheckPayeeName = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to Province.
        /// </summary>
        public string Prov
        {
            get => Province;
            set => Province = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to PostalCode.
        /// </summary>
        public string Postal
        {
            get => PostalCode;
            set => PostalCode = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to PhoneNumber.
        /// </summary>
        public string Phone
        {
            get => PhoneNumber;
            set => PhoneNumber = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to MobileNumber.
        /// </summary>
        public string PhoneAdditional1
        {
            get => MobileNumber;
            set => MobileNumber = value;
        }

        /// <summary>
        /// Legacy property for backward compatibility. Maps to DefaultPriceClassId.
        /// </summary>
        public int PriceLevel
        {
            get => DefaultPriceClassId;
            set => DefaultPriceClassId = value;
        }

        // ======================================================================
        // INOTIFYPROPERTYCHANGED IMPLEMENTATION
        // ======================================================================
        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ======================================================================
        // UTILITY METHODS
        // ======================================================================
        
        /// <summary>
        /// Creates a deep copy of the grower for editing.
        /// </summary>
        public Grower Clone()
        {
            return new Grower
            {
                GrowerId = GrowerId,
                GrowerNumber = GrowerNumber,
                FullName = FullName,
                CheckPayeeName = CheckPayeeName,
                Address = Address,
                City = City,
                Province = Province,
                PostalCode = PostalCode,
                PhoneNumber = PhoneNumber,
                MobileNumber = MobileNumber,
                Email = Email,
                GSTNumber = GSTNumber,
                BusinessNumber = BusinessNumber,
                CurrencyCode = CurrencyCode,
                PaymentGroupId = PaymentGroupId,
                DefaultDepotId = DefaultDepotId,
                DefaultPriceClassId = DefaultPriceClassId,
                PaymentMethodId = PaymentMethodId,
                IsActive = IsActive,
                IsOnHold = IsOnHold,
                ChargeGST = ChargeGST,
                Notes = Notes,
                CreatedAt = CreatedAt,
                CreatedBy = CreatedBy,
                ModifiedAt = ModifiedAt,
                ModifiedBy = ModifiedBy,
                DeletedAt = DeletedAt,
                DeletedBy = DeletedBy
            };
        }

        /// <summary>
        /// Determines if the grower has any changes compared to another grower.
        /// </summary>
        public bool HasChanges(Grower other)
        {
            if (other == null) return true;

            return GrowerNumber != other.GrowerNumber ||
                   FullName != other.FullName ||
                   CheckPayeeName != other.CheckPayeeName ||
                   Address != other.Address ||
                   City != other.City ||
                   Province != other.Province ||
                   PostalCode != other.PostalCode ||
                   PhoneNumber != other.PhoneNumber ||
                   MobileNumber != other.MobileNumber ||
                   Email != other.Email ||
                   GSTNumber != other.GSTNumber ||
                   BusinessNumber != other.BusinessNumber ||
                   CurrencyCode != other.CurrencyCode ||
                   PaymentGroupId != other.PaymentGroupId ||
                   DefaultDepotId != other.DefaultDepotId ||
                   DefaultPriceClassId != other.DefaultPriceClassId ||
                   PaymentMethodId != other.PaymentMethodId ||
                   IsActive != other.IsActive ||
                   IsOnHold != other.IsOnHold ||
                   ChargeGST != other.ChargeGST ||
                   Notes != other.Notes;
        }
    }
}