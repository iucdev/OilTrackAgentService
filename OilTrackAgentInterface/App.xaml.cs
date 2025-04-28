using OilTrackAgentInterface.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OilTrackAgentInterface {
    public partial class App : Application {
        public App() {
            // UI‑исключения
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // Фатальные для домена
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            // Неотслеженные исключения тасков
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            ShowError(e.Exception, "Произошла ошибка в интерфейсе");
            e.Handled = true;  // предотвратит закрытие приложения
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;
            ShowError(ex, "Непредвиденная ошибка приложения");
            // если нужно — Environment.Exit, т.к. после этого приложение может быть в некорректном состоянии
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
            ShowError(e.Exception, "Ошибка в фоновом задании");
            e.SetObserved();   // не даст «упасть» процессу
        }

        private void ShowError(Exception ex, string title) {
            // Можно заменить на свой всплывающий Window или тост
            NotificationService.ShowError($"{title}:\n{ex.Message}");
        }
    }
}
