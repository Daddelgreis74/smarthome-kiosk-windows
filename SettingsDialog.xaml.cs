using System;
using System.Windows;
using System.Windows.Input;
using SmartHomeKiosk.Models;
using SmartHomeKiosk.Services;

namespace SmartHomeKiosk
{
    public enum KioskAction
    {
        None,
        Reload,
        ClearCache,
        ExitApp
    }

    public partial class SettingsDialog : Window
    {
        private readonly KioskConfig _config;
        public KioskAction ActionRequested { get; private set; } = KioskAction.None;

        public SettingsDialog(KioskConfig config)
        {
            InitializeComponent();
            _config = config;

            TxtUrl.Text = _config.DashboardUrl;
            TxtPin.Text = _config.PinCode;
            TxtDimMinutes.Text = _config.DimTimeoutMinutes.ToString();
            TxtStandbyMinutes.Text = _config.HardwareStandbyTimeoutMinutes.ToString();
            ChkAutoHideCursor.IsChecked = _config.AutoHideCursor;
            ChkAutoStart.IsChecked = _config.AutoStartEnabled;

            TxtCurrentVersion.Text = $"v{UpdateService.CurrentVersion.ToString(3)}";
            if (UpdateService.LatestUpdateInfo != null)
            {
                DisplayUpdateInfo(UpdateService.LatestUpdateInfo);
            }

            Loaded += (s, e) => PbPin.Focus();
        }

        private void PbPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnUnlock_Click(sender, e);
            }
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (PbPin.Password == _config.PinCode)
            {
                PinPanel.Visibility = Visibility.Collapsed;
                SettingsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Falscher PIN-Code!", "Zugriff verweigert", MessageBoxButton.OK, MessageBoxImage.Error);
                PbPin.Clear();
                PbPin.Focus();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url) || (!url.StartsWith("http://") && !url.StartsWith("https://")))
            {
                MessageBox.Show("Bitte gib eine gültige URL ein (z. B. http://192.168.178.100:3000)", "Ungültige URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string pin = TxtPin.Text.Trim();
            if (string.IsNullOrEmpty(pin))
            {
                MessageBox.Show("Bitte gib einen Kiosk-PIN ein.", "PIN erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtDimMinutes.Text, out int dimMin) || dimMin < 0) dimMin = 3;
            if (!int.TryParse(TxtStandbyMinutes.Text, out int standbyMin) || standbyMin < 0) standbyMin = 15;

            _config.DashboardUrl = url;
            _config.PinCode = pin;
            _config.DimTimeoutMinutes = dimMin;
            _config.HardwareStandbyTimeoutMinutes = standbyMin;
            _config.AutoHideCursor = ChkAutoHideCursor.IsChecked == true;
            _config.AutoStartEnabled = ChkAutoStart.IsChecked == true;

            AutoStartService.SetAutoStart(_config.AutoStartEnabled);
            ConfigService.SaveConfig(_config);

            DialogResult = true;
            Close();
        }

        private void DisplayUpdateInfo(UpdateInfo info)
        {
            TxtUpdateStatus.Text = $"Neues Update {info.TagName} verfügbar!";
            TxtUpdateStatus.Foreground = (System.Windows.Media.Brush)FindResource("AccentCyan");

            TxtNewVersionTitle.Text = $"Neues Update {info.TagName}: {info.Title}";
            TxtChangelog.Text = string.IsNullOrWhiteSpace(info.Changelog) ? "Keine Release-Notes vorhanden." : info.Changelog;
            UpdateAvailablePanel.Visibility = Visibility.Visible;
        }

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdate.IsEnabled = false;
            TxtUpdateStatus.Text = "Prüfe auf Updates...";
            TxtUpdateStatus.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary");

            try
            {
                var result = await UpdateService.CheckForUpdatesAsync();
                if (result.IsUpdateAvailable && result.Info != null)
                {
                    DisplayUpdateInfo(result.Info);
                }
                else if (result.ErrorMessage != null)
                {
                    TxtUpdateStatus.Text = $"Fehler: {result.ErrorMessage}";
                    TxtUpdateStatus.Foreground = (System.Windows.Media.Brush)FindResource("TextDanger");
                }
                else
                {
                    TxtUpdateStatus.Text = "Status: Sie verwenden bereits die neueste Version.";
                    TxtUpdateStatus.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary");
                    UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                }
            }
            finally
            {
                BtnCheckUpdate.IsEnabled = true;
            }
        }

        private async void BtnInstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            var info = UpdateService.LatestUpdateInfo;
            if (info == null) return;

            var confirm = MessageBox.Show(
                $"Möchtest du das Update {info.TagName} jetzt herunterladen und installieren?\n\nDie Kiosk-App wird für die Aktualisierung kurz geschlossen und startet anschließend automatisch neu.",
                "Kiosk-Update installieren",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            BtnInstallUpdate.IsEnabled = false;
            BtnCheckUpdate.IsEnabled = false;
            UpdateProgressBar.Visibility = Visibility.Visible;
            TxtDownloadProgress.Visibility = Visibility.Visible;
            UpdateProgressBar.Value = 0;
            TxtDownloadProgress.Text = "Download wird gestartet...";

            var progress = new Progress<int>(pct =>
            {
                UpdateProgressBar.Value = pct;
                TxtDownloadProgress.Text = $"Herunterladen... {pct}%";
            });

            try
            {
                using var cts = new System.Threading.CancellationTokenSource();
                await UpdateService.DownloadAndInstallAsync(info, progress, cts.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Herunterladen oder Installieren des Updates:\n\n{ex.Message}", "Update-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnInstallUpdate.IsEnabled = true;
                BtnCheckUpdate.IsEnabled = true;
                UpdateProgressBar.Visibility = Visibility.Collapsed;
                TxtDownloadProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            ActionRequested = KioskAction.Reload;
            DialogResult = true;
            Close();
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            ActionRequested = KioskAction.ClearCache;
            DialogResult = true;
            Close();
        }

        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Möchtest du die Kiosk-App wirklich beenden und zum Windows-Desktop zurückkehren?", "Kiosk beenden", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                ActionRequested = KioskAction.ExitApp;
                DialogResult = true;
                Close();
            }
        }
    }
}
