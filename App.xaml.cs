using System;
using System.Threading;
using System.Windows;
using SmartHomeKiosk.Models;
using SmartHomeKiosk.Services;

namespace SmartHomeKiosk
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;

        private static void LogStartup(string msg)
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartHomeKiosk");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
            }
            catch { }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LogStartup("Application_Startup gestartet");

            // Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogStartup($"UnhandledException: {args.ExceptionObject}");
                MessageBox.Show($"Unerwarteter Fehler beim Starten:\n{args.ExceptionObject}", "SmartHome Kiosk Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            DispatcherUnhandledException += (s, args) =>
            {
                LogStartup($"DispatcherUnhandledException: {args.Exception.Message}\n{args.Exception.StackTrace}");
                MessageBox.Show($"UI Fehler:\n{args.Exception.Message}", "SmartHome Kiosk Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            // Verhindert automatisches Beenden beim Schließen des Setup-Dialogs
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Single-Instance Check
            _singleInstanceMutex = new Mutex(true, "SmartHomeKiosk_SingleInstance_Mutex", out bool isNewInstance);
            LogStartup($"Mutex erstellt, isNewInstance: {isNewInstance}");
            if (!isNewInstance)
            {
                LogStartup("Zweite Instanz erkannt -> Shutdown");
                MessageBox.Show("SmartHome Kiosk läuft bereits im Hintergrund.", "Bereits gestartet", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            // 2. Ruhezustand unterdrücken
            PowerService.PreventSystemSleep();
            LogStartup("PowerService.PreventSystemSleep aufgerufen");

            // 3. Konfiguration laden
            KioskConfig config = ConfigService.LoadConfig();
            LogStartup($"Config geladen. IsConfigured={config.IsConfigured}, Url={config.DashboardUrl}");

            // 4. Falls noch nicht eingerichtet, Setup-Assistent anzeigen
            if (!config.IsConfigured)
            {
                LogStartup("Zeige SetupWizardWindow...");
                var wizard = new SetupWizardWindow(config);
                bool? result = wizard.ShowDialog();
                LogStartup($"SetupWizardWindow Ergebnis: {result}");

                if (result != true)
                {
                    LogStartup("SetupWizard abgebrochen -> Shutdown");
                    Current.Shutdown();
                    return;
                }

                // Neu geladene / gespeicherte Konfiguration übernehmen
                config = ConfigService.LoadConfig();
            }

            // 5. Haupt-Kiosk-Fenster öffnen
            LogStartup("Erstelle MainWindow...");
            var mainWindow = new MainWindow(config);
            Current.MainWindow = mainWindow;
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            LogStartup("MainWindow.Show()...");
            mainWindow.Show();
            LogStartup("MainWindow.Show() beendet.");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            LogStartup($"Application_Exit aufgerufen! ExitCode={e.ApplicationExitCode}");
            PowerService.AllowSystemSleep();

            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
                catch { }
            }
        }
    }
}
