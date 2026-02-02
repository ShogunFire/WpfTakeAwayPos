using RestaurantPOS.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantPOS.Views.Controls
{
    public partial class KeypadInputBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(KeypadInputBox),
                new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty DisplayValueProperty =
            DependencyProperty.Register(
                nameof(DisplayValue),
                typeof(string),
                typeof(KeypadInputBox),
                new PropertyMetadata("0"));

        public string DisplayValue
        {
            get => (string)GetValue(DisplayValueProperty);
            set => SetValue(DisplayValueProperty, value);
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target),
                typeof(KeypadTarget),
                typeof(KeypadInputBox),
                new PropertyMetadata(KeypadTarget.None));

        public KeypadTarget Target
        {
            get => (KeypadTarget)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public static readonly DependencyProperty ActiveTargetProperty =
            DependencyProperty.Register(
                nameof(ActiveTarget),
                typeof(KeypadTarget),
                typeof(KeypadInputBox),
                new FrameworkPropertyMetadata(
                    KeypadTarget.None,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnActiveTargetChanged));

        public KeypadTarget ActiveTarget
        {
            get => (KeypadTarget)GetValue(ActiveTargetProperty);
            set => SetValue(ActiveTargetProperty, value);
        }

        private static void OnActiveTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KeypadInputBox inputBox)
            {
                inputBox.UpdateIsActive();
            }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(KeypadInputBox),
                new PropertyMetadata(false));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            private set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty ActivateCommandProperty =
            DependencyProperty.Register(
                nameof(ActivateCommand),
                typeof(ICommand),
                typeof(KeypadInputBox),
                new PropertyMetadata(null));

        public ICommand ActivateCommand
        {
            get => (ICommand)GetValue(ActivateCommandProperty);
            set => SetValue(ActivateCommandProperty, value);
        }

        public KeypadInputBox()
        {
            InitializeComponent();
        }

        private void UpdateIsActive()
        {
            IsActive = ActiveTarget == Target;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == TargetProperty)
            {
                UpdateIsActive();
            }
        }
    }
}
