using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WinTextEdit
{
    public static class RegistryHelper
    {
        private const string ContextMenuKeyPath = @"Software\Classes\*\shell\OpenWithWinTextEdit";
        private const string ProgId = "WinTextEdit.AssocFile";
        private const string ProgIdKeyPath = @"Software\Classes\" + ProgId;
        private static readonly string[] HandledExtensions = { ".txt", ".log", ".md", ".ini", ".cfg", ".yaml", ".json" };

        private static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        public static bool IsContextMenuRegistered()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(ContextMenuKeyPath))
                {
                    if (key == null) return false;
                    using (var commandKey = key.OpenSubKey("command"))
                    {
                        if (commandKey == null) return false;
                        string? value = commandKey.GetValue("") as string;
                        string currentExe = GetExecutablePath();
                        return value != null && value.Contains(currentExe);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public static void RegisterContextMenu()
        {
            try
            {
                string exePath = GetExecutablePath();
                using (var key = Registry.CurrentUser.CreateSubKey(ContextMenuKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("", "Open with WinTextEdit");
                        key.SetValue("Icon", exePath);
                        using (var commandKey = key.CreateSubKey("command"))
                        {
                            commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to register context menu in Registry.", ex);
            }
        }

        public static void UnregisterContextMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(ContextMenuKeyPath, throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unregister context menu from Registry.", ex);
            }
        }

        public static bool IsFileAssociationRegistered()
        {
            try
            {
                using (var progIdKey = Registry.CurrentUser.OpenSubKey(ProgIdKeyPath))
                {
                    if (progIdKey == null) return false;
                    using (var commandKey = progIdKey.OpenSubKey(@"shell\open\command"))
                    {
                        if (commandKey == null) return false;
                        string? value = commandKey.GetValue("") as string;
                        string currentExe = GetExecutablePath();
                        if (value == null || !value.Contains(currentExe)) return false;
                    }
                }

                using (var txtKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.txt\OpenWithProgids"))
                {
                    return txtKey?.GetValue(ProgId) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void RegisterFileAssociations()
        {
            try
            {
                string exePath = GetExecutablePath();

                using (var progIdKey = Registry.CurrentUser.CreateSubKey(ProgIdKeyPath))
                {
                    if (progIdKey != null)
                    {
                        progIdKey.SetValue("", "Text Document (WinTextEdit)");
                        using (var commandKey = progIdKey.CreateSubKey(@"shell\open\command"))
                        {
                            commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }

                foreach (string ext in HandledExtensions)
                {
                    string path = $@"Software\Classes\{ext}\OpenWithProgids";
                    using (var key = Registry.CurrentUser.CreateSubKey(path))
                    {
                        key?.SetValue(ProgId, "");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to register file associations in Registry.", ex);
            }
        }

        public static void UnregisterFileAssociations()
        {
            try
            {
                foreach (string ext in HandledExtensions)
                {
                    string path = $@"Software\Classes\{ext}\OpenWithProgids";
                    using (var key = Registry.CurrentUser.OpenSubKey(path, writable: true))
                    {
                        key?.DeleteValue(ProgId, throwOnMissingValue: false);
                    }
                }

                Registry.CurrentUser.DeleteSubKeyTree(ProgIdKeyPath, throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unregister file associations from Registry.", ex);
            }
        }
    }
}
