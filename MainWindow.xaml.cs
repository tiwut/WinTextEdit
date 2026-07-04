using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using FontFamily = System.Windows.Media.FontFamily;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace WinTextEdit
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath;
        private bool _isModified;
        private bool _isInitializing;
        private bool _isInitializingRegistryCheckboxes;
        private readonly ThemeManager _themeManager;
        private double _defaultFontSize = 14;

        public MainWindow()
        {
            InitializeComponent();
            _themeManager = new ThemeManager();

            Editor.TextChanged += Editor_TextChanged;
            Editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            
            Editor.PreviewMouseWheel += Editor_PreviewMouseWheel;
            
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            InitializeThemes();
            InitializeLanguages();
            InitializeRegistrySettings();
            LoadAppSettings();
            _isInitializing = false;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                OpenFile(args[1]);
            }
            else
            {
                UpdateTitle();
            }
        }

        private void InitializeThemes()
        {
            ThemeComboBox.Items.Clear();
            foreach (var theme in _themeManager.LoadedThemes)
            {
                ThemeComboBox.Items.Add(theme.Name);
            }

            string defaultTheme = "Classic Dark";
            if (ThemeComboBox.Items.Contains(defaultTheme))
            {
                ThemeComboBox.SelectedItem = defaultTheme;
            }
            else if (ThemeComboBox.Items.Count > 0)
            {
                ThemeComboBox.SelectedIndex = 0;
            }
        }

        private void InitializeLanguages()
        {
            LanguageComboBox.Items.Clear();
            LanguageComboBox.Items.Add("Plain Text");

            foreach (var definition in HighlightingManager.Instance.HighlightingDefinitions)
            {
                LanguageComboBox.Items.Add(definition.Name);
            }

            LanguageComboBox.SelectedIndex = 0;
        }

        private void InitializeRegistrySettings()
        {
            _isInitializingRegistryCheckboxes = true;
            try
            {
                ContextMenuCheckBox.IsChecked = RegistryHelper.IsContextMenuRegistered();
                DefaultEditorCheckBox.IsChecked = RegistryHelper.IsFileAssociationRegistered();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Registry error: {ex.Message}";
            }
            finally
            {
                _isInitializingRegistryCheckboxes = false;
            }
        }

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (!_isModified)
            {
                _isModified = true;
                UpdateTitle();
            }
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            int line = Editor.TextArea.Caret.Line;
            int column = Editor.TextArea.Caret.Column;
            LineColumnText.Text = $"Ln {line}, Col {column}";
        }

        private void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                e.Handled = true;
                ShowPreviewWindow();
            }
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Untitled" : Path.GetFileName(_currentFilePath);
            string modMarker = _isModified ? "*" : "";
            Title = $"{modMarker}{fileName} - WinTextEdit";
        }

        private void DetectAndSetLanguage(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string lang = ext switch
            {
                ".cs" => "C#",
                ".cpp" or ".hpp" or ".c" or ".h" => "C++",
                ".py" or ".pyw" => "Python",
                ".js" or ".mjs" => "JavaScript",
                ".ts" or ".tsx" => "TypeScript",
                ".html" or ".htm" => "HTML",
                ".xml" or ".xaml" or ".csproj" or ".config" => "XML",
                ".css" => "CSS",
                ".json" => "JSON",
                ".sql" => "SQL",
                ".md" or ".markdown" => "MarkDown",
                ".java" => "Java",
                ".php" => "PHP",
                _ => "Plain Text"
            };

            foreach (var item in LanguageComboBox.Items)
            {
                if (item.ToString()?.Equals(lang, StringComparison.OrdinalIgnoreCase) == true)
                {
                    LanguageComboBox.SelectedItem = item;
                    return;
                }
            }
            LanguageComboBox.SelectedIndex = 0;
        }

        #region File Operations

        private void NewFile()
        {
            if (!PromptSaveIfModified()) return;

            Editor.Text = "";
            _currentFilePath = null;
            _isModified = false;
            LanguageComboBox.SelectedIndex = 0;
            UpdateTitle();
            StatusText.Text = "New file created.";
        }

        private void OpenFile(string filePath)
        {
            try
            {
                Editor.Text = File.ReadAllText(filePath);
                _currentFilePath = filePath;
                _isModified = false;
                DetectAndSetLanguage(filePath);
                UpdateTitle();
                StatusText.Text = $"Opened: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open file:\n{ex.Message}", "Error Opening File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowOpenDialog()
        {
            if (!PromptSaveIfModified()) return;

            var openDialog = new OpenFileDialog
            {
                Filter = "Text Documents (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*",
                Title = "Open File"
            };

            if (openDialog.ShowDialog() == true)
            {
                OpenFile(openDialog.FileName);
            }
        }

        private bool SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return ShowSaveAsDialog();
            }

            try
            {
                File.WriteAllText(_currentFilePath, Editor.Text);
                
                if (Path.GetFileName(_currentFilePath).Equals("custom.yaml", StringComparison.OrdinalIgnoreCase))
                {
                    _themeManager.LoadAllThemes();
                    if (ThemeComboBox.SelectedItem?.ToString() == "Custom")
                    {
                        _themeManager.ApplyTheme(this, Editor, "Custom");
                        StatusText.Text = "Custom theme updated and applied.";
                    }
                }

                _isModified = false;
                UpdateTitle();
                StatusText.Text = "File saved successfully.";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save file:\n{ex.Message}", "Error Saving File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ShowSaveAsDialog()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Text Documents (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*",
                Title = "Save File As"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _currentFilePath = saveDialog.FileName;
                DetectAndSetLanguage(_currentFilePath);
                return SaveFile();
            }
            return false;
        }

        private bool PromptSaveIfModified()
        {
            if (!_isModified) return true;

            var result = MessageBox.Show($"Do you want to save changes to {(string.IsNullOrEmpty(_currentFilePath) ? "Untitled" : Path.GetFileName(_currentFilePath))}?",
                                         "WinTextEdit",
                                         MessageBoxButton.YesNoCancel,
                                         MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                return SaveFile();
            }
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Menu Click Handlers

        private void New_Click(object sender, RoutedEventArgs e) => NewFile();
        private void Open_Click(object sender, RoutedEventArgs e) => ShowOpenDialog();
        private void Save_Click(object sender, RoutedEventArgs e) => SaveFile();
        private void SaveAs_Click(object sender, RoutedEventArgs e) => ShowSaveAsDialog();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            ShowPreviewWindow();
        }

        private void ShowPreviewWindow()
        {
            string selectedLang = LanguageComboBox.SelectedItem?.ToString() ?? "Plain Text";
            var previewWin = new PreviewWindow(this, Editor.Text, selectedLang, _themeManager.CurrentTheme);
            previewWin.Show();
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => Editor.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Editor.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => Editor.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => Editor.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();
        private void SelectAll_Click(object sender, RoutedEventArgs e) => Editor.SelectAll();

        private void WordWrap_Changed(object sender, RoutedEventArgs e)
        {
            if (Editor != null && WordWrapMenuItem != null)
            {
                Editor.WordWrap = WordWrapMenuItem.IsChecked;
            }
        }

        private void LineNumbers_Changed(object sender, RoutedEventArgs e)
        {
            if (Editor != null && LineNumbersMenuItem != null)
            {
                Editor.ShowLineNumbers = LineNumbersMenuItem.IsChecked;
            }
        }

        private void Font_Click(object sender, RoutedEventArgs e)
        {
            using (var fontDialog = new System.Windows.Forms.FontDialog())
            {
                fontDialog.Font = new System.Drawing.Font(Editor.FontFamily.Source, (float)(Editor.FontSize * 72.0 / 96.0));
                
                if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Editor.FontFamily = new FontFamily(fontDialog.Font.Name);
                    Editor.FontSize = fontDialog.Font.Size * 96.0 / 72.0;
                    _defaultFontSize = Editor.FontSize;
                    UpdateZoomDisplay();
                    SaveAppSettings();
                }
            }
        }

        #endregion

        #region Zoom Handlers

        private void ZoomIn()
        {
            if (Editor.FontSize < 96)
            {
                Editor.FontSize += 1.5;
                UpdateZoomDisplay();
            }
        }

        private void ZoomOut()
        {
            if (Editor.FontSize > 6)
            {
                Editor.FontSize -= 1.5;
                UpdateZoomDisplay();
            }
        }

        private void ZoomReset()
        {
            Editor.FontSize = _defaultFontSize;
            UpdateZoomDisplay();
        }

        private void UpdateZoomDisplay()
        {
            double ratio = (Editor.FontSize / _defaultFontSize) * 100;
            ZoomText.Text = $"{ratio:F0}%";
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
        private void ZoomReset_Click(object sender, RoutedEventArgs e) => ZoomReset();

        #endregion

        #region Theme and Language Selection

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem == null) return;
            string selectedTheme = ThemeComboBox.SelectedItem.ToString()!;
            _themeManager.ApplyTheme(this, Editor, selectedTheme);
            StatusText.Text = $"Applied theme: {selectedTheme}";
            if (!_isInitializing)
            {
                SaveAppSettings();
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem == null) return;
            string selectedLang = LanguageComboBox.SelectedItem.ToString()!;
            
            if (selectedLang == "Plain Text")
            {
                Editor.SyntaxHighlighting = null;
            }
            else
            {
                Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(selectedLang);
            }

            ThemeManager.ApplyTheme(this, Editor, _themeManager.CurrentTheme);
            StatusText.Text = $"Language set to: {selectedLang}";
        }

        private void EditCustomTheme_Click(object sender, RoutedEventArgs e)
        {
            string themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes", "custom.yaml");
            if (!File.Exists(themePath))
            {
                MessageBox.Show("Custom theme file custom.yaml does not exist in the themes folder.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                "Would you like to open the custom theme configuration file (custom.yaml) in the editor? Saving edits to this file will immediately update and apply the custom theme styling.",
                "Edit Custom Theme",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                OpenFile(themePath);
                SettingsPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Settings Panel

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = (SettingsPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Windows Integration Registry Handlers

        private void ContextMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingRegistryCheckboxes) return;
            try
            {
                RegistryHelper.RegisterContextMenu();
                StatusText.Text = "Right-click context menu registered.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register context menu:\n{ex.Message}", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeRegistrySettings();
            }
        }

        private void ContextMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingRegistryCheckboxes) return;
            try
            {
                RegistryHelper.UnregisterContextMenu();
                StatusText.Text = "Right-click context menu unregistered.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unregister context menu:\n{ex.Message}", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeRegistrySettings();
            }
        }

        private void DefaultEditor_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingRegistryCheckboxes) return;
            try
            {
                RegistryHelper.RegisterFileAssociations();
                StatusText.Text = "File associations registered.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register file associations:\n{ex.Message}", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeRegistrySettings();
            }
        }

        private void DefaultEditor_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingRegistryCheckboxes) return;
            try
            {
                RegistryHelper.UnregisterFileAssociations();
                StatusText.Text = "File associations unregistered.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unregister file associations:\n{ex.Message}", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeRegistrySettings();
            }
        }

        #endregion

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link:\n{ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveAppSettings();
            if (!PromptSaveIfModified())
            {
                e.Cancel = true;
            }
        }

        #region App Settings Persistence

        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private void LoadAppSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        if (ThemeComboBox.Items.Contains(settings.LastTheme))
                        {
                            ThemeComboBox.SelectedItem = settings.LastTheme;
                        }
                        
                        if (!string.IsNullOrEmpty(settings.LastFontFamily))
                        {
                            Editor.FontFamily = new FontFamily(settings.LastFontFamily);
                        }
                        if (settings.LastFontSize >= 6 && settings.LastFontSize <= 96)
                        {
                            Editor.FontSize = settings.LastFontSize;
                            _defaultFontSize = settings.LastFontSize;
                            UpdateZoomDisplay();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveAppSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    LastTheme = ThemeComboBox.SelectedItem?.ToString() ?? "Classic Dark",
                    LastFontFamily = Editor.FontFamily.Source,
                    LastFontSize = _defaultFontSize
                };
                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
            }
        }

        #endregion
    }

    public class AppSettings
    {
        public string LastTheme { get; set; } = "Classic Dark";
        public string LastFontFamily { get; set; } = "Consolas";
        public double LastFontSize { get; set; } = 14;
    }
}