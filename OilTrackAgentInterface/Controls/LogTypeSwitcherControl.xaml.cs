using System.Windows;
using System.Windows.Controls;

namespace OilTrackAgentInterface.Controls {
    public partial class LogTypeSwitcherControl : UserControl {
        public LogTypeSwitcherControl() {
            InitializeComponent();
        }

        // Булево свойство переключателя
        public static readonly DependencyProperty IsReceiveSelectedProperty =
            DependencyProperty.Register(
                nameof(IsReceiveSelected),
                typeof(bool),
                typeof(LogTypeSwitcherControl),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public bool IsReceiveSelected {
            get => (bool)GetValue(IsReceiveSelectedProperty);
            set => SetValue(IsReceiveSelectedProperty, value);
        }

        // Текст для состояния true (On)
        public static readonly DependencyProperty OnTextProperty =
            DependencyProperty.Register(
                nameof(OnText),
                typeof(string),
                typeof(LogTypeSwitcherControl),
                new PropertyMetadata("On")
            );

        public string OnText {
            get => (string)GetValue(OnTextProperty);
            set => SetValue(OnTextProperty, value);
        }

        // Текст для состояния false (Off)
        public static readonly DependencyProperty OffTextProperty =
            DependencyProperty.Register(
                nameof(OffText),
                typeof(string),
                typeof(LogTypeSwitcherControl),
                new PropertyMetadata("Off")
            );

        public string OffText {
            get => (string)GetValue(OffTextProperty);
            set => SetValue(OffTextProperty, value);
        }
    }
}
