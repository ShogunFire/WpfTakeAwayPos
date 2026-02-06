using RestaurantPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantPOS.Views.Controls
{
    public partial class KeypadControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(decimal),
                typeof(KeypadControl),
                new FrameworkPropertyMetadata(
                    0m,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeypadControl keypad)
            {
                // Sync the buffer when Value changes from an external binding
                keypad._buffer = keypad.Value.ToString();
                if (keypad._buffer == "0")
                    keypad._buffer = "0";
            }
        }

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty UnitLabelProperty =
            DependencyProperty.Register(
                nameof(UnitLabel),
                typeof(string),
                typeof(KeypadControl),
                new FrameworkPropertyMetadata(""));

        public string UnitLabel
        {
            get => (string)GetValue(UnitLabelProperty);
            set => SetValue(UnitLabelProperty, value);
        }

        public static readonly DependencyProperty IsPrefixProperty =
            DependencyProperty.Register(
                nameof(IsPrefix),
                typeof(bool),
                typeof(KeypadControl),
                new FrameworkPropertyMetadata(false));

        public bool IsPrefix
        {
            get => (bool)GetValue(IsPrefixProperty);
            set => SetValue(IsPrefixProperty, value);
        }

        public static readonly DependencyProperty ShowDisplayProperty =
            DependencyProperty.Register(
                nameof(ShowDisplay),
                typeof(bool),
                typeof(KeypadControl),
                new FrameworkPropertyMetadata(true));

        public bool ShowDisplay
        {
            get => (bool)GetValue(ShowDisplayProperty);
            set => SetValue(ShowDisplayProperty, value);
        }

        private string _buffer = "0";

        public KeypadControl()
        {
            InitializeComponent();
            UpdateValue();
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string digit)
            {
                NumberClick(digit);
            }
        }

        private void Decimal_Click(object sender, RoutedEventArgs e)
        {
            DecimalClick();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            Backspace();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void NumberClick(string digit)
        {
            _buffer = _buffer == "0" ? digit : _buffer + digit;
            UpdateValue();
        }

        private void DecimalClick()
        {
            if (!_buffer.Contains("."))
                _buffer += ".";
            UpdateValue();
        }

        private void Backspace()
        {
            _buffer = _buffer.Length > 1 ? _buffer[..^1] : "0";
            UpdateValue();
        }

        private void Clear()
        {
            _buffer = "0";
            UpdateValue();
        }

        private void UpdateValue()
        {
            if (decimal.TryParse(_buffer, out var v))
                Value = v;
        }
    }

}
