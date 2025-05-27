using System.Globalization;
using CSSharpLanguageCentral.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace CSSharpLanguageCentral.Database;

public class CssLcDbContext(DbContextOptions<CssLcDbContext> options, CssLcSupportedDbType provider) : DbContext(options)
{
    public DbSet<CsSharpLcUserPrefs> UserPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CsSharpLcUserPrefs>(entity =>
        {
            entity.ToTable("user_language_preferences");
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.SteamId64)
                .IsUnique();
            
            entity.Property(e => e.UserCulture)
                .HasConversion(
                    v => v.Name,
                    v => new CultureInfo(v))
                .HasMaxLength(10)
                .IsRequired()
                .HasColumnName("culture_name");
                
            entity.Property(e => e.SteamId64)
                .HasColumnName("steam_id");
                
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");
                
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");
            
            switch (provider)
            {
                case CssLcSupportedDbType.Sqlite:
                    entity.Property(e => e.CreatedAt)
                        .HasDefaultValueSql("datetime('now')");
                    entity.Property(e => e.UpdatedAt)
                        .HasDefaultValueSql("datetime('now')");
                    break;
                case CssLcSupportedDbType.MySql:
                    entity.Property(e => e.CreatedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    entity.Property(e => e.UpdatedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
                    break;
                    
                case CssLcSupportedDbType.PostgreSql:
                    entity.Property(e => e.CreatedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    entity.Property(e => e.UpdatedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    break;
            }
        });
    }
}