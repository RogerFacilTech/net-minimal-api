using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FacShopAPI.Pedidos.Data
{
    public class PedidosDbContextFactory : IDesignTimeDbContextFactory<PedidosDbContext>
    {
        public PedidosDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PedidosDbContext>();
            optionsBuilder.UseSqlite("Data Source=Pedidos.db");
            return new PedidosDbContext(optionsBuilder.Options);
        }
    }
}