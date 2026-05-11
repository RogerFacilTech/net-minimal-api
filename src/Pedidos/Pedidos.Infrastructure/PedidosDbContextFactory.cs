using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FacShopAPI.Pedidos.Infrastructure
{
    public class PedidosDbContextFactory : IDesignTimeDbContextFactory<PedidosDbContext>
    {
        public PedidosDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PedidosDbContext>();
            optionsBuilder.UseSqlite("Data Source=Pedidos.db",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory_Pedidos"));
            return new PedidosDbContext(optionsBuilder.Options);
        }
    }
}