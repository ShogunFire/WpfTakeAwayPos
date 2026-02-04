using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Services.Interfaces;
using System.ComponentModel;

namespace RestaurantPOS.ViewModels
{
    public partial class StepPopupViewModel : BaseViewModel
    {
        private readonly IPopupService _popupService;
        private IStepFlow _flow;

        [ObservableProperty]
        private int currentStepIndex;

        [ObservableProperty]
        private int totalSteps;

        public IStepFlow Flow
        {
            get => _flow;
            private set => SetProperty(ref _flow, value);
        }

        public int CurrentStepNumber => CurrentStepIndex + 1;

        public double Progress => TotalSteps == 0
            ? 0
            : (double)(CurrentStepIndex + 1) / TotalSteps;

        public bool CanGoBack => CurrentStepIndex > 0;

        public bool CanGoNext => Flow != null && Flow.CanMoveNext(CurrentStepIndex);

        public string BackButtonText => CurrentStepIndex == 0 ? "Cancel" : "Back";

        public string NextButtonText => CurrentStepIndex == TotalSteps - 1 ? "Confirm" : "Next";

        public string StepTitle => Flow?.GetStepTitle(CurrentStepIndex) ?? string.Empty;

        public string StepDescription => Flow?.GetStepDescription(CurrentStepIndex) ?? string.Empty;

        public StepPopupViewModel(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public void Initialize(IStepFlow flow)
        {
            if (Flow is INotifyPropertyChanged oldFlow)
            {
                oldFlow.PropertyChanged -= Flow_PropertyChanged;
            }

            Flow = flow;
            TotalSteps = flow.StepCount;
            CurrentStepIndex = 0;

            if (Flow is INotifyPropertyChanged newFlow)
            {
                newFlow.PropertyChanged += Flow_PropertyChanged;
            }

            flow.OnStepEntered(0);
            RaiseComputedProperties();
        }

        private void Flow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CanGoNext));
        }

        partial void OnCurrentStepIndexChanged(int value)
        {
            Flow?.OnStepEntered(value);
            RaiseComputedProperties();
        }

        private void RaiseComputedProperties()
        {
            OnPropertyChanged(nameof(CurrentStepNumber));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(BackButtonText));
            OnPropertyChanged(nameof(NextButtonText));
            OnPropertyChanged(nameof(StepTitle));
            OnPropertyChanged(nameof(StepDescription));
        }

        [RelayCommand]
        private void Next()
        {
            if (!CanGoNext)
                return;

            if (CurrentStepIndex == TotalSteps - 1)
            {
                Flow?.OnCompleted();
                _popupService.Close();
                return;
            }

            CurrentStepIndex++;
        }

        [RelayCommand]
        private void BackOrCancel()
        {
            if (CurrentStepIndex == 0)
            {
                _popupService.Close();
                return;
            }

            CurrentStepIndex--;
        }
    }
}
