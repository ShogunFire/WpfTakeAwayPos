using System.ComponentModel;

namespace RestaurantPOS.ViewModels
{
    public interface IStepFlow : INotifyPropertyChanged
    {
        int StepCount { get; }
        bool CanMoveNext(int step);
        void OnStepEntered(int step);
        void OnCompleted();
        string GetStepTitle(int step);
        string GetStepDescription(int step);
    }
}
