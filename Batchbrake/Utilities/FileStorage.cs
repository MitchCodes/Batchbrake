using System;
using System.IO;
using System.Text.Json;

namespace Batchbrake.Utilities
{
    /// <summary>
    /// Provides an interface for loading and saving objects to a file.
    /// </summary>
    /// <typeparam name="T">The type of object to be loaded and saved.</typeparam>
    public interface IFileStorage<T>
    {
        /// <summary>
        /// Saves the specified object to a file.
        /// </summary>
        /// <param name="data">The object to save.</param>
        /// <param name="fileName">The name of the file.</param>
        void Save(T data, string fileName);

        /// <summary>
        /// Loads an object from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to load from.</param>
        /// <returns>The loaded object.</returns>
        T? Load(string fileName);
    }

    /// <summary>
    /// Provides functionality for saving and loading objects as JSON files
    /// in the user's application data directory.
    /// </summary>
    /// <typeparam name="T">The type of object to be saved and loaded.</typeparam>
    public class JsonFileStorage<T> : IFileStorage<T>
    {
        private readonly string _appDataDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFileStorage{T}"/> class.
        /// </summary>
        /// <param name="appName">The name of the application, used to create a subdirectory in the application data folder.</param>
        public JsonFileStorage(string appName)
        {
            _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);

            if (!Directory.Exists(_appDataDirectory))
            {
                Directory.CreateDirectory(_appDataDirectory);
            }
        }

        /// <summary>
        /// Saves the specified object as a JSON file in the application's data directory.
        /// </summary>
        /// <param name="data">The object to save.</param>
        /// <param name="fileName">The name of the file.</param>
        public void Save(T data, string fileName)
        {
            string filePath = Path.Combine(_appDataDirectory, fileName);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads an object from the specified JSON file in the application's data directory.
        /// </summary>
        /// <param name="fileName">The name of the file to load from.</param>
        /// <returns>The loaded object.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        public T? Load(string fileName)
        {
            string filePath = Path.Combine(_appDataDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' was not found.");
            }

            var json = File.ReadAllText(filePath);

            if (json == null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
