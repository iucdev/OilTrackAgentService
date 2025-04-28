using OilTrackAgentInterface.Controls;
using System.Windows;

namespace OilTrackAgentInterface.Services {
    public static class NotificationService {
        public static void ShowError(string message) {
            Application.Current.Dispatcher.Invoke(() => {
                var toast = new ToastNotificationWindow(message);
                toast.ShowToast();
            });
        }
    }
}
