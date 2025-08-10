# Batchbrake

A Windows desktop application for batch video conversion using HandBrakeCLI. Built with Avalonia UI and .NET 8, Batchbrake provides an intuitive drag-and-drop interface for managing video encoding queues with preset management and parallel processing capabilities. While the application is built on cross-platform technology, official releases are provided for Windows only.

## Features

### Core Functionality
- **Batch Video Conversion**: Process multiple videos simultaneously with parallel conversion support
- **Drag & Drop Interface**: Simply drag video files into the application to add them to the conversion queue
- **Queue Management**: Add, remove, reorder, and modify videos in the conversion queue
- **Real-time Progress Tracking**: Monitor conversion progress with detailed status updates
- **Session Management**: Automatically save and restore your work sessions

### HandBrake Integration
- **Built-in Preset Support**: Access all HandBrake's built-in presets (90+ presets across 7 categories)
- **Custom Preset Support**: Import and use your own HandBrake presets from JSON files
- **Preset Categories**: Organized presets including General, Web, Devices, Matroska, Hardware, Professional, and CLI Defaults
- **Advanced Settings**: Full access to HandBrake's encoding options including quality settings, codecs, and containers

### Video Processing
- **Multiple Format Support**: Input support for MP4, MKV, AVI, MOV, and more
- **Output Format Options**: MP4, MKV, WebM output formats
- **Quality Control**: Constant Quality (CRF) and average bitrate encoding options
- **Hardware Acceleration**: Support for hardware-accelerated encoding (NVENC, QSV, etc.)
- **Audio & Subtitle Preservation**: Maintain multiple audio tracks and subtitle streams

### User Interface
- **Windows Native**: Optimized for Windows 10/11
- **Modern UI**: Clean, responsive interface built with Avalonia UI
- **Real-time Logging**: Detailed conversion logs with timestamps
- **Settings Management**: Persistent application settings and preferences
- **About Window**: Version information and system details

## System Requirements

### Minimum Requirements
- **OS**: Windows 10 or later (64-bit, 32-bit, or ARM64)
- **RAM**: 4 GB RAM
- **Storage**: 100 MB for application + space for video processing
- **.NET**: .NET 8 Runtime (automatically included with releases)

### Required Dependencies
- **HandBrakeCLI**: Required for video conversion
  - Download from [HandBrake.fr](https://handbrake.fr/downloads.php)
  - Or install via package managers like Chocolatey: `choco install handbrake-cli`
- **FFmpeg** (Optional): For enhanced video metadata extraction
  - Download from [FFmpeg.org](https://ffmpeg.org/download.html)
  - Or install via Chocolatey: `choco install ffmpeg`

## Installation

### Windows
1. Download the latest release from the [Releases page](https://github.com/yourusername/batchbrake/releases)
   - `Batchbrake-Windows-win-x64.zip` for 64-bit systems (recommended)
   - `Batchbrake-Windows-win-x86.zip` for 32-bit systems
   - `Batchbrake-Windows-win-arm64.zip` for ARM64 systems
2. Extract the ZIP file to your desired location
3. Run `Batchbrake.exe`
4. Install HandBrakeCLI if not already present

### Build from Source

**Note**: While official releases are Windows-only, the source code can still be built and run on Windows, macOS, and Linux due to the cross-platform nature of .NET and Avalonia UI.

```bash
git clone https://github.com/yourusername/batchbrake.git
cd batchbrake
dotnet restore
dotnet build
dotnet run --project Batchbrake/Batchbrake.csproj
```

For other platforms (macOS/Linux), you'll need to:
- Install .NET 8 SDK
- Install HandBrakeCLI and FFmpeg via your platform's package manager
- Build and run from source as shown above

## Quick Start Guide

### Basic Usage
1. **Launch Batchbrake**
2. **Add Videos**: Drag & drop video files or use File → Add Videos
3. **Select Preset**: Choose from 90+ built-in HandBrake presets
4. **Set Output**: Choose output directory and format
5. **Start Conversion**: Click "Start All" to begin batch processing

### First-Time Setup
1. **Configure HandBrakeCLI Path**:
   - Go to Tools → HandBrake Settings
   - Browse to HandBrakeCLI executable location
   - Test the connection

2. **Optional FFmpeg Setup**:
   - Go to Tools → FFmpeg Settings
   - Configure FFmpeg and FFprobe paths for enhanced metadata

### Adding Custom Presets
1. **Create Preset in HandBrake GUI**:
   - Configure your desired settings
   - Save as preset
   - Export preset to JSON file

2. **Import to Batchbrake**:
   - Tools → HandBrake Settings → Custom Presets
   - Browse and add your JSON preset file
   - Preset will appear in dropdown with built-in presets

## Configuration

### HandBrake Settings
Access via Tools → HandBrake Settings:

- **CLI Path**: Path to HandBrakeCLI executable
- **Default Preset**: Preset applied to new videos
- **Quality Settings**: RF quality value (0-51, lower = higher quality)
- **Video Encoder**: x264, x265, NVENC, etc.
- **Audio Settings**: Encoder, bitrate, mixdown options
- **Custom Presets**: Import JSON preset files

### Application Preferences
Access via Tools → Preferences:

- **Output Directory**: Default location for converted files
- **Parallel Conversions**: Number of simultaneous conversions
- **Auto-start**: Begin conversions automatically when added
- **Notifications**: System notifications for completion
- **Log Level**: Verbosity of conversion logs

### Advanced Configuration
Configuration files are stored in:
- **Windows**: `%APPDATA%\Batchbrake\`

Files include:
- `handbrake-settings.json`: HandBrake configuration
- `ffmpeg-settings.json`: FFmpeg configuration  
- `preferences.json`: Application preferences
- `session.json`: Current work session

## Usage Examples

### Batch Convert to MP4
1. Drag multiple video files into Batchbrake
2. Select "Fast 1080p30" preset
3. Set output format to "MP4"
4. Click "Start All"

### Custom Quality Encoding
1. Tools → HandBrake Settings
2. Set Quality (RF) to 18 (high quality)
3. Choose x265 encoder for smaller files
4. Apply settings and convert

### Preserve All Audio Tracks
1. Create custom preset in HandBrake:
   - Audio tab → "Add All Remaining"
   - Set codecs to "Auto Passthru"
2. Export preset and import to Batchbrake
3. Use preset for conversions

### Hardware Acceleration
1. Tools → HandBrake Settings
2. Video Encoder → Select NVENC or QSV
3. Adjust quality settings as needed
4. Convert with GPU acceleration

## Troubleshooting

### Common Issues

**HandBrakeCLI Not Found**
- Ensure HandBrakeCLI is installed and accessible
- Check path in Tools → HandBrake Settings
- Add HandBrakeCLI to your system PATH or specify full path

**Conversion Fails**
- Check input file integrity
- Verify sufficient disk space
- Review conversion logs for error details
- Try different preset or settings

**Poor Performance**
- Reduce parallel conversion count
- Close other resource-intensive applications
- Consider hardware acceleration options
- Check available RAM and CPU usage

**Custom Presets Not Loading**
- Verify JSON file format (export from HandBrake GUI)
- Check file permissions and accessibility
- Review preset file path in settings

### Getting Help
- Check the [Issues page](https://github.com/yourusername/batchbrake/issues) for known problems
- Submit bug reports with:
  - Operating system and version
  - Batchbrake version
  - Detailed error description
  - Log files if available

## Development

### Architecture
- **Framework**: .NET 8 with Avalonia UI
- **Pattern**: MVVM with ReactiveUI
- **Testing**: xUnit with Moq
- **External Tools**: HandBrakeCLI, FFmpeg integration

### Project Structure
```
Batchbrake/
├── Batchbrake/              # Main application
│   ├── ViewModels/          # MVVM view models
│   ├── Models/              # Data models
│   ├── Utilities/           # External tool wrappers
│   ├── Services/            # Application services
│   └── Converters/          # UI value converters
├── Batchbrake.Tests/        # Unit tests
└── README.md
```

### Building
```bash
# Development build
dotnet build

# Release build
dotnet build --configuration Release

# Run tests
dotnet test

# Create Windows deployment packages
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r win-x86 --self-contained
dotnet publish -c Release -r win-arm64 --self-contained
```

### Contributing
1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [HandBrake](https://handbrake.fr/) - The excellent video transcoder that powers conversions
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform .NET UI framework
- [FFmpeg](https://ffmpeg.org/) - Multimedia framework for metadata extraction
- [ReactiveUI](https://www.reactiveui.net/) - Reactive MVVM framework

## Changelog

### Version 1.0.0
- Initial release with batch conversion support
- Drag & drop interface
- Built-in and custom preset support
- Parallel processing capabilities
- Session management
- Windows support (x64, x86, ARM64)