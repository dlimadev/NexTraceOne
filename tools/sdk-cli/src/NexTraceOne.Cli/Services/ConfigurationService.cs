using System.Text.Json;

namespace NexTraceOne.Cli.Services;

public class ConfigurationService
{
    private readonly string _configPath;
    private CliConfiguration? _configuration;

    public ConfigurationService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "nextraceone");
        
        Directory.CreateDirectory(appDataPath);
        _configPath = Path.Combine(appDataPath, "config.json");
        
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            _configuration = JsonSerializer.Deserialize<CliConfiguration>(json);
        }
        else
        {
            _configuration = new CliConfiguration
            {
                Endpoint = "http://localhost:5000",
                Timeout = 30,
                OutputFormat = "table",
                Colors = true
            };
            SaveConfiguration();
        }
    }

    private void SaveConfiguration()
    {
        var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(_configPath, json);
    }

    public string GetEndpoint() => _configuration?.Endpoint ?? "http://localhost:5000";
    
    public void SetEndpoint(string endpoint)
    {
        if (_configuration != null)
        {
            _configuration.Endpoint = endpoint;
            SaveConfiguration();
        }
    }

    public int GetTimeout() => _configuration?.Timeout ?? 30;
    
    public void SetTimeout(int timeout)
    {
        if (_configuration != null)
        {
            _configuration.Timeout = timeout;
            SaveConfiguration();
        }
    }

    public string GetOutputFormat() => _configuration?.OutputFormat ?? "table";
    
    public void SetOutputFormat(string format)
    {
        if (_configuration != null)
        {
            _configuration.OutputFormat = format;
            SaveConfiguration();
        }
    }

    public bool GetColors() => _configuration?.Colors ?? true;
    
    public void SetColors(bool enabled)
    {
        if (_configuration != null)
        {
            _configuration.Colors = enabled;
            SaveConfiguration();
        }
    }

    public string? GetApiKey() => _configuration?.ApiKey;
    
    public void SetApiKey(string apiKey)
    {
        if (_configuration != null)
        {
            _configuration.ApiKey = apiKey;
            SaveConfiguration();
        }
    }

    public string? GetToken() => _configuration?.Token;
    
    public void SetToken(string token)
    {
        if (_configuration != null)
        {
            _configuration.Token = token;
            _configuration.TokenExpiry = DateTime.UtcNow.AddHours(1);
            SaveConfiguration();
        }
    }

    public bool IsTokenValid()
    {
        return _configuration?.TokenExpiry != null && 
               _configuration.TokenExpiry > DateTime.UtcNow;
    }

    public void ClearToken()
    {
        if (_configuration != null)
        {
            _configuration.Token = null;
            _configuration.TokenExpiry = null;
            SaveConfiguration();
        }
    }

    public Dictionary<string, object> GetAllSettings()
    {
        return new Dictionary<string, object>
        {
            ["endpoint"] = GetEndpoint(),
            ["timeout"] = GetTimeout(),
            ["outputFormat"] = GetOutputFormat(),
            ["colors"] = GetColors(),
            ["authenticated"] = IsTokenValid()
        };
    }

    public void Reset()
    {
        _configuration = new CliConfiguration
        {
            Endpoint = "http://localhost:5000",
            Timeout = 30,
            OutputFormat = "table",
            Colors = true
        };
        SaveConfiguration();
    }
}

public class CliConfiguration
{
    public string Endpoint { get; set; } = "http://localhost:5000";
    public int Timeout { get; set; } = 30;
    public string OutputFormat { get; set; } = "table";
    public bool Colors { get; set; } = true;
    public string? ApiKey { get; set; }
    public string? Token { get; set; }
    public DateTime? TokenExpiry { get; set; }
}
