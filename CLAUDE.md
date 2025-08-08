# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Batchbrake is an Avalonia-based desktop application for batch video conversion using HandBrakeCLI. It provides a cross-platform GUI for managing video encoding queues with drag-and-drop functionality and preset management.

## Development Commands

### Building
```bash
dotnet build Batchbrake.sln
dotnet build Batchbrake.sln --configuration Release
```

### Running
```bash
dotnet run --project Batchbrake\Batchbrake.csproj
```

### Testing
```bash
dotnet test Batchbrake.Tests\Batchbrake.Tests.csproj
dotnet test  # Run all tests in solution
```

## Architecture

### Core Components

- **MainWindow/MainWindowViewModel**: Primary application interface with drag-and-drop video file support and conversion queue management
- **VideoModelViewModel**: Represents individual videos in the conversion queue with properties for input/output paths, presets, and conversion status
- **HandbrakeCLIWrapper**: Manages HandBrakeCLI process execution for video conversion and preset retrieval
- **FFmpegWrapper**: Extracts video metadata (duration, resolution, codec) from input files

### Key Patterns

- **MVVM Architecture**: Uses ReactiveUI for view models with proper property change notifications
- **Dependency Injection**: Services like `IFilePickerService` are injected into view models
- **External Tool Integration**: Wraps command-line tools (HandBrakeCLI, FFmpeg) with proper process management and error handling
- **Async Operations**: Video processing and file operations use async/await patterns

### Project Structure

- `Batchbrake/` - Main application project (Avalonia UI)
- `Batchbrake.Tests/` - XUnit test project with Moq for mocking
- `ViewModels/` - MVVM view models using ReactiveUI
- `Models/` - Data models (VideoInfoModel, Session)
- `Utilities/` - External tool wrappers (HandBrakeCLI, FFmpeg)
- `Services/` - Application services (FilePickerService)
- `Converters/` - Avalonia value converters for UI binding

### External Dependencies

The application requires HandBrakeCLI and FFmpeg executables to be available in the system PATH or specified paths. Video conversion functionality depends on these external tools being properly configured.