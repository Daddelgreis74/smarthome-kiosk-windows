using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace SmartHomeKiosk.Services
{
    public static class AutoStartService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SmartHomeKiosk";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                if (key == null) return false;
                var value = key.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoStartService.IsAutoStartEnabled error: {ex.Message}");
                return false;
            }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoStartService.SetAutoStart error: {ex.Message}");
            }
        }
    }
}
