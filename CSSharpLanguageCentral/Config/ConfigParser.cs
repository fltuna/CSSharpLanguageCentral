using System.Globalization;
using CounterStrikeSharp.API.Core;
using CSSharpLanguageCentral.Database;
using Tomlyn;
using Tomlyn.Model;

namespace CSSharpLanguageCentral.Config;

public sealed class ConfigParser(string configPath, BasePlugin plugin)
{
    private string ConfigPath { get; } = configPath;
    private BasePlugin Plugin { get; } = plugin;
    
    public PluginConfig Load()
    {
        string directory = Path.GetDirectoryName(ConfigPath)!;
        
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        
        if (File.Exists(ConfigPath))
            return LoadConfigFromFile();
        
        WriteDefaultConfig();
        
        return LoadConfigFromFile();
    }

    private PluginConfig LoadConfigFromFile()
    {
        string configText;

        // If file exists, then load from file.
        if (File.Exists(ConfigPath))
        {
            configText = File.ReadAllText(ConfigPath);
        }
        else
        {
            throw new FileNotFoundException($"Config file not found: {ConfigPath}");
        }

        TomlTable toml = Toml.ToModel(configText);

        if (!toml.TryGetValue("Settings", out var settingsSection) || settingsSection is not TomlTable settingsTable)
            throw new InvalidOperationException("Config file doesn't have a Settings section");

        var mutableConfig = new MutablePluginConfig();
        
        var missingSections = ParsePluginSettings(settingsTable, mutableConfig);

        if (missingSections.Any())
            throw new InvalidOperationException($"Required plugin settings are missing: {string.Join(", ", missingSections)}");
        
        var dbConfig = mutableConfig.CreateDatabaseConfig();
        
        if (dbConfig == null)
            throw new InvalidOperationException("Failed to parse database config.");
        
        return new PluginConfig(dbConfig, mutableConfig.ShouldPrintWelcomeMessage, mutableConfig.FallbackLanguage, mutableConfig.AdditionalCommandAliases, mutableConfig.CountryLanguageMapping);
    }
    
    private List<string> ParsePluginSettings(TomlTable pluginSettingTable, MutablePluginConfig pluginSettings)
    {
        List<string> missingSettings = new();

        if (pluginSettingTable.TryGetValue("ShouldPrintWelcomeMessage", out var shouldPrintWelcomeMessageObj) && shouldPrintWelcomeMessageObj is bool shouldPrintWelcomeMessage)
        {
            pluginSettings.ShouldPrintWelcomeMessage = shouldPrintWelcomeMessage;
        }
        else
        {
            missingSettings.Add("ShouldPrintWelcomeMessage");
        }


        if (pluginSettingTable.TryGetValue("AdditionalCommandAliases", out var additionalCommandAliasesObj) && additionalCommandAliasesObj is TomlArray additionalCommandAliases)
        {
            pluginSettings.AdditionalCommandAliases.AddRange(ParseStringArray(additionalCommandAliases));
        }
        else
        {
            missingSettings.Add("AdditionalCommandAliases");
        }

        if (pluginSettingTable.TryGetValue("FallbackLanguage", out var fallbackLanguageObj) && fallbackLanguageObj is string fallbackLanguageString)
        {
            // We don't handle exception here, because config parser should be failed when invalid language name specified
            pluginSettings.FallbackLanguage = CultureInfo.GetCultureInfo(fallbackLanguageString);
        }
        else
        {
            missingSettings.Add("FallbackLanguage");
        }

        if (pluginSettingTable.TryGetValue("LanguageMapping", out var languageMappingObj) && languageMappingObj is TomlTable languageMapping)
        {
            foreach (var (key, value) in languageMapping)
            {
                if (value is not string valueString)
                    throw new InvalidOperationException("Language name should be string!");
                
                pluginSettings.CountryLanguageMapping[key] = valueString;
            }
        }
        else
        {
            missingSettings.Add("LanguageMapping");
        }
        
        // Process DB information

        if (pluginSettingTable.TryGetValue("DatabaseType", out var dbTypeObj) && dbTypeObj is string dbType)
        {
            pluginSettings.DatabaseType = dbType;
        }
        else
        {
            missingSettings.Add("DatabaseType");
        }

        if (pluginSettingTable.TryGetValue("DatabaseHost", out var databaseHostObj) && databaseHostObj is string databaseHost)
        {
            pluginSettings.DatabaseHost = databaseHost;
        }
        else
        {
            missingSettings.Add("DatabaseHost");
        }

        if (pluginSettingTable.TryGetValue("DatabasePort", out var databasePortObj) && databasePortObj is string databasePort)
        {
            pluginSettings.DatabasePort = databasePort;
        }
        else
        {
            missingSettings.Add("DatabasePort");
        }

        if (pluginSettingTable.TryGetValue("DatabaseName", out var databaseNameObj) && databaseNameObj is string databaseName)
        {
            pluginSettings.DatabaseName = databaseName;
        }
        else
        {
            missingSettings.Add("DatabaseName");
        }

        if (pluginSettingTable.TryGetValue("DatabaseUser", out var databaseUserObj) && databaseUserObj is string databaseUser)
        {
            pluginSettings.DatabaseUser = databaseUser;
        }
        else
        {
            missingSettings.Add("DatabaseUser");
        }

        if (pluginSettingTable.TryGetValue("DatabasePassword", out var databasePasswordObj) && databasePasswordObj is string databasePassword)
        {
            pluginSettings.DatabasePassword = databasePassword;
        }
        else
        {
            missingSettings.Add("DatabasePassword");
        }
        
        
        return missingSettings;
    }
        
    private List<string> ParseStringArray(TomlArray array)
    {
        List<string> result = new List<string>();
        foreach (var item in array)
        {
            if (item is string stringValue)
            {
                result.Add(stringValue);
            }
        }
        return result;
    }

    private void WriteDefaultConfig()
    {
        const string defaultConfig = @"
[Settings]
# Additional command aliases for language command.
# Also, this plugin overrides !lang command behaviour to this plugin's command, so its optional
AdditionalCommandAliases = [""css_language""]

# Should print welcome message to when player join to inform server supports !lang command.
ShouldPrintWelcomeMessage = true

# Use this language code if your LanguageMapping doesn't supported a language that detected by a GeoIP
# See ""ISO 639"" to available language codes.
FallbackLanguage = ""en""

# Maps country and language
# See ""ISO 3166-1"" to available country codes
# See ""ISO 639"" to available language codes.
LanguageMapping = { JA = ""ja"", US = ""en"" }

# Database
DatabaseType = ""sqlite""
# When you using sqlite, then this name become file name
DatabaseName = ""CSSharpLanguageCentral.db""
DatabaseHost = """"
DatabasePort = """"
DatabaseUser = """"
DatabasePassword = """"
";
        
        File.WriteAllText(ConfigPath, defaultConfig);
    }

    private sealed class MutablePluginConfig
    {
        public List<string> AdditionalCommandAliases = new();
        
        public bool ShouldPrintWelcomeMessage = true;
        
        
        public CultureInfo FallbackLanguage = CultureInfo.InvariantCulture;
    
        public Dictionary<string, string> CountryLanguageMapping { get; } = new();
        
        public string DatabaseType = string.Empty;
        public string DatabaseHost = string.Empty;
        public string DatabasePort = string.Empty;
        public string DatabaseName = string.Empty;
        public string DatabaseUser = string.Empty;
        public string DatabasePassword = string.Empty;

        public DatabaseConfig? CreateDatabaseConfig()
        {
            if (string.IsNullOrEmpty(DatabaseType))
                return null;
            
            if (!Enum.TryParse<CssLcSupportedDbType>(DatabaseType, true, out var dbType))
                return null;
            
            return new DatabaseConfig(dbType, DatabaseHost, DatabasePort, DatabaseName, DatabaseUser, ref DatabasePassword);
        }
    }
}