using System.Text.Json;
using FotoPrint.Models;

namespace FotoPrint.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly object _lock = new();

        public ConfigService(IWebHostEnvironment env)
        {
            _configPath = Path.Combine(env.ContentRootPath, "config.json");
            if (!File.Exists(_configPath))
            {
                var def = new Settings();
                File.WriteAllText(_configPath, JsonSerializer.Serialize(def, _jsonOptions));
            }
        }

        public Settings Load()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }

        public void Save(Settings s)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(s, _jsonOptions);
                File.WriteAllText(_configPath, json);
            }
        }
    }
}
