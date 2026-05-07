using Catalogo.Domain;
using Catalogo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Data;

public class CatalogoDbContext : DbContext
{
    public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<Atributo> Atributos => Set<Atributo>();
    public DbSet<Midia> Midias => Set<Midia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nome).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descricao).IsRequired().HasConversion(
                descricao => descricao.Value,
                value => DescricaoProduto.Reconstituir(value));
            entity.Property(p => p.Preco).HasPrecision(10, 2).IsRequired();
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nome).IsRequired().HasMaxLength(100);
        });

        // Add other entity configurations here...
    }
}