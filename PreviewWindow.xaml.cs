using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Markdig;

namespace WinTextEdit
{
    public partial class PreviewWindow : Window
    {
        private readonly string _documentText;
        private readonly string _language;
        private readonly Theme _theme;
        private bool _isLoaded;

        public PreviewWindow(Window owner, string documentText, string language, Theme theme)
        {
            InitializeComponent();
            Owner = owner;
            _documentText = documentText;
            _language = language;
            _theme = theme;

            Loaded += PreviewWindow_Loaded;
        }

        private void PreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            SetInitialPreviewMode();
        }

        private void SetInitialPreviewMode()
        {
            string mode = "Raw Code";

            if (_language.Equals("HTML", StringComparison.OrdinalIgnoreCase))
            {
                mode = "HTML";
            }
            else if (_language.Equals("MarkDown", StringComparison.OrdinalIgnoreCase))
            {
                mode = "Markdown";
            }
            else if (_language.Equals("CSS", StringComparison.OrdinalIgnoreCase))
            {
                mode = "CSS";
            }

            foreach (ComboBoxItem item in PreviewModeComboBox.Items)
            {
                if (item.Content.ToString()?.Equals(mode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    PreviewModeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void PreviewModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || PreviewModeComboBox.SelectedItem == null) return;

            string selectedMode = (PreviewModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Raw Code";
            RenderPreview(selectedMode);
        }

        private void RenderPreview(string mode)
        {
            string htmlContent = "";

            if (mode.Equals("HTML", StringComparison.OrdinalIgnoreCase))
            {
                string styleOverride = $@"
                <style>
                body {{
                    background-color: {_theme.Background} !important;
                    color: {_theme.Foreground} !important;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                }}
                </style>";

                if (_documentText.Contains("<body", StringComparison.OrdinalIgnoreCase))
                {
                    if (_documentText.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                    {
                        htmlContent = _documentText.Replace("</head>", styleOverride + "</head>");
                    }
                    else
                    {
                        htmlContent = _documentText.Replace("<body", "<body" + styleOverride);
                    }
                }
                else
                {
                    htmlContent = $"<html><head>{styleOverride}</head><body>{_documentText}</body></html>";
                }
            }
            else if (mode.Equals("Markdown", StringComparison.OrdinalIgnoreCase))
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string mdHtml = Markdown.ToHtml(_documentText, pipeline);

                htmlContent = $@"
                <html>
                <head>
                <style>
                body {{
                    background-color: {_theme.Background};
                    color: {_theme.Foreground};
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                    padding: 25px;
                    line-height: 1.6;
                    max-width: 800px;
                    margin: 0 auto;
                }}
                h1, h2, h3, h4, h5, h6 {{
                    color: {_theme.Foreground};
                    border-bottom: 1px solid {_theme.MenuBorder};
                    padding-bottom: 6px;
                    margin-top: 24px;
                    margin-bottom: 16px;
                }}
                a {{
                    color: {_theme.StatusBarBackground};
                    text-decoration: none;
                }}
                a:hover {{
                    text-decoration: underline;
                }}
                hr {{
                    border: 0;
                    border-top: 1px solid {_theme.MenuBorder};
                    margin: 20px 0;
                }}
                pre {{
                    background-color: {_theme.MenuBackground};
                    border: 1px solid {_theme.MenuBorder};
                    padding: 12px;
                    border-radius: 6px;
                    overflow-x: auto;
                }}
                code {{
                    font-family: Consolas, Monaco, monospace;
                    background-color: {_theme.MenuBackground};
                    padding: 2px 4px;
                    border-radius: 3px;
                }}
                pre code {{
                    padding: 0;
                    background-color: transparent;
                }}
                blockquote {{
                    border-left: 4px solid {_theme.StatusBarBackground};
                    padding: 0 15px;
                    color: {_theme.Foreground};
                    opacity: 0.8;
                    margin: 0 0 16px 0;
                }}
                table {{
                    border-collapse: collapse;
                    width: 100%;
                    margin-bottom: 16px;
                }}
                th, td {{
                    border: 1px solid {_theme.MenuBorder};
                    padding: 8px 13px;
                }}
                th {{
                    background-color: {_theme.MenuBackground};
                    font-weight: bold;
                }}
                </style>
                </head>
                <body>
                    {mdHtml}
                </body>
                </html>";
            }
            else if (mode.Equals("CSS", StringComparison.OrdinalIgnoreCase))
            {
                htmlContent = $@"
                <html>
                <head>
                <style>
                body {{
                    background-color: {_theme.Background};
                    color: {_theme.Foreground};
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif;
                    padding: 25px;
                    line-height: 1.5;
                }}
                hr {{
                    border: 0;
                    border-top: 1px solid {_theme.MenuBorder};
                    margin: 20px 0;
                }}
                .btn {{
                    padding: 6px 12px;
                    border-radius: 4px;
                    cursor: pointer;
                }}
                .btn-primary {{
                    background-color: {_theme.StatusBarBackground};
                    color: {_theme.StatusBarForeground};
                    border: none;
                }}
                .badge {{
                    background-color: {_theme.MenuBorder};
                    padding: 2px 6px;
                    border-radius: 10px;
                    font-size: 11px;
                }}
                
                {_documentText}
                </style>
                </head>
                <body>
                    <h1>CSS Stylesheet Preview</h1>
                    <p>This is a live preview showing how your custom CSS styles common HTML tags in this theme.</p>
                    <hr/>
                    <h2>Headings</h2>
                    <h1>Heading 1 (h1)</h1>
                    <h2>Heading 2 (h2)</h2>
                    <h3>Heading 3 (h3)</h3>
                    <hr/>
                    <h2>Paragraphs &amp; Text</h2>
                    <p>This is standard paragraph text (p). It is commonly styled with line-height and font size.</p>
                    <p>Here is some <strong>strong</strong>, <em>emphasized</em>, and <a href='#'>hyperlinked text</a>.</p>
                    <hr/>
                    <h2>Components</h2>
                    <button class='btn btn-primary'>Primary Button</button>
                    <button class='btn'>Secondary Button</button>
                    <hr/>
                    <h2>Lists</h2>
                    <ul>
                        <li>First list item</li>
                        <li>Second list item with <span class='badge'>Badge</span></li>
                    </ul>
                </body>
                </html>";
            }
            else
            {
                string escaped = WebUtility.HtmlEncode(_documentText);
                htmlContent = $@"
                <html>
                <head>
                <style>
                body {{
                    background-color: {_theme.Background};
                    color: {_theme.Foreground};
                    font-family: Consolas, Monaco, 'Andale Mono', monospace;
                    font-size: 13px;
                    padding: 20px;
                    margin: 0;
                }}
                pre {{
                    white-space: pre-wrap;
                    word-wrap: break-word;
                    margin: 0;
                }}
                </style>
                </head>
                <body>
                    <pre><code>{escaped}</code></pre>
                </body>
                </html>";
            }

            try
            {
                PreviewBrowser.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write preview: {ex.Message}");
            }
        }
    }
}
