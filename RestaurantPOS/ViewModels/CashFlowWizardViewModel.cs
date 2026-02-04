using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantPOS.Models;
using RestaurantPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.ViewModels
{
    public partial class CashFlowWizardViewModel : ObservableObject, IStepFlow
    {
        private readonly ICashControlService _cashControlService;
        private readonly IDialogService _dialogService;
        private readonly CashFlowType _flowType;
        private readonly Action _onCompleted;

        [ObservableProperty]
        private decimal amount;

        [ObservableProperty]
        private string selectedCategory;

        [ObservableProperty]
        private CashRemovalReason selectedReason;

        [ObservableProperty]
        private ObservableCollection<string> categories = new();

        [ObservableProperty]
        private ObservableCollection<CashRemovalReason> availableReasons = new();

        public int StepCount => 3;

        private readonly Dictionary<string, List<(string reason, string description)>> _removeReasonsByCategory = new()
        {
            { "Banking", new List<(string, string)> 
            { 
                ("Bank Deposit", "Money deposited to bank account"),
                ("Safe Drop", "Cash placed in secure safe"),
                ("Change Fund", "Initial change fund setup"),
                ("Float Adjustment", "Adjustment to register float"),
                ("Till Transfer", "Cash moved between registers"),
                ("Cash Pickup", "Armored car cash pickup"),
                ("Cash Collection", "Collection of excess cash"),
                ("Armored Pickup", "Armored car collection service")
            } },
            { "Operations", new List<(string, string)>
            {
                ("Petty Cash", "Small operational expenses"),
                ("Supplies", "Purchase of supplies"),
                ("Utilities", "Utility bill payments"),
                ("Repairs", "Equipment or building repairs"),
                ("Maintenance", "Regular maintenance costs"),
                ("Delivery Fees", "Delivery and shipping costs"),
                ("Cleaning", "Cleaning service or supplies"),
                ("Misc Expense", "Miscellaneous business expenses")
            } },
            { "Staff", new List<(string, string)>
            {
                ("Tips Payout", "Staff tip distribution"),
                ("Wages Advance", "Advance on staff wages"),
                ("Staff Meals", "Complimentary staff meals"),
                ("Training", "Staff training costs"),
                ("Uniforms", "Staff uniform purchases"),
                ("Reimbursement", "Employee reimbursement"),
                ("Travel", "Staff travel expenses"),
                ("Overtime", "Overtime payment")
            } },
            { "Vendors", new List<(string, string)>
            {
                ("Vendor Payment", "Payment to vendor"),
                ("Emergency Purchase", "Emergency supply purchase"),
                ("Market Run", "Quick market supply run"),
                ("Packaging", "Packaging material purchase"),
                ("Disposables", "Disposable items purchase"),
                ("Equipment Rental", "Equipment rental fees"),
                ("Service Call", "Service provider call"),
                ("Rush Order", "Expedited order fees")
            } },
            { "Promotions", new List<(string, string)>
            {
                ("Promo Cost", "Promotional campaign cost"),
                ("Sampling", "Food sampling for promotion"),
                ("Marketing", "Marketing and advertising"),
                ("Coupons", "Coupon redemption cost"),
                ("Loyalty Reward", "Customer loyalty rewards"),
                ("Event Sponsorship", "Event sponsorship cost"),
                ("Gift Card Cashout", "Gift card redemption"),
                ("Discount Adjustment", "Manual discount adjustment")
            } },
            { "Other", new List<(string, string)>
            {
                ("Other", "Other miscellaneous reason"),
                ("Uncategorized", "Uncategorized cash removal"),
                ("Accounting Adjustment", "Accounting reconciliation"),
                ("Refund Correction", "Refund correction entry"),
                ("Cash Over/Short", "Cash variance adjustment"),
                ("Bank Fees", "Banking fees paid"),
                ("Taxes", "Tax payments"),
                ("Insurance", "Insurance payment")
            } }
        };

        private readonly Dictionary<string, List<(string reason, string description)>> _addReasonsByCategory = new()
        {
            { "Cash Received", new List<(string, string)>
            {
                ("Deposit Return", "Returned deposit or refund"),
                ("Owner Contribution", "Owner or investor cash injection"),
                ("Loan Proceeds", "Business loan received"),
                ("Customer Refund", "Customer refund for returned items")
            } },
            { "Banking", new List<(string, string)>
            {
                ("Bank Withdrawal", "Withdrawal from bank account"),
                ("Float Addition", "Additional float from bank"),
                ("Change Fund", "Change fund replenishment")
            } },
            { "Sales", new List<(string, string)>
            {
                ("Offline Sales", "Sales not recorded in system"),
                ("Prior Day Sales", "Sales from previous day collection"),
                ("Cash Reconciliation", "Reconciliation of cash discrepancies")
            } },
            { "Other", new List<(string, string)>
            {
                ("Other", "Other miscellaneous reason"),
                ("Uncategorized", "Uncategorized cash addition")
            } }
        };

        public CashFlowWizardViewModel(ICashControlService cashControlService, IDialogService dialogService, CashFlowType flowType, Action onCompleted)
        {
            _cashControlService = cashControlService;
            _dialogService = dialogService;
            _flowType = flowType;
            _onCompleted = onCompleted;
        }

        public bool CanMoveNext(int step)
        {
            return step switch
            {
                0 => Amount > 0,
                1 => !string.IsNullOrWhiteSpace(SelectedCategory),
                2 => SelectedReason != null,
                _ => false
            };
        }

        public void OnStepEntered(int step)
        {
            if (step == 1)
            {
                LoadCategories();
            }

            if (step == 2)
            {
                LoadReasons();
            }
        }

        public void OnCompleted()
        {
            if (Amount <= 0 || SelectedReason == null)
                return;

            if (_flowType == CashFlowType.Add)
            {
                _cashControlService.AddCash(Amount, SelectedReason.Reason);
                _dialogService.Alert($"Added ${Amount:F2} - {SelectedReason.Reason}", "Cash Added");
            }
            else
            {
                _cashControlService.RemoveCash(Amount, SelectedReason.Reason);
                _dialogService.Alert($"Removed ${Amount:F2} - {SelectedReason.Reason}", "Cash Removed");
            }

            _onCompleted?.Invoke();
        }

        public string GetStepTitle(int step)
        {
            return step switch
            {
                0 => _flowType == CashFlowType.Add ? "Add Cash" : "Remove Cash",
                1 => "Reason",
                2 => "Details",
                _ => string.Empty
            };
        }

        public string GetStepDescription(int step)
        {
            return step switch
            {
                0 => "Enter the amount",
                1 => "Select the main reason",
                2 => "Select the detailed reason",
                _ => string.Empty
            };
        }

        private void LoadCategories()
        {
            var categories = _flowType == CashFlowType.Add
                ? _addReasonsByCategory.Keys
                : _removeReasonsByCategory.Keys;

            Categories = new ObservableCollection<string>(categories);
        }

        private void LoadReasons()
        {
            var reasonsDictionary = _flowType == CashFlowType.Add
                ? _addReasonsByCategory
                : _removeReasonsByCategory;

            AvailableReasons.Clear();

            if (string.IsNullOrWhiteSpace(SelectedCategory))
                return;

            if (reasonsDictionary.TryGetValue(SelectedCategory, out var reasons))
            {
                foreach (var (reason, description) in reasons)
                {
                    AvailableReasons.Add(new CashRemovalReason(reason, description));
                }
            }
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                LoadReasons();
            }
        }
    }
}
