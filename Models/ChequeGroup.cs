using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model for grouping cheques by type in the unified cheque preparation view
    /// </summary>
    public class ChequeGroup : INotifyPropertyChanged
    {
        private ChequePaymentType _paymentType;
        private string _groupName;
        private List<ChequeItem> _cheques;
        private decimal _totalAmount;
        private int _chequeCount;
        private bool _isExpanded;

        public ChequePaymentType PaymentType
        {
            get => _paymentType;
            set => SetProperty(ref _paymentType, value);
        }

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        public List<ChequeItem> Cheques
        {
            get => _cheques;
            set => SetProperty(ref _cheques, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public int ChequeCount
        {
            get => _chequeCount;
            set => SetProperty(ref _chequeCount, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        // Computed properties
        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string ChequeCountDisplay => $"{ChequeCount} cheque{(ChequeCount != 1 ? "s" : "")}";
        public string Icon => GetIconForPaymentType(PaymentType);
        public string StatusSummary => GetStatusSummary();

        public ChequeGroup()
        {
            Cheques = new List<ChequeItem>();
            IsExpanded = true;
        }

        public ChequeGroup(ChequePaymentType paymentType, string groupName) : this()
        {
            PaymentType = paymentType;
            GroupName = groupName;
        }

        public void AddCheque(ChequeItem cheque)
        {
            if (cheque == null) return;

            Cheques.Add(cheque);
            UpdateTotals();
        }

        public void RemoveCheque(ChequeItem cheque)
        {
            if (cheque == null) return;

            Cheques.Remove(cheque);
            UpdateTotals();
        }

        public void UpdateTotals()
        {
            TotalAmount = Cheques?.Sum(c => c.Amount) ?? 0;
            ChequeCount = Cheques?.Count ?? 0;
        }

        private string GetIconForPaymentType(ChequePaymentType paymentType)
        {
            return paymentType switch
            {
                ChequePaymentType.Regular => "📄",
                ChequePaymentType.Advance => "💰",
                ChequePaymentType.Consolidated => "🔗",
                _ => "📄"
            };
        }

        private string GetStatusSummary()
        {
            if (Cheques == null || !Cheques.Any())
                return "No cheques";

            var statusCounts = Cheques.GroupBy(c => c.Status)
                .Select(g => $"{g.Count()} {g.Key}")
                .ToList();

            return string.Join(", ", statusCounts);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
