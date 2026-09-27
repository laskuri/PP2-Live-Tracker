using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PP2_Live_Tracker
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException +=
                App_DispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;
        }

        private static void App_DispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            ShowAndSaveError(e.Exception);
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            Exception exception =
                e.ExceptionObject as Exception ??
                new Exception(
                    e.ExceptionObject?.ToString() ??
                    "Tuntematon käynnistysvirhe.");

            ShowAndSaveError(exception);
        }

        private static void ShowAndSaveError(
            Exception exception)
        {
            string errorText =
                exception.ToString();

            try
            {
                string logPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "startup-error.txt");

                File.WriteAllText(
                    logPath,
                    errorText);
            }
            catch
            {
                // Lokin kirjoittaminen epäonnistui.
            }

            MessageBox.Show(
                errorText,
                "PP2 Live Tracker - käynnistysvirhe",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}