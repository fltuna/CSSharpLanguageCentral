using System.Collections.Concurrent;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CSSharpLanguageCentral.Config;
using CSSharpLanguageCentral.Database;
using CSSharpLanguageCentral.Util;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;

namespace CSSharpLanguageCentral;

public class CsSharpLanguageCentral : BasePlugin
{
    public override string ModuleName => "CSSharpLanguageCentral";
    public override string ModuleVersion => "0.0.1";

    public override string ModuleDescription => "Provides player language management with database and client language detection";

    public override string ModuleAuthor => "fakeutna A.K.A fltuna, tuna";

    public readonly FakeConVar<string> GeoIpDatabaseFileName = new("csslc_geoip_database_file", "The GeoIP database file to use for client country detection", "GeoLite2-City.mmdb");
    
    private readonly Dictionary<int, string> _clientCountry = new();
    private readonly Dictionary<int, SteamID> _userSteamIds = new();
    
    private readonly ConcurrentDictionary<int, bool> _isPlayerLanguageLoaded = new();

    private PluginConfig _pluginConfig = null!;
    private LanguageDbService _languageDbService = null!;
    
    
    public override void Load(bool hotReload)
    {
        _pluginConfig = new ConfigParser(Path.Combine(ModuleDirectory, "plugin.toml"), this).Load();
        _languageDbService = new LanguageDbService(DbContextFactory.CreateContext(_pluginConfig.DatabaseConfig, this));
        
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);


        CommandRemover.RemoveCommandByDefinition("css_lang");
        
        AddCommand("css_language", "", SetLanguageCommand);
        AddCommand("css_lang", "", SetLanguageCommand);
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RemoveListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        
        RemoveCommand("css_language", SetLanguageCommand);
        RemoveCommand("css_lang", SetLanguageCommand);
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
            PrintLocalizedChatWithPrefixToPlayer(player, "Language.Command.CurrentLanguage", language.NativeName, language.Name);
            return;
        }

        if (info.ArgCount != 2)
            return;
        
        var languageArg = info.GetArg(1);
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultures(CultureTypes.AllCultures).Single(x => x.Name == languageArg);
        }
        catch (InvalidOperationException)
        {
            PrintLocalizedChatWithPrefixToPlayer(player, "Language.Command.LanguageNotFound", languageArg);
            return;
        }
        
        PlayerLanguageManager.Instance.SetLanguage(steamId, culture);
        PrintLocalizedChatWithPrefixToPlayer(player, "Language.Command.LanguageChanged", culture.NativeName, culture.Name);
    }


    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        
        if (player == null)
            return;

        _clientCountry.Remove(slot, out var country);
        
        country ??= _pluginConfig.FallbackLanguage.Name;

        var culture = _pluginConfig.CountryLanguageMapping.TryGetValue(country, out var languageName)
            ? new CultureInfo(languageName) :
            _pluginConfig.FallbackLanguage;

        var steamId = (SteamID)player.SteamID;
        _userSteamIds[slot] = steamId;
        
        PlayerLanguageManager.Instance.SetLanguage(steamId, culture);
        
        Task.Run(async () =>
        {
            var playerLang = await _languageDbService.GetLanguageAsync(steamId.SteamId64);
            
            await Server.NextFrameAsync(() =>
            {
                if (playerLang == null)
                {
                    AddTimer(5.0F, () =>
                    {
                        PrintLocalizedChatWithPrefixToPlayer(player, "General.Notification.JoinMessage.LanguageNotLoaded");
                    });
                }
                else
                {
                    PlayerLanguageManager.Instance.SetLanguage(steamId, playerLang);

                    AddTimer(5.0F, () =>
                    {
                        PrintLocalizedChatWithPrefixToPlayer(player, "General.Notification.JoinMessage.LanguageLoaded", playerLang.NativeName);
                    });
                }
                _isPlayerLanguageLoaded[slot] = true;
            });
        });
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
        catch (Exception)
        {
            // Ignored
        }
    }

    private void OnClientDisconnect(int slot)
    {
        // If player is joined recently and language is not loaded yet, we don't need to save anything
        if (!_isPlayerLanguageLoaded.Remove(slot, out var isLoaded) || !isLoaded)
            return;
        
        if (!_userSteamIds.TryGetValue(slot, out var playerSteamId))
            return;
        
        var culture = PlayerLanguageManager.Instance.GetLanguage(playerSteamId);
        
        Task.Run(async () =>
        {
            bool isOperationSucceeded = await _languageDbService.SaveLanguageAsync(playerSteamId.SteamId64, culture);

            await Server.NextFrameAsync(() =>
            {
                if (!isOperationSucceeded)
                {
                    Logger.LogError("Failed to save language for player {SteamID}", playerSteamId.SteamId64);
                }
            });
        });
    }
    
    
    
    private void PrintLocalizedChatWithPrefixToPlayer(CCSPlayerController? player, string translationKey, params object[] args)
    {
        if (player == null)
        {
            Server.PrintToConsole($"{LocalizeText(player, translationKey, args)}");
            return;
        }
        
        player.PrintToChat(GetTextWithPluginPrefix(player, LocalizeText(player, translationKey, args)).ToString());
    }

    private ReadOnlySpan<char> GetTextWithPluginPrefix(CCSPlayerController player, ReadOnlySpan<char> text)
    {
        return $"{Localizer.ForPlayer(player, "Plugin.Prefix")} {text}".AsSpan();
    }

    private ReadOnlySpan<char> LocalizeText(CCSPlayerController? player, string translationKey, params object[] args)
    {
        if (player == null)
        {
            return Localizer[translationKey, args].Value.AsSpan();
        }

        return Localizer.ForPlayer(player, translationKey, args).AsSpan();
    }
}