using CSSharpLanguageCentral.Database.Models;
using System.Globalization;
using CounterStrikeSharp.API;
using Microsoft.EntityFrameworkCore;

namespace CSSharpLanguageCentral.Database;

public sealed class LanguageDbService
{
    private readonly CssLcDbContext _context;
    private readonly Dictionary<ulong, CultureInfo> _cache = new();
    
    public LanguageDbService(CssLcDbContext context)
    {
        _context = context;
        
        var result = _context.Database.EnsureCreated();
    }
    
    public async Task<CultureInfo?> GetLanguageAsync(ulong steamId64)
    {
        if (_cache.TryGetValue(steamId64, out var cached))
            return cached;
        
        var userPrefs = await _context.UserPreferences
            .FirstOrDefaultAsync(u => u.SteamId64 == steamId64);
            
        if (userPrefs != null)
        {
            _cache[steamId64] = userPrefs.UserCulture;
            return userPrefs.UserCulture;
        }
        
        return null;
    }
    
    public async Task<bool> SaveLanguageAsync(ulong steamId64, CultureInfo culture)
    {
        try
        {
            var existing = await _context.UserPreferences
                .FirstOrDefaultAsync(u => u.SteamId64 == steamId64);
            
            if (existing != null)
            {
                existing.UserCulture = culture;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newPrefs = new CsSharpLcUserPrefs(culture, steamId64)
                {
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserPreferences.Add(newPrefs);
            }
            
            await _context.SaveChangesAsync();
            
            _cache[steamId64] = culture;
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public void RemoveFromCache(ulong steamId64)
    {
        _cache.Remove(steamId64);
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}