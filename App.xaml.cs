using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace Brew3
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Глобальная обработка необработанных исключений
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Произошла необработанная ошибка:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true; // Помечаем как обработанное, чтобы приложение не закрылось
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"Критическая ошибка:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Критическая ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

}
