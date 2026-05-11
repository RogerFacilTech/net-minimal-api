using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FacShopAPI.Catalogo.Infrastructure
{
    public class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
    {
        public CatalogoDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CatalogoDbContext>();
            optionsBuilder.UseSqlite("Data Source=Catalogo.db",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory_Catalogo"));

            return new CatalogoDbContext(optionsBuilder.Options);
        }
    }
}