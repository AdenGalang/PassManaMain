using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PassManaAlpha.Core
{
    public class AppConfig
    {
        // List of vault file paths the user has opened or created
        public List<string> KnownVaults { get; set; } = new();

        // Path of the last active vault (just for remembering selection on restart)
        public string? LastActiveVault { get; set; }

        public string LastAccessed { get; set; } = "Never";

        // Legacy: kept so old config.json files don't crash on deserialise
        public string MasterKey { get; set; } = string.Empty;

        private static readonly string ConfigPath = "config.json";

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();
            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
