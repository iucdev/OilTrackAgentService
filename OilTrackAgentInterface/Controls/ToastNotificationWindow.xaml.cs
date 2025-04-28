using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace OilTrackAgentInterface.Controls {
    public partial class ToastNotificationWindow : Window {
        private const int DISPLAY_MS = 3500;
        private const int FADE_MS = 300;

        public ToastNotificationWindow(string message) {
            InitializeComponent();
            PART_Message.Text = message;

            // позиционируем в правом нижнем углу экрана
            var desktop = SystemParameters.WorkArea;
            Left = desktop.Right - Width - 16;
            Top = desktop.Bottom - Height - 16;
        }

        public async void ShowToast() {
            // Плавный FadeIn
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FADE_MS));
            PART_Border.BeginAnimation(OpacityProperty, fadeIn);
            Show();

            // Ждём DISPLAY_MS
            await Task.Delay(DISPLAY_MS);

            // Плавный FadeOut
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FADE_MS));
            fadeOut.Completed += (s, e) => Close();
            PART_Border.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
