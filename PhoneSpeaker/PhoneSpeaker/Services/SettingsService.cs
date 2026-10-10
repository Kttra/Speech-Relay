using System;
using System.IO;
using System.Text.Json;

namespace PhoneSpeaker.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhoneSpeaker",
            "settings.json");

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
    }
}
