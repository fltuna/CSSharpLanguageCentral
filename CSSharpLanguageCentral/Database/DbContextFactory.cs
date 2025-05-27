using System.Runtime.InteropServices;
using System.Security;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CSSharpLanguageCentral.Config;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;

namespace CSSharpLanguageCentral.Database;

public static class DbContextFactory
{
    public static CssLcDbContext CreateContext(DatabaseConfig config, BasePlugin plugin)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CssLcDbContext>();
        var connectionString = BuildConnectionString(config, plugin);
        
        try
        {
            switch (config.DatabaseType)
            {
                case CssLcSupportedDbType.Sqlite:
                    optionsBuilder.UseSqlite(connectionString + ";");
                    break;
                    
                case CssLcSupportedDbType.MySql:
                    var mysqlVersion = new MySqlServerVersion(new Version(8, 0, 21));
                    Server.PrintToConsole(connectionString);
                    optionsBuilder.UseMySql(connectionString, mysqlVersion);
                    break;
                    
                case CssLcSupportedDbType.PostgreSql:
                    Server.PrintToConsole(connectionString);
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
                    
                default:
                    throw new NotSupportedException($"Database provider {config.DatabaseType} is not supported");
            }
            
            #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            #endif
            
            return new CssLcDbContext(optionsBuilder.Options, config.DatabaseType);
        }
        finally
        {
            SecurelyEraseString(ref connectionString);
        }
    }
    
    private static string BuildConnectionString(DatabaseConfig config, BasePlugin plugin)
    {
        switch (config.DatabaseType)
        {
            case CssLcSupportedDbType.Sqlite:
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = Path.Combine(plugin.ModuleDirectory, config.DatabaseName),
                    Mode = SqliteOpenMode.ReadWriteCreate
                };

                return builder.ToString();
                
            case CssLcSupportedDbType.MySql:
                
                string passwordMySql = ConvertSecureStringToString(config.DatabasePassword);
                
                try
                {
                    return new MySqlConnectionStringBuilder
                    {
                        Server = config.DatabaseHost,
                        Port = uint.Parse(config.DatabasePort),
                        Database = config.DatabaseName,
                        UserID = config.DatabaseUser,
                        Password = passwordMySql,
                    }.ToString();
                }
                finally
                {
                    SecurelyEraseString(ref passwordMySql);
                }
                
            case CssLcSupportedDbType.PostgreSql:
                string passwordPostgreSql = ConvertSecureStringToString(config.DatabasePassword);
                
                try
                {
                    return new NpgsqlConnectionStringBuilder
                    {
                        Host = config.DatabaseHost,
                        Port = int.Parse(config.DatabasePort),
                        Database = config.DatabaseName,
                        Username = config.DatabaseUser,
                        Password = passwordPostgreSql,
                        IncludeErrorDetail = true
                    }.ToString();
                }
                finally 
                {
                    SecurelyEraseString(ref passwordPostgreSql);
                }
                
            default:
                throw new NotSupportedException($"Database provider {config.DatabaseType} is not supported");
        }
    }
    
    private static string ConvertSecureStringToString(SecureString secureString)
    {
        IntPtr bstrPtr = Marshal.SecureStringToBSTR(secureString);
        try
        {
            return Marshal.PtrToStringBSTR(bstrPtr);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(bstrPtr);
        }
    }
    
    private static void SecurelyEraseString(ref string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        
        GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            int size = value.Length * sizeof(char);
            
            Marshal.Copy(new byte[size], 0, ptr, size);
        }
        finally
        {
            handle.Free();
            value = string.Empty;
        }
    }
}
