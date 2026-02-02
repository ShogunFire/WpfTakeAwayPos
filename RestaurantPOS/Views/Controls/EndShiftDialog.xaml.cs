using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantPOS.Views.Controls
{
    public partial class EndShiftDialog : UserControl
    {
        public static readonly DependencyProperty KeypadValueProperty =
            DependencyProperty.Register(
                nameof(KeypadValue),
                typeof(decimal),
                typeof(EndShiftDialog),
                new PropertyMetadata(0m));

        public decimal KeypadValue
        {
            get => (decimal)GetValue(KeypadValueProperty);
            set => SetValue(KeypadValueProperty, value);
        }

        public static readonly DependencyProperty ExpectedCashProperty =
            DependencyProperty.Register(
                nameof(ExpectedCash),
                typeof(decimal),
                typeof(EndShiftDialog),
                new PropertyMetadata(0m));

        public decimal ExpectedCash
        {
            get => (decimal)GetValue(ExpectedCashProperty);
            set => SetValue(ExpectedCashProperty, value);
        }

        public static readonly DependencyProperty CountedCashProperty =
            DependencyProperty.Register(
                nameof(CountedCash),
                typeof(decimal),
                typeof(EndShiftDialog),
                new PropertyMetadata(0m));

        public decimal CountedCash
        {
            get => (decimal)GetValue(CountedCashProperty);
            set => SetValue(CountedCashProperty, value);
        }

        public static readonly DependencyProperty DifferenceProperty =
            DependencyProperty.Register(
                nameof(Difference),
                typeof(decimal),
                typeof(EndShiftDialog),
                new PropertyMetadata(0m));

        public decimal Difference
        {
            get => (decimal)GetValue(DifferenceProperty);
            set => SetValue(DifferenceProperty, value);
        }

        public static readonly DependencyProperty IsDialogOpenProperty =
            DependencyProperty.Register(
                nameof(IsDialogOpen),
                typeof(bool),
                typeof(EndShiftDialog),
                new PropertyMetadata(false));

        public bool IsDialogOpen
        {
            get => (bool)GetValue(IsDialogOpenProperty);
            set => SetValue(IsDialogOpenProperty, value);
        }

        public static readonly DependencyProperty SubmitCommandProperty =
            DependencyProperty.Register(
                nameof(SubmitCommand),
                typeof(ICommand),
                typeof(EndShiftDialog),
                new PropertyMetadata(null));

        public ICommand SubmitCommand
        {
            get => (ICommand)GetValue(SubmitCommandProperty);
            set => SetValue(SubmitCommandProperty, value);
        }

        public static readonly DependencyProperty CloseDialogCommandProperty =
            DependencyProperty.Register(
                nameof(CloseDialogCommand),
                typeof(ICommand),
                typeof(EndShiftDialog),
                new PropertyMetadata(null));

        public ICommand CloseDialogCommand
        {
            get => (ICommand)GetValue(CloseDialogCommandProperty);
            set => SetValue(CloseDialogCommandProperty, value);
        }

        public EndShiftDialog()
        {
            InitializeComponent();
        }
    }
}
