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
    /// Interaction logic for TankDisplayControl.xaml
    /// </summary>
    public partial class TankDisplayControl : UserControl {
        public TankDisplayControl() {
            InitializeComponent();
        }

        // DependencyProperty: процент заполнения (0–100)
        public static readonly DependencyProperty LevelPercentProperty =
            DependencyProperty.Register(
                nameof(LevelPercent),
                typeof(double),
                typeof(TankDisplayControl),
                new PropertyMetadata(0.0));

        public double LevelPercent {
            get => (double)GetValue(LevelPercentProperty);
            set => SetValue(LevelPercentProperty, value);
        }
    }
}
