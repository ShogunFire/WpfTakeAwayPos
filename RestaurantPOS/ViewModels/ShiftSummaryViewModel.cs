using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class ShiftSummaryViewModel : BaseViewModel
    {
        private readonly IShiftService _shiftService;
        private readonly ICashControlService _cashControlService;
        private readonly INavigationService _navigationService;
        private Shift _completedShift;

        [ObservableProperty]
        private DateTime startTime;

        [ObservableProperty]
        private DateTime endTime;

        [ObservableProperty]
        private decimal openingCash;

        [ObservableProperty]
        private decimal expectedCash;

        [ObservableProperty]
        private decimal countedCash;

        [ObservableProperty]
        private decimal difference;

        [ObservableProperty]
        private decimal totalOrders;

        [ObservableProperty]
        private int orderCount;

        [ObservableProperty]
        private string differenceStatus; // "Balanced", "Over", "Under"

        [ObservableProperty]
        private string differenceColor; // For UI styling

        [ObservableProperty]
        private ObservableCollection<CashTransactionSummary> transactions;

        public ShiftSummaryViewModel(
            IShiftService shiftService,
            ICashControlService cashControlService,
            INavigationService navigationService)
        {
            _shiftService = shiftService;
            _cashControlService = cashControlService;
            _navigationService = navigationService;
            Transactions = new ObservableCollection<CashTransactionSummary>();
            
            // Load the most recent completed shift automatically
            LoadCompletedShift();
        }

        private void LoadCompletedShift()
        {
            try
            {
                // Get the most recently completed shift from the service
                // Since EndShift was just called, we can get the last shift from the database
                var shift = _shiftService.GetActiveShift();
                
                if (shift != null && shift.EndDateTime.HasValue)
                {
                    LoadShiftSummary(shift);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading shift summary: {ex.Message}");
            }
        }

        public void LoadShiftSummary(Shift completedShift)
        {
            _completedShift = completedShift;

            StartTime = completedShift.StartDateTime;
            EndTime = completedShift.EndDateTime ?? DateTime.Now;
            OpeningCash = completedShift.OpeningCash;
            ExpectedCash = completedShift.ExpectedCash ?? 0;
            CountedCash = completedShift.DeclaredCash ?? 0;
            Difference = completedShift.Difference ?? 0;

            // Calculate difference status and color
            if (Difference == 0)
            {
                DifferenceStatus = "Balanced ✓";
                DifferenceColor = "SuccessColor"; // Using theme resource
            }
            else if (Difference > 0)
            {
                DifferenceStatus = $"Over +{Math.Abs(Difference):C}";
                DifferenceColor = "InfoColor"; // Using theme resource
            }
            else
            {
                DifferenceStatus = $"Under {Difference:C}";
                DifferenceColor = "ErrorColor"; // Using theme resource
            }

            // Load transaction summary
            LoadTransactionSummary();
        }

        private void LoadTransactionSummary()
        {
            Transactions.Clear();
            var shiftTransactions = _cashControlService.GetTransactionsByShift(_completedShift.ShiftId);
            var orderCount = 0;
            var orderTotal = 0m;

            foreach (var transaction in shiftTransactions)
            {
                // Skip sales transactions - they don't appear in the summary
                if (transaction.Type == CashTransactionType.Sale)
                {
                    orderTotal += transaction.Amount;
                    orderCount++;
                    continue;
                }

                var summary = new CashTransactionSummary
                {
                    Type = transaction.Type.ToString(),
                    Amount = transaction.Amount,
                    TimeRecorded = transaction.Timestamp,
                    Category = transaction.Reason ?? "General"
                };

                Transactions.Add(summary);
            }

            TotalOrders = orderTotal;
            OrderCount = orderCount;
        }

        [RelayCommand]
        public void CloseShift()
        {
            // Navigate to login page
            _navigationService.Navigate<LoginViewModel>();
        }
    }

    public class CashTransactionSummary
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime TimeRecorded { get; set; }
        public string Category { get; set; }
    }
}
