using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using MaxMind.GeoIP2;

namespace CSSharpLanguageCentral;

public class CsSharpLanguageCentral : BasePlugin
{
    public override string ModuleName => "CSSharpLanguageCentral";
    public override string ModuleVersion => "0.0.1";

    public override string ModuleDescription => "Provides player language management with database and client language detection";

    public override string ModuleAuthor => "fakeutna A.K.A fltuna, tuna";

    public readonly FakeConVar<string> GeoIpDatabaseFileName = new("csslc_geoip_database_file", "The GeoIP database file to use for client country detection", "GeoLite2-City.mmdb");
    
    private readonly Dictionary<int, string> _clientCountry = new();
    
    private readonly Dictionary<int, bool> _isPlayerLanguageLoaded = new();

    private static readonly Dictionary<string, string> CountryMapping = new()
    {
        {"US", "en-US"},
        {"JP", "ja-JP"}
    };
    
    
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        
        AddCommand("css_language", "", SetLanguageCommand);
        
        AddCommandListener("css_lang", LanguageCommandListener, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RemoveListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        
        RemoveCommand("css_language", SetLanguageCommand);
        
        RemoveCommandListener("css_lang", LanguageCommandListener, HookMode.Pre);
    }

    private HookResult LanguageCommandListener(CCSPlayerController? player, CommandInfo info)
    {
        SetLanguageCommand(player, info);
        return HookResult.Stop;
    }

    private void SetLanguageCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var steamId = (SteamID)player.SteamID;

        if (info.ArgCount == 1)
        {
            var language = PlayerLanguageManager.Instance.GetLanguage(steamId);
            info.ReplyToCommand($"Current language is \"{language.Name}\" ({language.NativeName})");
            return;
        }

        if (info.ArgCount != 2)
            return;
        
        try
        {
            var language = info.GetArg(1);
            var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).Single(x => x.Name == language);
            PlayerLanguageManager.Instance.SetLanguage(steamId, cultureInfo);
            info.ReplyToCommand($"Language set to {cultureInfo.NativeName}");
        }
        catch (InvalidOperationException)
        {
            info.ReplyToCommand("Language not found.");
        }
        
        // TODO Save to DB
    }


    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        
        if (player == null)
            return;
        
        if(!_clientCountry.TryGetValue(slot, out var country))
            return;

        _clientCountry.Remove(slot);

        if (!CountryMapping.TryGetValue(country, out var languageIp))
        {
            languageIp = CountryMapping["US"];
        }

        var culture = new CultureInfo(languageIp);

        var steamId = (SteamID)player.SteamID;
        
        PlayerLanguageManager.Instance.SetLanguage(steamId, culture);
        
        // TODO Load from DB
    }


    private void OnClientConnect(int playerSlot, string name, string ipAddress)
    {
        var split = ipAddress.Split(":");
        var ip = split[0];
        var port = split[1];

        try
        {
            using var reader = new DatabaseReader( Path.Combine(ModuleDirectory, GeoIpDatabaseFileName.Value));
            var city = reader.City(ip);
            
            _clientCountry[playerSlot] = city.Country.IsoCode ?? "";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnClientDisconnect(int slot)
    {
        // If player is joined recently and language is not loaded yet, we don't need to save anything
        if (!_isPlayerLanguageLoaded.Remove(slot, out var isLoaded) || !isLoaded)
            return;
        
        
        // TODO Save to DB
    }
}