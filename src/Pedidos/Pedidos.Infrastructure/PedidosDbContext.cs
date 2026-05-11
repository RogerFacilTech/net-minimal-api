using FacShopAPI.Pedidos.Domain;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Pedidos.Infrastructure;

public class PedidosDbContext : DbContext
{
    public PedidosDbContext(DbContextOptions<PedidosDbContext> options) : base(options)
    {
    }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Status).HasConversion<string>().IsRequired();
            entity.Property(p => p.Total).HasPrecision(10, 2).IsRequired();
        });

        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.NomeProduto).IsRequired().HasMaxLength(100);
            entity.Property(i => i.PrecoUnitario).HasPrecision(10, 2).IsRequired();
        });

        // Add other entity configurations here...
    }
}