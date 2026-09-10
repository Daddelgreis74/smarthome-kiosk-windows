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
                using var cuKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var cuValue = cuKey?.GetValue(AppName) as string;
                if (!string.IsNullOrEmpty(cuValue)) return true;

                using var lmKey = Registry.LocalMachine.OpenSubKey(RunKeyPath, false);
                var lmValue = lmKey?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(lmValue);
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
                using var cuKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath) && cuKey != null)
                    {
                        cuKey.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    cuKey?.DeleteValue(AppName, false);
                    try
                    {
                        using var lmKey = Registry.LocalMachine.OpenSubKey(RunKeyPath, true);
                        lmKey?.DeleteValue(AppName, false);
                    }
                    catch { /* LocalMachine might require admin rights */ }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoStartService.SetAutoStart error: {ex.Message}");
            }
        }
    }
}
