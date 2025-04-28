using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OilTrackAgentInterface.Controls {
    /// <summary>
    /// Interaction logic for TankStatusCardControl.xaml
    /// </summary>
    public partial class TankStatusCardControl : UserControl {
        public TankStatusCardControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LevelPercentProperty =
            DependencyProperty.Register(
                nameof(LevelPercent), typeof(int), typeof(TankStatusCardControl), new PropertyMetadata(0));
        public int LevelPercent {
            get => (int)GetValue(LevelPercentProperty);
            set => SetValue(LevelPercentProperty, value);
        }

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(
                nameof(StatusText), typeof(string), typeof(TankStatusCardControl), new PropertyMetadata(string.Empty));
        public string StatusText {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public static readonly DependencyProperty InternalTankIdProperty =
            DependencyProperty.Register(
                nameof(InternalTankId), typeof(string), typeof(TankStatusCardControl), new PropertyMetadata(string.Empty));
        public string InternalTankId {
            get => (string)GetValue(InternalTankIdProperty);
            set => SetValue(InternalTankIdProperty, value);
        }

        public static readonly DependencyProperty OilProductTypeTextProperty =
            DependencyProperty.Register(
                nameof(OilProductTypeText), typeof(string), typeof(TankStatusCardControl), new PropertyMetadata(string.Empty));
        public string OilProductTypeText {
            get => (string)GetValue(OilProductTypeTextProperty);
            set => SetValue(OilProductTypeTextProperty, value);
        }

        public static readonly DependencyProperty CurrentVolumeProperty =
            DependencyProperty.Register(
                nameof(CurrentVolume), typeof(double), typeof(TankStatusCardControl), new PropertyMetadata(0.0));
        public double CurrentVolume {
            get => (double)GetValue(CurrentVolumeProperty);
            set => SetValue(CurrentVolumeProperty, value);
        }

        public static readonly DependencyProperty MaxVolumeProperty =
            DependencyProperty.Register(
                nameof(MaxVolume), typeof(double), typeof(TankStatusCardControl), new PropertyMetadata(0.0));
        public double MaxVolume {
            get => (double)GetValue(MaxVolumeProperty);
            set => SetValue(MaxVolumeProperty, value);
        }

        public static readonly DependencyProperty UnitTextProperty =
            DependencyProperty.Register(
                nameof(UnitText), typeof(string), typeof(TankStatusCardControl), new PropertyMetadata(string.Empty));
        public string UnitText {
            get => (string)GetValue(UnitTextProperty);
            set => SetValue(UnitTextProperty, value);
        }
    }
}