using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SmartHomeKiosk.Services
{
    public class UpdateInfo
    {
        public Version RemoteVersion { get; set; } = new Version(1, 0, 0);
        public string TagName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Changelog { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public long AssetSize { get; set; }
        public bool IsSetupInstaller { get; set; }
    }

    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public UpdateInfo? Info { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Daddelgreis74/smarthome-kiosk-windows/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static System.Threading.Timer? _periodicTimer;

        public static event Action<UpdateInfo>? UpdateAvailable;
        public static UpdateInfo? LatestUpdateInfo { get; private set; }

        public static Version CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(1, 0, 1);
            }
        }

        public static bool IsInstalledMode
        {
            get
            {
                string uninstallerPath = Path.Combine(AppContext.BaseDirectory, "unins000.exe");
                return File.Exists(uninstallerPath);
            }
        }

        static UpdateService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartHomeKiosk-Updater");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public static void StartPeriodicChecks(TimeSpan initialDelay, TimeSpan interval)
        {
            _periodicTimer?.Dispose();
            _periodicTimer = new System.Threading.Timer(async _ =>
            {
                var result = await CheckForUpdatesAsync();
                if (result.IsUpdateAvailable && result.Info != null)
                {
                    LatestUpdateInfo = result.Info;
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        UpdateAvailable?.Invoke(result.Info);
                    });
                }
            }, null, initialDelay, interval);
        }

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync(GitHubApiUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        IsUpdateAvailable = false,
                        ErrorMessage = $"GitHub API antwortete mit Status {(int)response.StatusCode} {response.ReasonPhrase}"
                    };
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString() ?? "";
                string title = root.TryGetProperty("name", out var n) ? n.GetString() ?? tagName : tagName;
                string body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

                // Version parsen (z.B. "v1.0.2" -> "1.0.2")
                string cleanVersionStr = tagName.TrimStart('v', 'V');
                if (!Version.TryParse(cleanVersionStr, out var remoteVersion))
                {
                    // Fallback falls 2-teilig (z.B. "1.0")
                    if (cleanVersionStr.Split('.').Length == 2)
                    {
                        Version.TryParse(cleanVersionStr + ".0", out remoteVersion);
                    }
                }

                if (remoteVersion == null)
                {
                    return new UpdateCheckResult
                    {
                        IsUpdateAvailable = false,
                        ErrorMessage = $"Ungültiges Versionsformat in Release: {tagName}"
                    };
                }

                // Prüfen ob neuere Version
                bool isNewer = remoteVersion > CurrentVersion;

                // Passendes Asset finden
                bool preferSetup = IsInstalledMode;
                string setupUrl = "";
                long setupSize = 0;
                string portableUrl = "";
                long portableSize = 0;

                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string assetName = asset.GetProperty("name").GetString() ?? "";
                        string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        long size = asset.TryGetProperty("size", out var s) ? s.GetInt64() : 0;

                        if (assetName.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            setupUrl = downloadUrl;
                            setupSize = size;
                        }
                        else if (assetName.Equals("SmartHomeKiosk.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            portableUrl = downloadUrl;
                            portableSize = size;
                        }
                    }
                }

                string chosenUrl = preferSetup ? setupUrl : portableUrl;
                long chosenSize = preferSetup ? setupSize : portableSize;
                bool isSetup = preferSetup;

                if (string.IsNullOrEmpty(chosenUrl))
                {
                    // Fallback
                    if (!string.IsNullOrEmpty(setupUrl))
                    {
                        chosenUrl = setupUrl;
                        chosenSize = setupSize;
                        isSetup = true;
                    }
                    else if (!string.IsNullOrEmpty(portableUrl))
                    {
                        chosenUrl = portableUrl;
                        chosenSize = portableSize;
                        isSetup = false;
                    }
                }

                var updateInfo = new UpdateInfo
                {
                    RemoteVersion = remoteVersion,
                    TagName = tagName,
                    Title = title,
                    Changelog = body,
                    DownloadUrl = chosenUrl,
                    AssetSize = chosenSize,
                    IsSetupInstaller = isSetup
                };

                if (isNewer)
                {
                    LatestUpdateInfo = updateInfo;
                }

                return new UpdateCheckResult
                {
                    IsUpdateAvailable = isNewer,
                    Info = updateInfo
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateService.CheckForUpdatesAsync error: {ex.Message}");
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public static async Task DownloadAndInstallAsync(UpdateInfo info, IProgress<int>? progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(info.DownloadUrl))
            {
                throw new InvalidOperationException("Keine gültige Download-URL für das Update vorhanden.");
            }

            string tempDir = Path.GetTempPath();
            string targetFileName = info.IsSetupInstaller ? "SmartHomeKiosk-Setup_update.exe" : "SmartHomeKiosk_new.exe";
            string tempFilePath = Path.Combine(tempDir, targetFileName);

            // Datei herunterladen mit Fortschritt
            using (var response = await _httpClient.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? info.AssetSize;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long bytesReadTotal = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        bytesReadTotal += bytesRead;

                        if (totalBytes > 0)
                        {
                            int pct = (int)((bytesReadTotal * 100) / totalBytes);
                            progress?.Report(pct);
                        }
                    }
                }
            }

            // Installation / Neustart ausführen
            if (info.IsSetupInstaller)
            {
                // Inno Setup Silent Installation
                var psi = new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    Arguments = "/SILENT /SUPPRESSMSGBOXES /FORCECLOSEAPPLICATIONS",
                    UseShellExecute = true
                };

                Process.Start(psi);
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            }
            else
            {
                // Portable Modus: Über kleinen CMD-Helper austauschen und neu starten
                string currentExe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "SmartHomeKiosk.exe");
                
                string cmdArgs = $"/c ping 127.0.0.1 -n 3 >nul & move /y \"{tempFilePath}\" \"{currentExe}\" & start \"\" \"{currentExe}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };

                Process.Start(psi);
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            }
        }
    }
}
