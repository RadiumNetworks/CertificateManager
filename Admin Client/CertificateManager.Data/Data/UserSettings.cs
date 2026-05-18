using System.Text.Json;

namespace CertificateManager.Admin.Data
{
    public class UserSettings
    {
        public string? ConnectionString { get; set; }

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CertificateManager");

        private static readonly string SettingsFile = Path.Combine(SettingsDir, "usersettings.json");

        public static UserSettings Load()
        {
            if (!File.Exists(SettingsFile))
                return new UserSettings();

            var json = File.ReadAllText(SettingsFile);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }
}
