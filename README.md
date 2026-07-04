# WinTextEdit

A modern, native text and code editor built for Windows. Combining the look and feel of the native OS with power-user features, WinTextEdit features robust multi-language syntax highlighting, a live markup previewer, registry-based shell integration, and custom theme rendering.

<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/3e654b62-2986-4662-82d6-902342a40e9c" />

## Key Features

- **Fluent Design Aesthetics**: Sleek, borderless interface utilizing native Windows elements, custom-styled Fluent scrollbars, and dynamic control color templates.
- **Syntax Highlighting**: Real-time syntax coloring for C#, C++, Python, JavaScript, TypeScript, HTML, XML, CSS, JSON, SQL, Java, and PHP (powered by AvalonEdit).
- **Live Preview Window**: Open a side-by-side rendering container (`Ctrl + P`) supporting:
  - **HTML**: Rendered structure with dynamic theme matching.
  - **Markdown**: HTML conversion powered by `Markdig`.
  - **CSS**: Inject custom CSS rules on top of sample widgets (headings, buttons, lists, badges) to test styling changes in real time.
  - **Raw Code**: Escaped plain text rendering.
- **Dynamic Themes**: Comes with 10 pre-loaded dark/light profiles (Dracula, Nord, Monokai, Solarized, Sepia, Cyberpunk, and more) plus a customizable `custom.yaml` file that updates styling instantly upon saving.
- **Session Persistence**: Theme selections, custom font families, and sizes are saved automatically to `settings.json` and restored on application startup.
- **Registry Integration**: Toggle file associations (`.txt`, `.log`, `.md`, `.json`, `.yaml`, `.ini`) and "Open with WinTextEdit" context menus. Operates under standard user privileges via `HKEY_CURRENT_USER` (no Administrator elevation required!).
- **Keyboard Shortcuts**:
  - `Ctrl + N`: New File
  - `Ctrl + O`: Open File
  - `Ctrl + S`: Save
  - `Ctrl + Shift + S`: Save As
  - `Ctrl + P`: Show Preview Window
  - `Ctrl + Mouse Wheel` / `Ctrl + + / -`: Zoom editor font size
  - `Ctrl + 0`: Reset zoom level

## Themes

<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/558f6f14-e9bf-4e9c-b1b2-e24b517711f0" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/56cd7905-f8e1-42c4-8688-fd56e9d0fa8a" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/efff3795-07d7-4cd2-9a75-966c4d8992b4" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/1708a750-0b6c-4249-a19a-2faf61924606" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/0745ca46-177a-4ebc-9616-ce3c0a5af978" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/67ada2ae-013a-46da-ae93-a07713b8a3c2" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/fe828f4a-13d3-4a72-b626-5fbe7a84b727" />
<img width="986" height="678" alt="image" src="https://github.com/user-attachments/assets/cc27460e-8477-4587-9ce6-a966e671396f" />

## Requirements

- **Operating System**: Windows 10 / 11
- **Framework**: .NET 8.0 SDK (Desktop development pack)

## Building from Source

The repository contains a build script (`build.cmd`) to clean build assets and bundle the application into a single portable folder:

1. Clone the repository.
2. Run the build script:
   ```cmd
   .\build.cmd dist
   ```
3. Find the compiled binaries inside the `.\dist` directory.

## License

This project is open-source under the MIT License.
