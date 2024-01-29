using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace devector
{
    internal class RecentFileManager
    {
        private const int MaxRecentFiles = 5; // Maximum number of recent files to keep
        private List<string> recentFiles = new List<string>(); // List to store recent files
        private readonly string filePath = "RecentFiles.json"; // Path to the JSON file for storing the list

        // Property to access the recent files
        public IEnumerable<string> Files => recentFiles;

        // Method to add a file to the recent files list
        public void AddFile(string file)
        {
            // Remove the file if it already exists to avoid duplicates
            recentFiles.Remove(file);
            // Insert the file at the beginning of the list
            recentFiles.Insert(0, file);

            // Ensure the list size does not exceed the maximum limit
            if (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
            }

            // Save the updated list of recent files
            SaveRecentFiles();
        }

        // Method to load recent files from the JSON file
        public void LoadRecentFiles()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                recentFiles = JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
            }
        }

        // Method to save the recent files list to the JSON file
        public void SaveRecentFiles()
        {
            string json = JsonSerializer.Serialize(recentFiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}

