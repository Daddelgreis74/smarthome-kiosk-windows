using System;
using System.Windows;
using SmartHomeKiosk.Models;
using SmartHomeKiosk.Services;

namespace SmartHomeKiosk
{
    public partial class SetupWizardWindow : Window
    {
        private readonly KioskConfig _config;

        public SetupWizardWindow(KioskConfig config)
        {
            InitializeComponent();
            _config = config;

            if (!string.IsNullOrEmpty(_config.DashboardUrl))
            {
                TxtUrl.Text = _config.DashboardUrl;
                TxtUrlPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtUrl.Text = "";
                TxtUrlPlaceholder.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(_config.PinCode))
            {
                TxtPin.Text = _config.PinCode;
            }
            TxtDimMinutes.Text = _config.DimTimeoutMinutes.ToString();
            TxtStandbyMinutes.Text = _config.HardwareStandbyTimeoutMinutes.ToString();
            ChkAutoHideCursor.IsChecked = _config.AutoHideCursor;
            ChkAutoStart.IsChecked = _config.AutoStartEnabled;
        }

        private void TxtUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TxtUrlPlaceholder != null)
            {
                TxtUrlPlaceholder.Visibility = string.IsNullOrEmpty(TxtUrl.Text) ? Visibility.Visible : Visibility.Collapsed;
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
            _config.IsConfigured = true;

            // Autostart im Windows-System anpassen
            AutoStartService.SetAutoStart(_config.AutoStartEnabled);

            // Konfiguration speichern
            ConfigService.SaveConfig(_config);

            DialogResult = true;
            Close();
        }
    }
}
