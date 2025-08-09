# Changelog

All notable changes to Batchbrake will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of Batchbrake
- Batch video conversion using HandBrakeCLI
- Drag and drop interface for adding videos
- Support for 90+ built-in HandBrake presets
- Custom preset support via JSON import
- Parallel video processing capabilities
- Queue management with add, remove, and reorder functionality
- Real-time progress tracking for conversions
- Session management (auto-save and restore)
- HandBrake settings configuration UI
- FFmpeg settings configuration UI
- Application preferences management
- Cross-platform support (Windows, macOS, Linux)
- About window with version information
- Detailed logging with timestamps
- Support for multiple output formats (MP4, MKV, WebM)
- Hardware acceleration support
- Custom preset file management with multiple file support
- File removal bug fix for custom preset management
- Direct JSON parsing for custom HandBrake presets
- Fixed preset loading on application startup

### Changed
- Improved preset loading to always include custom presets
- Enhanced debug logging for troubleshooting
- Better error handling for file operations

### Fixed
- Custom preset files not loading from JSON files
- Unable to remove JSON files from preset list
- Custom presets not appearing on initial application startup
- Preset loading race condition on startup

## [1.0.0] - TBD

### Added
- First stable release
- Complete feature set for batch video conversion
- Comprehensive documentation and README
- GitHub Actions workflow for automated releases
- Multi-architecture support (x64, x86, ARM, ARM64)

### Notes
- Requires HandBrakeCLI to be installed separately
- FFmpeg is optional but recommended for enhanced metadata extraction

---

## Version History Guidelines

### Version Numbering
- MAJOR version: Incompatible API changes
- MINOR version: Add functionality in a backward compatible manner  
- PATCH version: Backward compatible bug fixes

### Categories
- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Vulnerability fixes