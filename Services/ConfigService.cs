using System;
using System.IO;
using System.Text.Json;
using SmartHomeKiosk.Models;

namespace SmartHomeKiosk.Services
{
    public static class ConfigService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "SmartHomeKiosk");
            
        private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "appsettings.json");
        private static readonly string LocalConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static KioskConfig LoadConfig()
        {
            try
            {
                // 1. Zuerst prüfen, ob bereits eine konfigurierte Datei in AppData existiert
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var cfg = JsonSerializer.Deserialize<KioskConfig>(json, JsonOptions);
                    if (cfg != null) return cfg;
                }

                // 2. Fallback: Vorlage aus Anwendungsverzeichnis laden
                if (File.Exists(LocalConfigFilePath))
                {
                    string json = File.ReadAllText(LocalConfigFilePath);
                    var cfg = JsonSerializer.Deserialize<KioskConfig>(json, JsonOptions);
                    if (cfg != null) return cfg;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
            }

            return new KioskConfig();
        }

        public static void SaveConfig(KioskConfig config)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigFilePath, json);

                // Optional auch im lokalen Verzeichnis synchronisieren, wenn Schreibrechte da sind
                try
                {
                    File.WriteAllText(LocalConfigFilePath, json);
                }
                catch
                {
                    // Ignorieren falls im schreibgeschützten Ordner (z.B. Program Files)
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Konfiguration: {ex.Message}");
            }
        }
    }
}
