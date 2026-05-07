using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FacShopAPI.Catalogo.Data
{
    public class CatalogoDbContextFactory : IDesignTimeDbContextFactory<CatalogoDbContext>
    {
        public CatalogoDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CatalogoDbContext>();
            optionsBuilder.UseSqlite("Data Source=Catalogo.db");

            return new CatalogoDbContext(optionsBuilder.Options);
        }
    }
}