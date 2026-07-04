using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;

namespace WinTextEdit
{
    public class Theme
    {
        public string Name { get; set; } = "Untitled Theme";
        public string Background { get; set; } = "#FFFFFF";
        public string Foreground { get; set; } = "#000000";
        public string CaretColor { get; set; } = "#000000";
        public string SelectionBackground { get; set; } = "#ADD6FF";
        public string SelectionForeground { get; set; } = "#000000";
        public string LineNumbersForeground { get; set; } = "#A0A0A0";
        public string CurrentLineBackground { get; set; } = "#F0F0F0";
        public string MenuBackground { get; set; } = "#F5F5F5";
        public string MenuForeground { get; set; } = "#000000";
        public string MenuBorder { get; set; } = "#E0E0E0";
        public string StatusBarBackground { get; set; } = "#007ACC";
        public string StatusBarForeground { get; set; } = "#FFFFFF";
        public Dictionary<string, string> Syntax { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class ThemeManager
    {
        private static readonly string ThemesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes");
        public List<Theme> LoadedThemes { get; private set; } = new List<Theme>();
        public Theme CurrentTheme { get; private set; } = new Theme();

        public ThemeManager()
        {
            EnsureThemesExist();
            LoadAllThemes();
        }

        private void EnsureThemesExist()
        {
            if (!Directory.Exists(ThemesDirectory))
            {
                Directory.CreateDirectory(ThemesDirectory);
            }

            var defaults = GetDefaultThemes();
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var theme in defaults)
            {
                string safeName = theme.Name.Replace(" ", "_").ToLower();
                string filePath = Path.Combine(ThemesDirectory, $"{safeName}.yaml");
                if (!File.Exists(filePath))
                {
                    try
                    {
                        string yaml = serializer.Serialize(theme);
                        File.WriteAllText(filePath, yaml);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to write default theme: {ex.Message}");
                    }
                }
            }
        }

        public void LoadAllThemes()
        {
            LoadedThemes.Clear();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            if (Directory.Exists(ThemesDirectory))
            {
                var files = Directory.GetFiles(ThemesDirectory, "*.yaml");
                foreach (var file in files)
                {
                    try
                    {
                        string yaml = File.ReadAllText(file);
                        var theme = deserializer.Deserialize<Theme>(yaml);
                        if (theme != null)
                        {
                            LoadedThemes.Add(theme);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading theme {Path.GetFileName(file)}:\n{ex.Message}", "Theme Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            if (LoadedThemes.Count == 0)
            {
                LoadedThemes.Add(new Theme { Name = "Classic Light" });
            }
        }

        public void SaveCustomTheme(Theme theme)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string filePath = Path.Combine(ThemesDirectory, "custom.yaml");
            try
            {
                string yaml = serializer.Serialize(theme);
                File.WriteAllText(filePath, yaml);
                LoadAllThemes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save custom theme:\n{ex.Message}", "Theme Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ApplyTheme(Window window, TextEditor editor, string themeName)
        {
            var theme = LoadedThemes.Find(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
            if (theme == null)
            {
                if (LoadedThemes.Count > 0)
                    theme = LoadedThemes[0];
                else
                    theme = new Theme();
            }

            CurrentTheme = theme;
            ApplyTheme(window, editor, theme);
        }

        public static void ApplyTheme(Window window, TextEditor editor, Theme theme)
        {
            try
            {
                var bgBrush = CreateBrush(theme.Background);
                var fgBrush = CreateBrush(theme.Foreground);
                var caretBrush = CreateBrush(theme.CaretColor);
                var selBgBrush = CreateBrush(theme.SelectionBackground, 0.5);
                var lineNumBrush = CreateBrush(theme.LineNumbersForeground);
                
                editor.Background = bgBrush;
                editor.Foreground = fgBrush;
                editor.TextArea.Caret.CaretBrush = caretBrush;
                editor.TextArea.SelectionBrush = selBgBrush;
                editor.LineNumbersForeground = lineNumBrush;

                RemoveCurrentLineHighlight(editor);
                if (!string.IsNullOrEmpty(theme.CurrentLineBackground))
                {
                    var curLineBrush = CreateBrush(theme.CurrentLineBackground);
                    var renderer = new CurrentLineHighlightRenderer(editor, curLineBrush);
                    editor.TextArea.TextView.BackgroundRenderers.Add(renderer);
                }

                window.Resources["ThemeBackground"] = bgBrush;
                window.Resources["ThemeForeground"] = fgBrush;
                window.Resources["ThemeMenuBackground"] = CreateBrush(theme.MenuBackground);
                window.Resources["ThemeMenuForeground"] = CreateBrush(theme.MenuForeground);
                window.Resources["ThemeMenuBorder"] = CreateBrush(theme.MenuBorder);
                window.Resources["ThemeStatusBarBackground"] = CreateBrush(theme.StatusBarBackground);
                window.Resources["ThemeStatusBarForeground"] = CreateBrush(theme.StatusBarForeground);

                ApplySyntaxColoring(editor, theme);

                editor.TextArea.TextView.Redraw();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private static void ApplySyntaxColoring(TextEditor editor, Theme theme)
        {
            var highlighting = editor.SyntaxHighlighting;
            if (highlighting == null || theme.Syntax == null) return;

            foreach (var color in highlighting.NamedHighlightingColors)
            {
                string colorName = color.Name.ToLower();

                string? targetHex = null;
                if ((colorName.Contains("comment") || colorName.Contains("comments")) && theme.Syntax.TryGetValue("comment", out string? cVal))
                {
                    targetHex = cVal;
                }
                else if ((colorName.Contains("keyword") || colorName.Contains("keywords") || colorName == "statement") && theme.Syntax.TryGetValue("keyword", out string? kVal))
                {
                    targetHex = kVal;
                }
                else if ((colorName.Contains("string") || colorName.Contains("strings") || colorName == "char") && theme.Syntax.TryGetValue("string", out string? sVal))
                {
                    targetHex = sVal;
                }
                else if ((colorName.Contains("number") || colorName.Contains("numbers") || colorName == "digits" || colorName == "digit") && theme.Syntax.TryGetValue("number", out string? nVal))
                {
                    targetHex = nVal;
                }
                else if ((colorName.Contains("type") || colorName.Contains("types") || colorName == "class" || colorName == "interface") && theme.Syntax.TryGetValue("type", out string? tVal))
                {
                    targetHex = tVal;
                }
                else if ((colorName.Contains("method") || colorName.Contains("function") || colorName == "call") && theme.Syntax.TryGetValue("method", out string? mVal))
                {
                    targetHex = mVal;
                }
                else if (colorName.Contains("tag") && theme.Syntax.TryGetValue("xmlTag", out string? tagVal))
                {
                    targetHex = tagVal;
                }
                else if (colorName.Contains("attribute") && theme.Syntax.TryGetValue("xmlAttribute", out string? attrVal))
                {
                    targetHex = attrVal;
                }
                else if (colorName.Contains("value") && theme.Syntax.TryGetValue("xmlValue", out string? valVal))
                {
                    targetHex = valVal;
                }

                if (targetHex != null)
                {
                    try
                    {
                        var colorValue = (Color)ColorConverter.ConvertFromString(targetHex);
                        color.Foreground = new SimpleHighlightingBrush(colorValue);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void RemoveCurrentLineHighlight(TextEditor editor)
        {
            for (int i = editor.TextArea.TextView.BackgroundRenderers.Count - 1; i >= 0; i--)
            {
                if (editor.TextArea.TextView.BackgroundRenderers[i] is CurrentLineHighlightRenderer)
                {
                    editor.TextArea.TextView.BackgroundRenderers.RemoveAt(i);
                }
            }
        }

        private static Brush CreateBrush(string hexColor, double opacity = 1.0)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var brush = new SolidColorBrush(color);
                if (Math.Abs(opacity - 1.0) > 0.01)
                {
                    brush.Opacity = opacity;
                }
                brush.Freeze();
                return brush;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        private List<Theme> GetDefaultThemes()
        {
            var list = new List<Theme>();

            list.Add(new Theme
            {
                Name = "Classic Light",
                Background = "#FFFFFF",
                Foreground = "#000000",
                CaretColor = "#000000",
                SelectionBackground = "#ADD6FF",
                SelectionForeground = "#000000",
                LineNumbersForeground = "#888888",
                CurrentLineBackground = "#F2F7FC",
                MenuBackground = "#F5F5F5",
                MenuForeground = "#000000",
                MenuBorder = "#E0E0E0",
                StatusBarBackground = "#007ACC",
                StatusBarForeground = "#FFFFFF",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#0000FF" },
                    { "comment", "#008000" },
                    { "string", "#A31515" },
                    { "number", "#098658" },
                    { "type", "#267F99" },
                    { "method", "#795E26" },
                    { "xmlTag", "#800000" },
                    { "xmlAttribute", "#FF0000" },
                    { "xmlValue", "#0000FF" }
                }
            });

            list.Add(new Theme
            {
                Name = "Classic Dark",
                Background = "#1E1E1E",
                Foreground = "#D4D4D4",
                CaretColor = "#FFFFFF",
                SelectionBackground = "#264F78",
                SelectionForeground = "#FFFFFF",
                LineNumbersForeground = "#858585",
                CurrentLineBackground = "#2A2A2A",
                MenuBackground = "#2D2D30",
                MenuForeground = "#F1F1F1",
                MenuBorder = "#3E3E40",
                StatusBarBackground = "#007ACC",
                StatusBarForeground = "#FFFFFF",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#569CD6" },
                    { "comment", "#6A9955" },
                    { "string", "#D69D85" },
                    { "number", "#B5CEA8" },
                    { "type", "#4EC9B0" },
                    { "method", "#DCDCAA" },
                    { "xmlTag", "#E06C75" },
                    { "xmlAttribute", "#D19A66" },
                    { "xmlValue", "#98C379" }
                }
            });

            list.Add(new Theme
            {
                Name = "Solarized Light",
                Background = "#FDF6E3",
                Foreground = "#657B83",
                CaretColor = "#586E75",
                SelectionBackground = "#EEE8D5",
                SelectionForeground = "#586E75",
                LineNumbersForeground = "#93A1A1",
                CurrentLineBackground = "#F5ECD5",
                MenuBackground = "#ECE4D0",
                MenuForeground = "#586E75",
                MenuBorder = "#D3C7A7",
                StatusBarBackground = "#073642",
                StatusBarForeground = "#EEE8D5",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#859900" },
                    { "comment", "#93A1A1" },
                    { "string", "#2AA198" },
                    { "number", "#D33682" },
                    { "type", "#B58900" },
                    { "method", "#268BD2" },
                    { "xmlTag", "#CB4B16" },
                    { "xmlAttribute", "#B58900" },
                    { "xmlValue", "#2AA198" }
                }
            });

            list.Add(new Theme
            {
                Name = "Solarized Dark",
                Background = "#002B36",
                Foreground = "#839496",
                CaretColor = "#93A1A1",
                SelectionBackground = "#073642",
                SelectionForeground = "#93A1A1",
                LineNumbersForeground = "#586E75",
                CurrentLineBackground = "#073A44",
                MenuBackground = "#00212B",
                MenuForeground = "#839496",
                MenuBorder = "#073642",
                StatusBarBackground = "#2AA198",
                StatusBarForeground = "#002B36",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#859900" },
                    { "comment", "#586E75" },
                    { "string", "#2AA198" },
                    { "number", "#D33682" },
                    { "type", "#B58900" },
                    { "method", "#268BD2" },
                    { "xmlTag", "#CB4B16" },
                    { "xmlAttribute", "#B58900" },
                    { "xmlValue", "#2AA198" }
                }
            });

            list.Add(new Theme
            {
                Name = "Monokai",
                Background = "#272822",
                Foreground = "#F8F8F2",
                CaretColor = "#F8F8F0",
                SelectionBackground = "#49483E",
                SelectionForeground = "#F8F8F2",
                LineNumbersForeground = "#90908A",
                CurrentLineBackground = "#3E3D32",
                MenuBackground = "#1E1F1C",
                MenuForeground = "#F8F8F2",
                MenuBorder = "#3E3D32",
                StatusBarBackground = "#A6E22E",
                StatusBarForeground = "#272822",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#F92672" },
                    { "comment", "#75715E" },
                    { "string", "#E6DB74" },
                    { "number", "#AE81FF" },
                    { "type", "#66D9EF" },
                    { "method", "#A6E22E" },
                    { "xmlTag", "#F92672" },
                    { "xmlAttribute", "#A6E22E" },
                    { "xmlValue", "#E6DB74" }
                }
            });

            list.Add(new Theme
            {
                Name = "Dracula",
                Background = "#282A36",
                Foreground = "#F8F8F2",
                CaretColor = "#F8F8F0",
                SelectionBackground = "#44475A",
                SelectionForeground = "#F8F8F2",
                LineNumbersForeground = "#6272A4",
                CurrentLineBackground = "#343746",
                MenuBackground = "#1E1F29",
                MenuForeground = "#F8F8F2",
                MenuBorder = "#44475A",
                StatusBarBackground = "#BD93F9",
                StatusBarForeground = "#282A36",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#FF79C6" },
                    { "comment", "#6272A4" },
                    { "string", "#F1FA8C" },
                    { "number", "#BD93F9" },
                    { "type", "#8BE9FD" },
                    { "method", "#50FA7B" },
                    { "xmlTag", "#FF79C6" },
                    { "xmlAttribute", "#50FA7B" },
                    { "xmlValue", "#F1FA8C" }
                }
            });

            list.Add(new Theme
            {
                Name = "Nord",
                Background = "#2E3440",
                Foreground = "#D8DEE9",
                CaretColor = "#D8DEE9",
                SelectionBackground = "#434C5E",
                SelectionForeground = "#E5E9F0",
                LineNumbersForeground = "#4C566A",
                CurrentLineBackground = "#3B4252",
                MenuBackground = "#242933",
                MenuForeground = "#D8DEE9",
                MenuBorder = "#3B4252",
                StatusBarBackground = "#88C0D0",
                StatusBarForeground = "#2E3440",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#81A1C1" },
                    { "comment", "#4C566A" },
                    { "string", "#A3BE8C" },
                    { "number", "#B48EAD" },
                    { "type", "#8FBCBB" },
                    { "method", "#88C0D0" },
                    { "xmlTag", "#81A1C1" },
                    { "xmlAttribute", "#8FBCBB" },
                    { "xmlValue", "#A3BE8C" }
                }
            });

            list.Add(new Theme
            {
                Name = "Gruvbox Dark",
                Background = "#282828",
                Foreground = "#EBDBB2",
                CaretColor = "#FBF1C7",
                SelectionBackground = "#504945",
                SelectionForeground = "#EBDBB2",
                LineNumbersForeground = "#7C6F64",
                CurrentLineBackground = "#32302F",
                MenuBackground = "#1D2021",
                MenuForeground = "#EBDBB2",
                MenuBorder = "#3C3836",
                StatusBarBackground = "#D79921",
                StatusBarForeground = "#282828",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#FB4934" },
                    { "comment", "#928374" },
                    { "string", "#B8BB26" },
                    { "number", "#D3869B" },
                    { "type", "#FABD2F" },
                    { "method", "#B8BB26" },
                    { "xmlTag", "#FB4934" },
                    { "xmlAttribute", "#FABD2F" },
                    { "xmlValue", "#B8BB26" }
                }
            });

            list.Add(new Theme
            {
                Name = "Cyberpunk",
                Background = "#180024",
                Foreground = "#00FF66",
                CaretColor = "#FF007F",
                SelectionBackground = "#FF0055",
                SelectionForeground = "#FFFFFF",
                LineNumbersForeground = "#6B0099",
                CurrentLineBackground = "#2D003B",
                MenuBackground = "#100018",
                MenuForeground = "#00FF66",
                MenuBorder = "#FF007F",
                StatusBarBackground = "#FF007F",
                StatusBarForeground = "#180024",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#FF007F" },
                    { "comment", "#00FFFF" },
                    { "string", "#FFFF00" },
                    { "number", "#FF9900" },
                    { "type", "#00FFFF" },
                    { "method", "#00FF66" },
                    { "xmlTag", "#FF007F" },
                    { "xmlAttribute", "#00FFFF" },
                    { "xmlValue", "#FFFF00" }
                }
            });

            list.Add(new Theme
            {
                Name = "Sepia",
                Background = "#F4ECD8",
                Foreground = "#5B4636",
                CaretColor = "#5B4636",
                SelectionBackground = "#E4D9C0",
                SelectionForeground = "#5B4636",
                LineNumbersForeground = "#A18F7B",
                CurrentLineBackground = "#EBE2CD",
                MenuBackground = "#E9DEC6",
                MenuForeground = "#5B4636",
                MenuBorder = "#D7CBB4",
                StatusBarBackground = "#704214",
                StatusBarForeground = "#FDFDFD",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#A52A2A" },
                    { "comment", "#8B8682" },
                    { "string", "#2E8B57" },
                    { "number", "#8B4513" },
                    { "type", "#4A708B" },
                    { "method", "#8B4500" },
                    { "xmlTag", "#A52A2A" },
                    { "xmlAttribute", "#4A708B" },
                    { "xmlValue", "#2E8B57" }
                }
            });

            list.Add(new Theme
            {
                Name = "Custom",
                Background = "#22252A",
                Foreground = "#ECEFF4",
                CaretColor = "#E5E9F0",
                SelectionBackground = "#4C566A",
                SelectionForeground = "#ECEFF4",
                LineNumbersForeground = "#4C566A",
                CurrentLineBackground = "#2E3440",
                MenuBackground = "#1B1C1F",
                MenuForeground = "#ECEFF4",
                MenuBorder = "#2E3440",
                StatusBarBackground = "#5E81AC",
                StatusBarForeground = "#ECEFF4",
                Syntax = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "keyword", "#81A1C1" },
                    { "comment", "#6B7280" },
                    { "string", "#A3BE8C" },
                    { "number", "#B48EAD" },
                    { "type", "#8FBCBB" },
                    { "method", "#88C0D0" },
                    { "xmlTag", "#81A1C1" },
                    { "xmlAttribute", "#8FBCBB" },
                    { "xmlValue", "#A3BE8C" }
                }
            });

            return list;
        }
    }

    public class CurrentLineHighlightRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly Brush _brush;

        public KnownLayer Layer => KnownLayer.Background;

        public CurrentLineHighlightRenderer(TextEditor editor, Brush brush)
        {
            _editor = editor;
            _brush = brush;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_editor.Document == null) return;
            
            textView.EnsureVisualLines();
            var currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset);
            foreach (var visualLine in textView.VisualLines)
            {
                if (visualLine.FirstDocumentLine.LineNumber == currentLine.LineNumber)
                {
                    foreach (var textLine in visualLine.TextLines)
                    {
                        var rect = BackgroundGeometryBuilder.GetRectsForSegment(textView, new ICSharpCode.AvalonEdit.Document.TextSegment
                        {
                            StartOffset = currentLine.Offset,
                            Length = currentLine.Length
                        });

                        foreach (var r in rect)
                        {
                            var fullWidthRect = new Rect(0, r.Y, textView.ActualWidth, r.Height);
                            drawingContext.DrawRectangle(_brush, null, fullWidthRect);
                        }
                    }
                }
            }
        }
    }
}
