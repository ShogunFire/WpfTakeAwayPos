using RestaurantPOS.Services.Interfaces;
using RestaurantPOS.Views.Controls;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantPOS.Services
{
    public class DialogService : IDialogService
    {
        private DialogBox _dialogBox;

        public void Initialize(DialogBox dialogBox)
        {
            _dialogBox = dialogBox;
        }

        public Task<bool> Confirm(string message, string title = "Confirm")
        {
            return ShowDialogAsync(message, title, DialogType.Confirm, "Yes", "No");
        }

        public Task Alert(string message, string title = "Alert")
        {
            return ShowDialogAsync(message, title, DialogType.Alert, "OK", "");
        }

        private Task<bool> ShowDialogAsync(string message, string title, DialogType dialogType, string confirmText, string cancelText)
        {
            if (_dialogBox == null)
            {
                // Fallback to MessageBox if DialogBox not initialized
                if (dialogType == DialogType.Confirm)
                {
                    var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    return Task.FromResult(result == MessageBoxResult.Yes);
                }
                else
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                    return Task.FromResult(true);
                }
            }

            var tcs = new TaskCompletionSource<bool>();

            // Configure the dialog
            _dialogBox.Title = title;
            _dialogBox.Message = message;
            _dialogBox.DialogType = dialogType;
            _dialogBox.ConfirmText = confirmText;
            _dialogBox.CancelText = cancelText;

            // Set up command handlers
            RelayCommand confirmCommand = new RelayCommand(() =>
            {
                _dialogBox.IsOpen = false;
                tcs.SetResult(true);
            });

            RelayCommand cancelCommand = new RelayCommand(() =>
            {
                _dialogBox.IsOpen = false;
                tcs.SetResult(false);
            });

            _dialogBox.ConfirmCommand = confirmCommand;
            _dialogBox.CancelCommand = cancelCommand;

            // Show the dialog
            _dialogBox.IsOpen = true;

            return tcs.Task;
        }
    }

    // Simple RelayCommand implementation for dialog buttons
    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly System.Action _execute;

        public RelayCommand(System.Action execute)
        {
            _execute = execute;
        }

        public event System.EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
