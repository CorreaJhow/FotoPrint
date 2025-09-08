using System.Text.Json;
using System.Text.Json.Serialization;

public class AdminConfig
{
    [JsonPropertyName("adminUsername")]
    public string AdminUsername { get; set; }

    [JsonPropertyName("adminPassword")]
    public string AdminPassword { get; set; }
}

public class AdminConfigService
{
    public AdminConfig Load()
    {
        var json = File.ReadAllText("adminconfig.json");
        return JsonSerializer.Deserialize<AdminConfig>(json);
    }
}
