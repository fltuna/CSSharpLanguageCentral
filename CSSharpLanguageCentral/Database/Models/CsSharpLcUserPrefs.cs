using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace CSSharpLanguageCentral.Database.Models;

public class CsSharpLcUserPrefs
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public ulong SteamId64 { get; set; }

    [Required]
    public CultureInfo UserCulture { get; set; } = CultureInfo.InvariantCulture;
    
    
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    
    public CsSharpLcUserPrefs() {}
    
    public CsSharpLcUserPrefs(CultureInfo userCulture, ulong steamId64)
    {
        SteamId64 = steamId64;
        UserCulture = userCulture;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}