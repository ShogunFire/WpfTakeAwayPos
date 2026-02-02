using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantPOS.Views.Controls
{
    public enum DialogType
    {
        Alert,
        Confirm
    }

    public partial class DialogBox : UserControl
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DialogBox),
                new PropertyMetadata(false));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DialogBox),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(DialogBox),
                new PropertyMetadata(string.Empty));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty DialogTypeProperty =
            DependencyProperty.Register(
                nameof(DialogType),
                typeof(DialogType),
                typeof(DialogBox),
                new PropertyMetadata(DialogType.Alert));

        public DialogType DialogType
        {
            get => (DialogType)GetValue(DialogTypeProperty);
            set => SetValue(DialogTypeProperty, value);
        }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register(
                nameof(ConfirmCommand),
                typeof(ICommand),
                typeof(DialogBox),
                new PropertyMetadata(null));

        public ICommand ConfirmCommand
        {
            get => (ICommand)GetValue(ConfirmCommandProperty);
            set => SetValue(ConfirmCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(
                nameof(CancelCommand),
                typeof(ICommand),
                typeof(DialogBox),
                new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty ConfirmTextProperty =
            DependencyProperty.Register(
                nameof(ConfirmText),
                typeof(string),
                typeof(DialogBox),
                new PropertyMetadata("OK"));

        public string ConfirmText
        {
            get => (string)GetValue(ConfirmTextProperty);
            set => SetValue(ConfirmTextProperty, value);
        }

        public static readonly DependencyProperty CancelTextProperty =
            DependencyProperty.Register(
                nameof(CancelText),
                typeof(string),
                typeof(DialogBox),
                new PropertyMetadata("Cancel"));

        public string CancelText
        {
            get => (string)GetValue(CancelTextProperty);
            set => SetValue(CancelTextProperty, value);
        }

        public DialogBox()
        {
            InitializeComponent();
        }
    }
}
