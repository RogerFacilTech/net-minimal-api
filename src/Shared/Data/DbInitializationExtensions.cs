using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FacShopAPI.Shared.Data;

public static class DbInitializationExtensions
{
    /// <summary>
    /// Aplica migrations pendentes e executa callback opcional (ex: seed).
    /// Use em Program.cs para garantir o banco atualizado na startup.
    /// </summary>
    public static IHost MigrateDatabase<TContext>(this IHost host, Action<TContext>? onMigrated = null)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        db.Database.Migrate();
        onMigrated?.Invoke(db);
        return host;
    }
}
