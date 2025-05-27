using CSSharpLanguageCentral.Database.Models;

namespace CSSharpLanguageCentral.Database;

public interface IUserPrefsRepository
{
    Task<CsSharpLcUserPrefs?> GetByIdAsync(int id);
    
    Task<CsSharpLcUserPrefs?> GetBySteamIdAsync(long steamId64);
    
    Task<bool> UpsertAsync(CsSharpLcUserPrefs userPrefs);
    
    Task<bool> ExistsByIdAsync(int id);
    Task<bool> ExistsBySteamIdAsync(long steamId64);
}