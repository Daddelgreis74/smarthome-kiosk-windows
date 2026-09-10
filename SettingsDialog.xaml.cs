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
