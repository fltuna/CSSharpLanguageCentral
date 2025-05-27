using System.Globalization;

namespace CSSharpLanguageCentral.Config;

public class PluginConfig(DatabaseConfig databaseConfig, bool shouldPrintWelcomeMessage, CultureInfo fallbackLanguage, List<string> additionalCommandAliases, Dictionary<string, string> countryLanguageMapping)
{
    public List<string> AdditionalCommandAliases { get; } = additionalCommandAliases;

    public bool ShouldPrintWelcomeMessage { get; } = shouldPrintWelcomeMessage;
    
    // ISO 639 Language Codes
    public CultureInfo FallbackLanguage { get; } = fallbackLanguage;
    
    public Dictionary<string, string> CountryLanguageMapping { get; } = countryLanguageMapping;

    public DatabaseConfig DatabaseConfig { get; } = databaseConfig;
}