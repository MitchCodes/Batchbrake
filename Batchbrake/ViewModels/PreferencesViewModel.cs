using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using Batchbrake.Models;

namespace Batchbrake.ViewModels
{
    public class PreferencesViewModel : ViewModelBase
    {
        private readonly Preferences _originalPreferences;
        private readonly Action<Preferences>? _onSave;
        private readonly Action? _onCancel;

        public PreferencesViewModel(Preferences preferences, Action<Preferences>? onSave = null, Action? onCancel = null)
        {
            _originalPreferences = preferences;
            _onSave = onSave;
            _onCancel = onCancel;

            // Copy values from the original preferences
            DefaultParallelInstances = preferences.DefaultParallelInstances;
            DefaultOutputFormat = preferences.DefaultOutputFormat;
            DefaultOutputPath = preferences.DefaultOutputPath;
            DeleteSourceAfterConversion = preferences.DeleteSourceAfterConversion;
            AutoSaveSession = preferences.AutoSaveSession;
            AutoSaveIntervalMinutes = preferences.AutoSaveIntervalMinutes;
            ShowCompletionNotifications = preferences.ShowCompletionNotifications;
            MinimizeToTray = preferences.MinimizeToTray;
            AutoStartConversions = preferences.AutoStartConversions;
            LogVerbosity = preferences.LogVerbosity;
            MaxLogLines = preferences.MaxLogLines;
            RememberWindowState = preferences.RememberWindowState;
            SupportedVideoExtensionsText = string.Join(", ", preferences.SupportedVideoExtensions);

            // Commands
            SaveCommand = ReactiveCommand.Create(Save);
            CancelCommand = ReactiveCommand.Create(Cancel);
            RestoreDefaultsCommand = ReactiveCommand.Create(RestoreDefaults);
        }

        #region Properties

        private int _defaultParallelInstances;
        public int DefaultParallelInstances
        {
            get => _defaultParallelInstances;
            set => this.RaiseAndSetIfChanged(ref _defaultParallelInstances, value);
        }

        private string _defaultOutputFormat = "mp4";
        public string DefaultOutputFormat
        {
            get => _defaultOutputFormat;
            set => this.RaiseAndSetIfChanged(ref _defaultOutputFormat, value);
        }

        private string _defaultOutputPath = "$(Folder)\\$(FileName)_converted.$(Ext)";
        public string DefaultOutputPath
        {
            get => _defaultOutputPath;
            set => this.RaiseAndSetIfChanged(ref _defaultOutputPath, value);
        }

        private bool _deleteSourceAfterConversion;
        public bool DeleteSourceAfterConversion
        {
            get => _deleteSourceAfterConversion;
            set => this.RaiseAndSetIfChanged(ref _deleteSourceAfterConversion, value);
        }

        private bool _autoSaveSession;
        public bool AutoSaveSession
        {
            get => _autoSaveSession;
            set => this.RaiseAndSetIfChanged(ref _autoSaveSession, value);
        }

        private int _autoSaveIntervalMinutes;
        public int AutoSaveIntervalMinutes
        {
            get => _autoSaveIntervalMinutes;
            set => this.RaiseAndSetIfChanged(ref _autoSaveIntervalMinutes, value);
        }

        private bool _showCompletionNotifications;
        public bool ShowCompletionNotifications
        {
            get => _showCompletionNotifications;
            set => this.RaiseAndSetIfChanged(ref _showCompletionNotifications, value);
        }

        private bool _minimizeToTray;
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => this.RaiseAndSetIfChanged(ref _minimizeToTray, value);
        }

        private bool _autoStartConversions;
        public bool AutoStartConversions
        {
            get => _autoStartConversions;
            set => this.RaiseAndSetIfChanged(ref _autoStartConversions, value);
        }

        private int _logVerbosity;
        public int LogVerbosity
        {
            get => _logVerbosity;
            set => this.RaiseAndSetIfChanged(ref _logVerbosity, value);
        }

        private int _maxLogLines;
        public int MaxLogLines
        {
            get => _maxLogLines;
            set => this.RaiseAndSetIfChanged(ref _maxLogLines, value);
        }

        private bool _rememberWindowState;
        public bool RememberWindowState
        {
            get => _rememberWindowState;
            set => this.RaiseAndSetIfChanged(ref _rememberWindowState, value);
        }

        private string _supportedVideoExtensionsText = "";
        public string SupportedVideoExtensionsText
        {
            get => _supportedVideoExtensionsText;
            set => this.RaiseAndSetIfChanged(ref _supportedVideoExtensionsText, value);
        }

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> RestoreDefaultsCommand { get; }

        #endregion

        #region Methods

        private void Save()
        {
            try
            {
                // Update the original preferences object
                _originalPreferences.DefaultParallelInstances = DefaultParallelInstances;
                _originalPreferences.DefaultOutputFormat = DefaultOutputFormat;
                _originalPreferences.DefaultOutputPath = DefaultOutputPath;
                _originalPreferences.DeleteSourceAfterConversion = DeleteSourceAfterConversion;
                _originalPreferences.AutoSaveSession = AutoSaveSession;
                _originalPreferences.AutoSaveIntervalMinutes = AutoSaveIntervalMinutes;
                _originalPreferences.ShowCompletionNotifications = ShowCompletionNotifications;
                _originalPreferences.MinimizeToTray = MinimizeToTray;
                _originalPreferences.AutoStartConversions = AutoStartConversions;
                _originalPreferences.LogVerbosity = LogVerbosity;
                _originalPreferences.MaxLogLines = MaxLogLines;
                _originalPreferences.RememberWindowState = RememberWindowState;

                // Parse supported video extensions
                var extensions = SupportedVideoExtensionsText
                    .Split(',')
                    .Select(ext => ext.Trim())
                    .Where(ext => !string.IsNullOrEmpty(ext))
                    .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                    .Distinct()
                    .ToList();

                _originalPreferences.SupportedVideoExtensions = extensions;

                _onSave?.Invoke(_originalPreferences);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the dialog
                System.Diagnostics.Debug.WriteLine($"Error saving preferences: {ex.Message}");
            }
        }

        private void Cancel()
        {
            _onCancel?.Invoke();
        }

        private void RestoreDefaults()
        {
            var defaults = new Preferences();
            
            DefaultParallelInstances = defaults.DefaultParallelInstances;
            DefaultOutputFormat = defaults.DefaultOutputFormat;
            DefaultOutputPath = defaults.DefaultOutputPath;
            DeleteSourceAfterConversion = defaults.DeleteSourceAfterConversion;
            AutoSaveSession = defaults.AutoSaveSession;
            AutoSaveIntervalMinutes = defaults.AutoSaveIntervalMinutes;
            ShowCompletionNotifications = defaults.ShowCompletionNotifications;
            MinimizeToTray = defaults.MinimizeToTray;
            AutoStartConversions = defaults.AutoStartConversions;
            LogVerbosity = defaults.LogVerbosity;
            MaxLogLines = defaults.MaxLogLines;
            RememberWindowState = defaults.RememberWindowState;
            SupportedVideoExtensionsText = string.Join(", ", defaults.SupportedVideoExtensions);
        }

        #endregion
    }
}