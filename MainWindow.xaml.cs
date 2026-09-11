using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using SmartHomeKiosk.Models;
using SmartHomeKiosk.Services;

namespace SmartHomeKiosk
{
    public partial class MainWindow : Window
    {
        private KioskConfig _config;
        private DateTime _lastActivityTime = DateTime.UtcNow;
        private bool _isHardwareDisplayOff = false;

        private readonly DispatcherTimer _inactivityCheckTimer;
        private readonly DispatcherTimer _cursorHideTimer;
        private readonly DispatcherTimer _retryTimer;
        private readonly DispatcherTimer _cornerClickResetTimer;
        private int _cornerClickCount = 0;
        private readonly MouseHookService _mouseHook;
        private bool _isSettingsOpen = false;

        // Edge-Swipe Geste (vom linken Bildschirmrand nach rechts)
        private Point? _swipeStartPoint;
        private bool _isEdgeSwiping;

        public MainWindow(KioskConfig config)
        {
            InitializeComponent();
            _config = config;

            // 1. Inaktivitäts-Timer (prüft jede Sekunde Standby-Bedingungen)
            _inactivityCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _inactivityCheckTimer.Tick += InactivityCheckTimer_Tick;
            _inactivityCheckTimer.Start();

            // 2. Cursor-Auto-Hide Timer (nach X Sekunden Mauszeiger ausblenden)
            _cursorHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_config.HideCursorDelaySeconds) };
            _cursorHideTimer.Tick += (s, e) =>
            {
                if (!_isSettingsOpen && _config.AutoHideCursor)
                {
                    Mouse.OverrideCursor = Cursors.None;
                }
                _cursorHideTimer.Stop();
            };
            if (_config.AutoHideCursor)
            {
                _cursorHideTimer.Start();
            }

            // 3. Retry-Timer für Verbindungsabbruch / Server-Start
            _retryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _retryTimer.Tick += (s, e) =>
            {
                if (KioskBrowser.CoreWebView2 != null)
                {
                    KioskBrowser.CoreWebView2.Navigate(_config.DashboardUrl);
                }
            };

            // 4. Dreifach-Klick Timer für linke obere Ecke (Maus-Shortcut)
            _cornerClickResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
            _cornerClickResetTimer.Tick += (s, e) =>
            {
                _cornerClickCount = 0;
                _cornerClickResetTimer.Stop();
            };

            // 6. Globaler Win32 Mouse Hook (fängt Klicks zuverlässig über dem nativen WebView2 HWND ab)
            _mouseHook = new MouseHookService(this);
            _mouseHook.ActivityDetected += ResetActivity;
            _mouseHook.MiddleClickDetected += () => OpenSettings();
            _mouseHook.LeftMouseDownDetected += (screenX, screenY) =>
            {
                try
                {
                    var point = PointFromScreen(new Point(screenX, screenY));
                    if (point.X >= 0 && point.X <= 120 && point.Y >= 0 && point.Y <= 120)
                    {
                        _cornerClickCount++;
                        _cornerClickResetTimer.Stop();
                        _cornerClickResetTimer.Start();

                        if (_cornerClickCount >= 3)
                        {
                            _cornerClickCount = 0;
                            _cornerClickResetTimer.Stop();
                            OpenSettings();
                            return;
                        }
                    }
                }
                catch { }
            };

            Closing += (s, e) =>
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartHomeKiosk");
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow Closing...\n");
            };
            Closed += (s, e) =>
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartHomeKiosk");
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow Closed!\n");
            };

            // 7. Update-Service initialisieren & Hintergrund-Prüfung alle 24h starten
            UpdateService.UpdateAvailable += OnUpdateAvailable;
            UpdateService.StartPeriodicChecks(TimeSpan.FromSeconds(8), TimeSpan.FromHours(24));

            Loaded += MainWindow_Loaded;
        }

        private void OnUpdateAvailable(UpdateInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateBadge.Visibility = Visibility.Visible;
                UpdateBadge.ToolTip = $"Neues Kiosk-Update {info.TagName} verfügbar!\nAntippen zum Installieren.";
            });
        }

        private void UpdateBadge_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ResetActivity();
            OpenSettings();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitWebViewAsync();
        }

        private async Task InitWebViewAsync()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartHomeKiosk");
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] InitWebViewAsync gestartet...\n");

                string dataFolder = Path.Combine(dir, "WebView2Data");

                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--ignore-certificate-errors --allow-insecure-localhost"
                };

                var env = await CoreWebView2Environment.CreateAsync(null, dataFolder, options);
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] CoreWebView2Environment erstellt.\n");

                await KioskBrowser.EnsureCoreWebView2Async(env);
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] EnsureCoreWebView2Async erfolgreich.\n");

                // Selbstsignierte und LAN-Zertifikate (z.B. https://192.168.178.100:8443/) immer akzeptieren
                KioskBrowser.CoreWebView2.ServerCertificateErrorDetected += (s, args) =>
                {
                    args.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
                };

                var settings = KioskBrowser.CoreWebView2.Settings;
                settings.AreDevToolsEnabled = false;
                settings.AreDefaultContextMenusEnabled = !_config.DisableContextMenu;
                settings.IsZoomControlEnabled = !_config.DisableTouchZoom;
                settings.IsPinchZoomEnabled = !_config.DisableTouchZoom;
                settings.IsSwipeNavigationEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
                settings.IsStatusBarEnabled = false;

                // Automatische Berechtigungen (Audio, Kamera, Mikrofon, Geolocation)
                if (_config.AutoApprovePermissions)
                {
                    KioskBrowser.CoreWebView2.PermissionRequested += (s, args) =>
                    {
                        args.State = CoreWebView2PermissionState.Allow;
                    };
                }

                // Script Injection fuer Mausrad-Klick (Middle Click) im Browser
                await KioskBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    window.addEventListener('mousedown', function(e) {
                        if (e.button === 1) { // Mittlere Maustaste
                            window.chrome.webview.postMessage('open_settings');
                        }
                    }, true);
                ");
                KioskBrowser.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    try
                    {
                        string msg = args.TryGetWebMessageAsString();
                        if (msg == "open_settings")
                        {
                            Dispatcher.Invoke(OpenSettings);
                        }
                    }
                    catch { }
                };

                // Navigations-Ereignisse
                KioskBrowser.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        WatchdogOverlay.Visibility = Visibility.Collapsed;
                        _retryTimer.Stop();
                    }
                    else
                    {
                        TxtWatchdogUrl.Text = $"Ziel: {_config.DashboardUrl}\nStatus: {args.WebErrorStatus}";
                        WatchdogOverlay.Visibility = Visibility.Visible;
                        if (!_retryTimer.IsEnabled) _retryTimer.Start();
                    }
                };

                // Erste Navigation
                TxtWatchdogUrl.Text = $"Ziel: {_config.DashboardUrl}";
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] Navigiere zu {_config.DashboardUrl}...\n");
                KioskBrowser.CoreWebView2.Navigate(_config.DashboardUrl);
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] InitWebViewAsync erfolgreich beendet.\n");
            }
            catch (Exception ex)
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartHomeKiosk");
                File.AppendAllText(Path.Combine(dir, "startup.log"), $"[{DateTime.Now:HH:mm:ss.fff}] InitWebViewAsync FEHLER: {ex}\n");
                MessageBox.Show($"Fehler beim Initialisieren der WebView2 Engine:\n{ex.Message}", "WebView2 Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InactivityCheckTimer_Tick(object? sender, EventArgs e)
        {
            if (_isSettingsOpen) return;

            var inactive = DateTime.UtcNow - _lastActivityTime;

            if (_config.IsHybridStandbyEnabled)
            {
                // Stufe 1: Schwarzer Software-Schoner (Dimming)
                if (inactive.TotalMinutes >= _config.DimTimeoutMinutes && ScreenSaverOverlay.Visibility != Visibility.Visible)
                {
                    ScreenSaverOverlay.Visibility = Visibility.Visible;
                }

                // Stufe 2: Physisches Hardware-Standby
                if (inactive.TotalMinutes >= _config.HardwareStandbyTimeoutMinutes && !_isHardwareDisplayOff)
                {
                    _isHardwareDisplayOff = true;
                    PowerService.TurnOffDisplay();
                }
            }
        }

        private void ResetActivity()
        {
            if (_isSettingsOpen)
            {
                Mouse.OverrideCursor = null;
                return;
            }

            _lastActivityTime = DateTime.UtcNow;

            // Monitor aufwecken falls im Hardware-Standby
            if (_isHardwareDisplayOff)
            {
                PowerService.TurnOnDisplay();
                _isHardwareDisplayOff = false;
            }

            // Software-Schoner ausblenden
            if (ScreenSaverOverlay.Visibility == Visibility.Visible)
            {
                ScreenSaverOverlay.Visibility = Visibility.Collapsed;
            }

            // Cursor wieder anzeigen und Auto-Hide neu triggern
            Mouse.OverrideCursor = null;
            _cursorHideTimer.Stop();
            if (_config.AutoHideCursor)
            {
                _cursorHideTimer.Start();
            }
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e) => ResetActivity();
        
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetActivity();

            // Schneller Kiosk-Zugriff via Klick auf das Mausrad (mittlere Maustaste)
            if (e.ChangedButton == MouseButton.Middle)
            {
                OpenSettings();
                e.Handled = true;
            }
        }

        private void Window_PreviewTouchDown(object sender, TouchEventArgs e) => ResetActivity();
        private void ScreenSaver_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ResetActivity();
        private void ScreenSaver_PreviewTouchDown(object sender, TouchEventArgs e) => ResetActivity();

        // Edge-Swipe Methoden (vom linken Rand nach rechts)
        private void StartEdgeSwipe(Point pt)
        {
            _swipeStartPoint = pt;
            _isEdgeSwiping = true;
        }

        private void CheckEdgeSwipe(Point currentPt)
        {
            if (!_isEdgeSwiping || _swipeStartPoint == null) return;

            double deltaX = currentPt.X - _swipeStartPoint.Value.X;
            double deltaY = Math.Abs(currentPt.Y - _swipeStartPoint.Value.Y);

            // Geste: Von links nach rechts mind. 85px wischen, max. 120px vertikale Abweichung
            if (deltaX >= 85 && deltaY <= 120)
            {
                CancelEdgeSwipe();
                OpenSettings();
            }
        }

        private void CancelEdgeSwipe()
        {
            _isEdgeSwiping = false;
            _swipeStartPoint = null;
            try
            {
                LeftEdgeSwipeZone.ReleaseMouseCapture();
                LeftEdgeSwipeZone.ReleaseAllTouchCaptures();
            }
            catch { }
        }

        // Linke Edge-Swipe Handlers
        private void EdgeSwipe_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            ResetActivity();
            var touch = e.GetTouchPoint(this);
            StartEdgeSwipe(touch.Position);
            try { LeftEdgeSwipeZone.CaptureTouch(e.TouchDevice); } catch { }
        }

        private void EdgeSwipe_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            ResetActivity();
            var touch = e.GetTouchPoint(this);
            CheckEdgeSwipe(touch.Position);
        }

        private void EdgeSwipe_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            CancelEdgeSwipe();
        }

        private void EdgeSwipe_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetActivity();
            if (e.ChangedButton == MouseButton.Left)
            {
                StartEdgeSwipe(e.GetPosition(this));
                try { LeftEdgeSwipeZone.CaptureMouse(); } catch { }
            }
        }

        private void EdgeSwipe_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ResetActivity();
            if (_isEdgeSwiping && e.LeftButton == MouseButtonState.Pressed)
            {
                CheckEdgeSwipe(e.GetPosition(this));
            }
        }

        private void EdgeSwipe_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            CancelEdgeSwipe();
        }

        // Tastenkombinationen (z. B. Notfall F2 oder Strg+Shift+S)
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 || (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
            {
                OpenSettings();
            }
        }

        private void BtnWatchdogSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

        private async void OpenSettings()
        {
            if (_isSettingsOpen) return;
            _isSettingsOpen = true;

            CancelEdgeSwipe();
            _cursorHideTimer.Stop();
            _inactivityCheckTimer.Stop();
            _cornerClickResetTimer.Stop();
            _cornerClickCount = 0;
            Mouse.OverrideCursor = null;

            try
            {
                var dialog = new SettingsDialog(_config)
                {
                    Owner = this
                };

                bool? result = dialog.ShowDialog();

                if (dialog.ActionRequested == KioskAction.ExitApp)
                {
                    Application.Current.Shutdown();
                    return;
                }

                if (dialog.ActionRequested == KioskAction.Reload)
                {
                    KioskBrowser.CoreWebView2?.Reload();
                    return;
                }

                if (dialog.ActionRequested == KioskAction.ClearCache)
                {
                    if (KioskBrowser.CoreWebView2 != null)
                    {
                        await KioskBrowser.CoreWebView2.Profile.ClearBrowsingDataAsync();
                        KioskBrowser.CoreWebView2.Reload();
                    }
                    return;
                }

                if (result == true)
                {
                    // Konfiguration neu einlesen
                    string oldUrl = _config.DashboardUrl;
                    _config = ConfigService.LoadConfig();

                    if (oldUrl != _config.DashboardUrl && KioskBrowser.CoreWebView2 != null)
                    {
                        TxtWatchdogUrl.Text = $"Ziel: {_config.DashboardUrl}";
                        KioskBrowser.CoreWebView2.Navigate(_config.DashboardUrl);
                    }
                }
            }
            finally
            {
                _isSettingsOpen = false;
                Mouse.OverrideCursor = null;
                _inactivityCheckTimer.Start();
                ResetActivity();
            }
        }
    }
}
